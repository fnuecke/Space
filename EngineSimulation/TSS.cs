using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Implements a Trailing State Synchronization.
    /// </summary>
    /// <see cref="http://warriors.eecs.umich.edu/games/papers/netgames02-tss.pdf"/>
    public sealed class TSS : IReversibleSimulation
    {
        #region Logger

        /// <summary>
        /// Logger for general purpose logging.
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Events

        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate an authoritative command / a user rollback request.
        /// The handler must trigger the process of getting a valid snapshot,
        /// which is fed back to this state (normally via <c>Depacketize()</c>).
        /// </summary>
        public event EventHandler<EventArgs> Invalidated;

        #endregion

        #region Properties

        /// <summary>
        /// The current frame of the leading state.
        /// </summary>
        public long CurrentFrame { get; private set; }

        /// <summary>
        /// The frame number of the trailing state, i.e. the point we cannot roll
        /// back past.
        /// </summary>
        public long TrailingFrame { get { return _simulations[_simulations.Length - 1].CurrentFrame; } }

        /// <summary>
        /// The component system manager in use in this simulation.
        /// </summary>
        public IManager Manager { get; private set; }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        public bool WaitingForSynchronization { get; private set; }

        /// <summary>
        /// Get the trailing simulation.
        /// </summary>
        public IAuthoritativeSimulation TrailingSimulation { get { return _simulations[_simulations.Length - 1]; } }

        /// <summary>
        /// Get the leading simulation.
        /// </summary>
        private IAuthoritativeSimulation LeadingSimulation { get { return _simulations[0]; } }

        #endregion

        #region Fields

        /// <summary>
        /// The delays of the individual simulations.
        /// </summary>
        private readonly uint[] _delays;

        /// <summary>
        /// The parameterization for the different threads.
        /// </summary>
        private readonly ThreadData[] _threadData;

        /// <summary>
        /// Tasks used to update different simulations.
        /// </summary>
        private readonly Task[] _tasks;

        /// <summary>
        /// The list of running simulations. They are ordered in in increasing
        /// delay, i.e. the state at slot 0 is the leading one, 1 is the next
        /// newest, and so on.
        /// </summary>
        private readonly IAuthoritativeSimulation[] _simulations;

        /// <summary>
        /// List of object ids to remove from delayed simulations when they
        /// reach the given frame.
        /// </summary>
        private readonly Dictionary<long, List<int>> _removes = new Dictionary<long, List<int>>();

        /// <summary>
        /// List of commands to push in delayed simulations when they reach the
        /// given frame.
        /// </summary>
        private readonly Dictionary<long, List<Command>> _commands = new Dictionary<long, List<Command>>();

        /// <summary>
        /// A list that is re-used for marking entries for removal.
        /// </summary>
        private readonly List<long> _reusableDeprecationList = new List<long>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TSS based meta state.
        /// </summary>
        /// <param name="delays">The delays to use for trailing simulations, with the delays in frames.</param>
        public TSS(uint[] delays)
        {
            _delays = new uint[delays.Length + 1];
            delays.CopyTo(_delays, 1);
            Array.Sort(_delays);

            // Initialize thread data. The trailing simulation will always be
            // updated by the main thread, to check if a rollback is required,
            // so we need one less than we have simulations.
            _threadData = new ThreadData[_delays.Length - 1];
            _tasks = new Task[_delays.Length - 1];

            // Generate initial simulations.
            _simulations = new IAuthoritativeSimulation[_delays.Length];

            // Our pass-through component manager, which allows adding and
            // removing only in the first frame (i.e. before the first update).
            Manager = new TSSEntityManager(this);

            // Mark us for need of sync.
            WaitingForSynchronization = true;
        }

        #endregion

        #region Invalidation / (Re-)Initialization

        /// <summary>
        /// Initialize the TSS to the given simulation. This also clears the
        /// <c>WaitingForSynchronization</c> flag.
        /// </summary>
        /// <param name="simulation">The simulation to initialize this TSS to.</param>
        public void Initialize(IAuthoritativeSimulation simulation)
        {
            MirrorSimulation(simulation, _simulations.Length - 1);
            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Mark the simulation as invalid (desynchronized). Will trigger a new
        /// <c>Invalidated</c> event.
        /// </summary>
        public void Invalidate()
        {
            OnInvalidated(EventArgs.Empty);
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// Push a command to all sub simulations.
        /// 
        /// This will lead to a rollback of all simulations that have already
        /// passed the command's frame. They will be fast-forwarded
        /// appropriately in the next update.
        /// </summary>
        /// <param name="command">the command to push.</param>
        public void PushCommand(Command command)
        {
            PushCommand(command, CurrentFrame);
        }

        /// <summary>
        /// Push a command to be executed at the given frame.  This will roll
        /// back, if necessary, to remove the object, meaning it can trigger
        /// invalidation.
        /// </summary>
        /// <param name="command">the command to push.</param>
        /// <param name="frame">the frame in which to execute the command.</param>
        public void PushCommand(Command command, long frame)
        {
            // Check if we can possibly apply this command.
            if (frame >= TrailingFrame)
            {
                // Store it to be removed in trailing simulations.
                if (!_commands.ContainsKey(frame))
                {
                    // No such command yet, push it.
                    _commands.Add(frame, new List<Command>());
                }

                // Rewind to the frame to retroactively apply changes. Only if
                // necessary, though.
                if (frame < CurrentFrame && !_commands[frame].Contains(command))
                {
                    Rewind(frame);
                }

                // Sort the commands by their ID, to get a deterministic order of
                // command execution.
                var index = _commands[frame].BinarySearch(command);
                if (index < 0)
                {
                    // Command is not yet known, just insert it.
                    _commands[frame].Insert(~index, command);
                }
                else
                {
                    // Command is already known, see if we can replace it because the
                    // new command is authoritative and the old one wasn't.
                    if (!_commands[frame][index].IsAuthoritative && command.IsAuthoritative)
                    {
                        // We already have that command, but it's not authoritative,
                        // yet this one is, so we'll replace it.
                        _commands[frame].Insert(index, command);
                        _commands[frame].RemoveAt(index + 1);
                    }
                    // else: we already have an authoritative one!
                }
            }
            else if (command.IsAuthoritative)
            {
                // Cannot apply, and this is an authoritative command. We need to sync.
                OnInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Advance leading simulation to <c>CurrentFrame + 1</c> and update
        /// all trailing simulations accordingly.
        /// </summary>
        public void Update()
        {
            // Advance the simulation.
            FastForward(++CurrentFrame);
        }

        #endregion

        #region Time specific adding / removal of entities

        /// <summary>
        /// Remove an object in a specific time frame. This will roll back, if
        /// necessary, to remove the object, meaning it can trigger invalidation.
        /// </summary>
        /// <param name="entity">The id of the object to remove.</param>
        /// <param name="frame">The frame to remove it at.</param>
        public void RemoveEntity(int entity, long frame)
        {
            // Store it to be removed in trailing simulations.
            if (!_removes.ContainsKey(frame))
            {
                _removes.Add(frame, new List<int>());
            }
            else if (_removes[frame].Contains(entity))
            {
                // Don't insert the same remove to the list twice.
                return;
            }
            _removes[frame].Add(entity);

            // Rewind to the frame to retroactively apply changes.
            if (frame < CurrentFrame)
            {
                Rewind(frame);
            }
        }

        #endregion

        #region Fine-grained playback control

        /// <summary>
        /// Run the simulation to the given frame, which may be in the past.
        /// This will make the given frame the new  <c>CurrentFrame</c>, which
        /// means that no adds/removes/commands/update will be applied to that
        /// actual frame just yet.
        /// </summary>
        /// <param name="frame">the frame to run to.</param>
        public void RunToFrame(long frame)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException("Waiting for synchronization.");
            }

            if (frame >= CurrentFrame)
            {
                // Moving forward, just run.
                FastForward(frame);
            }
            else
            {
                // Need to revert.
                Rewind(frame);
            }
            CurrentFrame = frame;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Serialize a simulation to a packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public Packet Packetize(Packet packet)
        {
            // Write the frame to fast forward to, after unwrapping.
            packet.Write(CurrentFrame);

            // Write the trailing simulation. We can reconstruct the newer ones
            // from there.
            packet.Write(_simulations[_simulations.Length - 1]);

            // Write pending object removals.
            packet.Write(_removes.Count);
            foreach (var remove in _removes)
            {
                packet.Write(remove.Key);
                packet.Write(remove.Value.Count);
                foreach (var entityUid in remove.Value)
                {
                    packet.Write(entityUid);
                }
            }

            // Write pending simulation commands.
            packet.Write(_commands.Count);
            foreach (var command in _commands)
            {
                packet.Write(command.Key);
                packet.WriteWithTypeInfo(command.Value);
            }

            return packet;
        }

        /// <summary>
        /// Deserialize a simulation from a packet.
        /// </summary>
        /// <param name="packet">The packet to read the data from.</param>
        public void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Unwrap the trailing state and mirror it to all the newer ones.
            packet.ReadPacketizableInto(ref _simulations[_simulations.Length - 1]);
            MirrorSimulation(_simulations[_simulations.Length - 1], _simulations.Length - 2);

            // Find adds / removes / commands that our out of date now, but keep
            // newer ones (that might be locally generated).
            PrunePastEvents();

            // Continue with reading the list of removes.
            var numRemoves = packet.ReadInt32();
            for (var removeIdx = 0; removeIdx < numRemoves; ++removeIdx)
            {
                var key = packet.ReadInt64();
                if (!_removes.ContainsKey(key))
                {
                    _removes.Add(key, new List<int>());
                }
                var numValues = packet.ReadInt32();
                for (var valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    _removes[key].Add(packet.ReadInt32());
                }
            }

            // And finally the commands.
            var numCommands = packet.ReadInt32();
            for (var commandIdx = 0; commandIdx < numCommands; ++commandIdx)
            {
                var key = packet.ReadInt64();
                if (!_commands.ContainsKey(key))
                {
                    _commands.Add(key, new List<Command>());
                }
                _commands[key].AddRange(packet.ReadPacketizablesWithTypeInfo<Command>());
            }

            // Got a valid state.
            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public ISimulation NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public void CopyInto(ISimulation into)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Utility methods

        private void OnInvalidated(EventArgs e)
        {
            WaitingForSynchronization = true;
            if (Invalidated != null)
            {
                Invalidated(this, e);
            }
        }

        /// <summary>
        /// Update the simulation by advancing forwards. This will run to,
        /// excluding, the given frame (i.e. adds/removes/commands will *not*
        /// be applied to the given frame, and its <c>Update()</c> method will
        /// *not* be called once it reaches the given frame).
        /// </summary>
        /// <param name="frame">The frame up to which to run.</param>
        private void FastForward(long frame)
        {
            // Start threads for the non-trailing simulations. We run these in
            // parallel with the trailing simulation update, even though this
            // might result in us having to redo this (if the trailing sim is
            // invalid). But the redo is *very* rare, so it's really not an issue.
            BeginThreadedUpdate(frame);

            // Process the trailing state, see if we need a roll-back.
            var needsRewind = false;
            while (TrailingSimulation.CurrentFrame < frame - _delays[_simulations.Length - 1])
            {
                // It needs running, so prepare it for that.
                PrepareForUpdate(TrailingSimulation);

                // Then check if any of the commands were unauthorized.
                needsRewind = TrailingSimulation.SkipNonAuthoritativeCommands() || needsRewind;

                // Do the actual stepping for the state.
                TrailingSimulation.Update();
            }

            // Wait for our worker threads to finish.
            EndThreadedUpdate();

            // Check if we had trailing tentative commands.
            if (needsRewind)
            {
                Logger.Trace("Pruned non-authoritative commands, mirroring trailing state.");
                MirrorSimulation(TrailingSimulation, _simulations.Length - 2);

                // Update the other states once more when doing a threaded update.
                BeginThreadedUpdate(frame);
                EndThreadedUpdate();
            }

            // Fast-forward the remaining states. As opposed to the threaded update
            // we only do this after the trailing simulation was updated, because
            // we'd gain nothing from doing it before that.
            NonThreadedUpdate(frame);

            // Clean up stuff that's too old to keep.
            PrunePastEvents();
        }

        /// <summary>
        /// Begin threaded update for all simulations except the trailing one.
        /// </summary>
        /// <param name="frame">The frame to perform the update for.</param>
        [Conditional("TSS_THREADING")]
        private void BeginThreadedUpdate(long frame)
        {
            for (var i = 0; i < _threadData.Length; i++)
            {
                _threadData[i].Simulation = _simulations[i];
                _threadData[i].Frame = frame - _delays[i];
                _tasks[i] = Task.Factory.StartNew(ThreadedUpdate, _threadData[i]);
            }
        }

        /// <summary>
        /// Perform a threaded update of a simulation.
        /// </summary>
        /// <param name="data">The information about which simulation to update up to which frame.</param>
        private void ThreadedUpdate(object data)
        {
            var info = (ThreadData)data;

            try
            {
                while (info.Simulation.CurrentFrame < info.Frame)
                {
                    PrepareForUpdate(info.Simulation);
                    info.Simulation.Update();
                }
            }
            catch (Exception ex)
            {
                Logger.WarnException("Error in threaded update.", ex);
                throw;
            }
        }
        
        /// <summary>
        /// Wait for threaded update to finish for all simulations.
        /// </summary>
        [Conditional("TSS_THREADING")]
        private void EndThreadedUpdate()
        {
            Task.WaitAll(_tasks);
        }

        /// <summary>
        /// Performs a non-threaded (sequential) update for all non-trailing
        /// simulations. This is called after the trailing simulation has been
        /// updated.
        /// </summary>
        /// <param name="frame">The frame to run the leading simulation to.</param>
#if TSS_THREADING // Fugly hack, but ConditionalAttribute does not support !TSS_THREADING.
        [Conditional("FALSE")]
#endif
        private void NonThreadedUpdate(long frame)
        {
            for (var i = 0; i < _simulations.Length - 1; i++)
            {
                while (_simulations[i].CurrentFrame < frame - _delays[i])
                {
                    PrepareForUpdate(_simulations[i]);
                    _simulations[i].Update();
                }
            }
        }

        /// <summary>
        /// Prepares a simulation for its next update, by pushing commands for
        /// that frame, as well as adding and removing entities.
        /// </summary>
        /// <param name="simulation">The simulation to prepare.</param>
        private void PrepareForUpdate(ISimulation simulation)
        {
            // The frame the state is now in, and that will be executed.
            var frame = simulation.CurrentFrame;

            // Check if we need to remove objects.
            if (_removes.ContainsKey(frame))
            {
                // Add a copy of it.
                foreach (var entityUid in _removes[frame])
                {
                    simulation.Manager.RemoveEntity(entityUid);
                }
            }

            // Check if we have commands to execute in that frame.
            if (_commands.ContainsKey(frame))
            {
                foreach (var command in _commands[frame])
                {
                    simulation.PushCommand(command);
                }
            }
        }

        /// <summary>
        /// Rewind the simulation to the "beginning" of the given frame.
        /// Meaning one frame before the given frame, making it ready to
        /// have adds/removes/commands applied to it again. If this fails
        /// (too far in the past) this will trigger a resynchronization
        /// request.
        /// </summary>
        /// <param name="frame">the frame to rewind to.</param>
        private void Rewind(long frame)
        {
            // Find first state that's not past the frame.
            for (var i = 0; i < _simulations.Length; ++i)
            {
                if (_simulations[i].CurrentFrame <= frame)
                {
                    // Success, mirror the state to all newer ones.
                    MirrorSimulation(_simulations[i], i - 1);
                    return; // Then return, so we don't trigger resync ;)
                }
            }

            // Cannot rewind that far, request resynchronization.
            OnInvalidated(EventArgs.Empty);
        }

        /// <summary>
        /// Mirror the given frame to all more recent frames.
        /// </summary>
        /// <param name="state">the state to mirror.</param>
        /// <param name="start">the index to start at.</param>
        private void MirrorSimulation(ICopyable<ISimulation> state, int start)
        {
            for (var i = start; i >= 0; --i)
            {
                _simulations[i] = _simulations[i] ?? (IAuthoritativeSimulation)state.NewInstance();
                state.CopyInto(_simulations[i]);
            }
        }

        /// <summary>
        /// Some cleanup, removing old adds/removes, that will never
        /// be checked again.
        /// </summary>
        private void PrunePastEvents()
        {
            // Remove adds / removes from the to-add list that have been added
            // to the state trailing furthest behind at this point.
            foreach (var key in _removes.Keys)
            {
                if (key < TrailingFrame)
                {
                    _reusableDeprecationList.Add(key);
                }
            }
            foreach (var key in _reusableDeprecationList)
            {
                _removes.Remove(key);
            }
            _reusableDeprecationList.Clear();

            foreach (var key in _commands.Keys)
            {
                if (key < TrailingFrame)
                {
                    _reusableDeprecationList.Add(key);
                }
            }
            foreach (var key in _reusableDeprecationList)
            {
                _commands.Remove(key);
            }
            _reusableDeprecationList.Clear();
        }

        #region Thread parameter wrapper

        /// <summary>
        /// Wrapper for parameters passed to a updater thread.
        /// </summary>
        private struct ThreadData
        {
            /// <summary>
            /// The simulation to update.
            /// </summary>
            public IAuthoritativeSimulation Simulation;

            /// <summary>
            /// The frame to run to.
            /// </summary>
            public long Frame;
        }

        #endregion

        #endregion

        #region Manager-Wrappers

        /// <summary>
        /// Managed wrapper for TSS to interface to the leading state for read
        /// operations, and inject commands for modifying operations.
        /// </summary>
        private sealed class TSSEntityManager : IManager
        {
            #region Properties

            /// <summary>
            /// A list of all components currently registered with this manager,
            /// in order of their ID.
            /// </summary>
            IEnumerable<Component> IManager.Components
            {
                get { throw new NotSupportedException(); }
            }

            /// <summary>
            /// A list of all systems registered with this manager.
            /// </summary>
            IEnumerable<AbstractSystem> IManager.Systems
            {
                get { throw new NotSupportedException(); }
            }

            /// <summary>
            /// Number of components currently registered in this system.
            /// </summary>
            public int ComponentCount
            {
                get { return _tss.LeadingSimulation.Manager.ComponentCount; }
            }

            #endregion

            #region Fields

            /// <summary>
            /// The TSS this wrapper is associated to.
            /// </summary>
            private readonly TSS _tss;

            #endregion

            #region Constructor

            public TSSEntityManager(TSS tss)
            {
                _tss = tss;
            }

            #endregion

            #region Logic

            /// <summary>
            /// Update all registered systems.
            /// </summary>
            /// <param name="frame">The frame in which the update is applied.</param>
            void IManager.Update(long frame)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Renders all registered systems.
            /// </summary>
            /// <param name="frame">The frame to render.</param>
            /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
            public void Draw(long frame, float elapsedMilliseconds)
            {
                _tss.LeadingSimulation.Manager.Draw(frame, elapsedMilliseconds);
            }

            #endregion

            #region Systems

            /// <summary>
            /// Add the specified system to this manager.
            /// </summary>
            /// <param name="system">The system to add.</param>
            /// <returns>
            /// This manager, for chaining.
            /// </returns>
            public IManager AddSystem(AbstractSystem system)
            {
                if (_tss.CurrentFrame > 0)
                {
                    throw new InvalidOperationException("Cannot add systems after simulation has started.");
                }

                if (system is IDrawingSystem)
                {
                    // Only insert in leading simulation.
                    _tss.LeadingSimulation.Manager.AddSystem(system);
                }
                else
                {
                    // Insert in all simulations.
                    foreach (var state in _tss._simulations)
                    {
                        state.Manager.CopySystem(system);
                    }
                }

                return this;
            }

            /// <summary>
            /// Add multiple systems to this manager.
            /// </summary>
            /// <param name="systems">The systems to add.</param>
            public void AddSystems(IEnumerable<AbstractSystem> systems)
            {
                foreach (var system in systems)
                {
                    AddSystem(system);
                }
            }

            /// <summary>
            /// Adds a copy of the specified system.
            /// </summary>
            /// <param name="system">The system to copy.</param>
            void IManager.CopySystem(AbstractSystem system)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Removes the specified system from this manager.
            /// </summary>
            /// <param name="system">The system to remove.</param>
            /// <returns>
            /// Whether the system was successfully removed.
            /// </returns>
            bool IManager.RemoveSystem(AbstractSystem system)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Get a system of the specified type.
            /// </summary>
            /// <param name="typeId">The type of the system to get.</param>
            /// <returns>
            /// The system with the specified type.
            /// </returns>
            public AbstractSystem GetSystem(int typeId)
            {
                return _tss.LeadingSimulation.Manager.GetSystem(typeId);
            }

            #endregion

            #region Entities and Components

            /// <summary>
            /// Creates a new entity and returns its ID.
            /// </summary>
            /// <returns>
            /// The id of the new entity.
            /// </returns>
            int IManager.AddEntity()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Test whether the specified entity exists.
            /// </summary>
            /// <param name="entity">The entity to check for.</param>
            /// <returns>
            /// Whether the manager contains the entity or not.
            /// </returns>
            public bool HasEntity(int entity)
            {
                return _tss.LeadingSimulation.Manager.HasEntity(entity);
            }

            /// <summary>
            /// Removes an entity and all its components from the system.
            /// </summary>
            /// <param name="entity">The entity to remove.</param>
            public void RemoveEntity(int entity)
            {
                _tss.RemoveEntity(entity, _tss.CurrentFrame);
            }

            /// <summary>
            /// Creates a new component for the specified entity.
            /// </summary>
            /// <typeparam name="T">The type of component to create.</typeparam>
            /// <param name="entity">The entity to attach the component to.</param>
            /// <returns>
            /// The new component.
            /// </returns>
            T IManager.AddComponent<T>(int entity)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Removes the specified component from the system.
            /// </summary>
            /// <param name="component">The component to remove.</param>
            void IManager.RemoveComponent(Component component)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Removes the specified component from the system.
            /// </summary>
            /// <param name="componentId">The id of the component to remove.</param>
            void IManager.RemoveComponent(int componentId)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Test whether the component with the specified id exists.
            /// </summary>
            /// <param name="componentId">The id of the component to check for.</param>
            /// <returns>
            /// Whether the manager contains the component or not.
            /// </returns>
            public bool HasComponent(int componentId)
            {
                return _tss.LeadingSimulation.Manager.HasComponent(componentId);
            }

            /// <summary>
            /// Get a component by its id.
            /// </summary>
            /// <param name="componentId">The if of the component to retrieve.</param>
            /// <returns>
            /// The component with the specified id.
            /// </returns>
            public Component GetComponentById(int componentId)
            {
                return _tss.LeadingSimulation.Manager.GetComponentById(componentId);
            }

            /// <summary>
            /// Gets the component of the specified type for an entity.
            /// </summary>
            /// <param name="entity">The entity to get the component of.</param>
            /// <param name="typeId"> </param>
            /// <returns>The component.</returns>
            public Component GetComponent(int entity, int typeId)
            {
                return _tss.LeadingSimulation.Manager.GetComponent(entity, typeId);
            }

            /// <summary>
            /// Allows enumerating over all components of the specified entity.
            /// </summary>
            /// <param name="entity">The entity for which to get the components.</param>
            /// <param name="typeId">The type of the components to get.</param>
            /// <returns>
            /// An enumerable listing all components of that entity.
            /// </returns>
            public IEnumerable<Component> GetComponents(int entity, int typeId)
            {
                return _tss.LeadingSimulation.Manager.GetComponents(entity, typeId);
            }

            #endregion

            #region Messaging

            /// <summary>
            /// Inform all interested systems of a message.
            /// </summary>
            /// <typeparam name="T">The type of the message.</typeparam>
            /// <param name="message">The sent message.</param>
            void IManager.SendMessage<T>(T message)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region Serialization / Hashing

            /// <summary>
            /// Write the object's state to the given packet.
            /// </summary>
            /// <param name="packet">The packet to write the data to.</param>
            /// <returns>
            /// The packet after writing.
            /// </returns>
            Packet IPacketizable.Packetize(Packet packet)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Bring the object to the state in the given packet.
            /// </summary>
            /// <param name="packet">The packet to read from.</param>
            void IPacketizable.Depacketize(Packet packet)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Write a complete entity, meaning all its components, to the
            /// specified packet. Entities saved this way can be restored using
            /// the <c>ReadEntity()</c> method.
            /// <para/>
            /// This uses the components' <c>Packetize</c> facilities.
            /// </summary>
            /// <param name="entity">The entity to write.</param>
            /// <param name="packet">The packet to write to.</param>
            /// <returns>
            /// The packet after writing the entity's components.
            /// </returns>
            public Packet PacketizeEntity(int entity, Packet packet)
            {
                return _tss.LeadingSimulation.Manager.PacketizeEntity(entity, packet);
            }

            /// <summary>
            /// Reads an entity from the specified packet, meaning all its
            /// components. This will create a new entity, with an id that
            /// may differ from the id the entity had when it was written.
            /// <para/>
            /// In particular, all re-created components will likely have different
            /// different ids as well, so this method is not suited for storing
            /// components that reference other components, even if just by their
            /// ID.
            /// <para/>
            /// This uses the components' <c>Depacketize</c> facilities.
            /// </summary>
            /// <param name="packet">The packet to read the entity from.</param>
            /// <returns>
            /// The id of the read entity.
            /// </returns>
            int IManager.DepacketizeEntity(Packet packet)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Push some unique data of the object to the given hasher,
            /// to contribute to the generated hash.
            /// </summary>
            /// <param name="hasher">The hasher to push data to.</param>
            void IHashable.Hash(Hasher hasher)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region Copying

            /// <summary>
            /// Creates a shallow copy of the object.
            /// </summary>
            /// <returns>The copy.</returns>
            IManager ICopyable<IManager>.NewInstance()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Creates a deep copy of the object, reusing the given object.
            /// </summary>
            /// <param name="into">The object to copy into.</param>
            /// <returns>The copy.</returns>
            void ICopyable<IManager>.CopyInto(IManager into)
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        #endregion
    }
}