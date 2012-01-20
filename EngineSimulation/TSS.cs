using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;

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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

#if DEBUG && GAMELOG
        /// <summary>
        /// Logger for game log (i.e. steps happening in a simulation).
        /// </summary>
        private static NLog.Logger gamelog = NLog.LogManager.GetLogger("GameLog.Simulation");
#endif

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

#if DEBUG && GAMELOG
        /// <summary>
        /// Whether to log any game state changes in detail, for debugging.
        /// </summary>
        public bool GameLogEnabled { get; set; }
#endif

        /// <summary>
        /// The component system manager in use in this simulation.
        /// </summary>
        public IEntityManager EntityManager { get; private set; }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        public bool WaitingForSynchronization { get; private set; }

        /// <summary>
        /// Get the trailing state.
        /// </summary>
        private IAuthoritativeSimulation TrailingState { get { return _simulations[_simulations.Length - 1]; } }

        /// <summary>
        /// Get the leading state.
        /// </summary>
        private IAuthoritativeSimulation LeadingState { get { return _simulations[0]; } }

        #endregion

        #region Fields

        /// <summary>
        /// The delays of the individual states.
        /// </summary>
        private uint[] _delays;

#if TSS_THREADING
        /// <summary>
        /// The parameterization for the different threads.
        /// </summary>
        private ThreadData[] _threadData;
#endif

        /// <summary>
        /// The list of running states. They are ordered in in increasing delay, i.e.
        /// the state at slot 0 is the leading one, 1 is the next newest, and so on.
        /// </summary>
        private IAuthoritativeSimulation[] _simulations;

        /// <summary>
        /// List of objects to add to delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<Entity>> _adds = new Dictionary<long, List<Entity>>();

        /// <summary>
        /// List of object ids to remove from delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<int>> _removes = new Dictionary<long, List<int>>();

        /// <summary>
        /// List of commands to execute in delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<Command>> _commands = new Dictionary<long, List<Command>>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TSS based meta state.
        /// </summary>
        /// <param name="delays">The delays to use for trailing states, with the delays in frames.</param>
        public TSS(uint[] delays)
        {
            _delays = new uint[delays.Length + 1];
            delays.CopyTo(_delays, 1);
            Array.Sort(_delays);

#if TSS_THREADING
            // Initialize thread data. The trailing state will always be
            // updated by the main thread, to check if a rollback is required,
            // so we need one less than we have states.
            _threadData = new ThreadData[_delays.Length - 1];
#endif

            // Generate initial states.
            _simulations = new IAuthoritativeSimulation[_delays.Length];

            // Our pass-through component manager, which allows adding and
            // removing only in the first frame (i.e. before the first update).
            EntityManager = new TSSEntityManager(this);

            // Mark us for need of sync.
            WaitingForSynchronization = true;
        }

        #endregion

        #region Invalidation / (Re-)Initialization

        /// <summary>
        /// Initialize the TSS to the given state. This also clears the
        /// <c>WaitingForSynchronization</c> flag.
        /// </summary>
        /// <param name="state">the state to initialize this TSS to.</param>
        public void Initialize(IAuthoritativeSimulation state)
        {
#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Initializing TSS.");
            }
#endif
            MirrorSimulation(state, _simulations.Length - 1);
            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Mark the state as invalid (desynchronized). Will trigger a new
        /// <c>ThresholdExceeded</c> event.
        /// </summary>
        public void Invalidate()
        {
#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Invalidating TSS.");
            }
#endif
            OnInvalidated(EventArgs.Empty);
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// Push a command to all sub states.
        /// 
        /// This will lead to a rollback of all states that have already passed
        /// the command's frame. They will be fast-forwarded appropriately in
        /// the next Step().
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
#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Pushing command to frame {0}: {1}", frame, command);
            }
#endif

            // Check if we can possibly apply this command.
            if (frame >= TrailingFrame)
            {
                // Store it to be removed in trailing states.
                if (!_commands.ContainsKey(frame))
                {
                    // No such command yet, push it.
                    _commands.Add(frame, new List<Command>());
                }
                // We don't need to check for duplicate / replacing authoritative here,
                // because the sub-state will do that itself.
                _commands[frame].Add(command);

                // Rewind to the frame to retroactively apply changes.
                if (frame < CurrentFrame)
                {
                    Rewind(frame);
                }
            }
            else if (command.IsAuthoritative)
            {
                // Cannot apply, and this is an authoritative command. We need to sync.
                OnInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Advance leading state to <c>CurrentFrame + 1</c> and update all trailing
        /// states accordingly.
        /// </summary>
        public void Update()
        {
            // Advance the simulation.
            FastForward(++CurrentFrame);
        }

        #endregion

        #region Time specific adding / removal of entities

        /// <summary>
        /// Add an object in a specific time frame. This will roll back, if
        /// necessary, to insert the object, meaning it can trigger invalidation.
        /// </summary>
        /// <param name="entity">the object to insert.</param>
        /// <param name="frame">the frame to insert it at.</param>
        public void AddEntity(Entity entity, long frame)
        {
            // Store it to be inserted in trailing states.
            if (!_adds.ContainsKey(frame))
            {
                _adds.Add(frame, new List<Entity>());
            }
            else if (_adds[frame].Contains(entity))
            {
                // Don't insert the same add to the list twice.
                return;
            }
            else if (_removes.ContainsKey(frame) && _removes[frame].Contains(entity.UID))
            {
                // Do not allow removal and adding of the same object in the same
                // frame, as this can lead to unexpected behavior (may not happen
                // in the intended order!)
                throw new InvalidOperationException("Cannot add an object in the same frame as it will be removed.");
            }
            _adds[frame].Add(entity.DeepCopy());

            // Rewind to the frame to retroactively apply changes.
            if (frame < CurrentFrame)
            {
                Rewind(frame);
            }
        }

        /// <summary>
        /// Remove an object in a specific time frame. This will roll back, if
        /// necessary, to remove the object, meaning it can trigger invalidation.
        /// </summary>
        /// <param name="entityId">the id of the object to remove.</param>
        /// <param name="frame">the frame to remove it at.</param>
        public void RemoveEntity(int entityUid, long frame)
        {
            // Store it to be removed in trailing states.
            if (!_removes.ContainsKey(frame))
            {
                _removes.Add(frame, new List<int>());
            }
            else if (_removes[frame].Contains(entityUid))
            {
                // Don't insert the same remove to the list twice.
                return;
            }
            else if (_adds.ContainsKey(frame) && _adds[frame].Find(a => a.UID == entityUid) != null)
            {
                // Do not allow removal and adding of the same object in the same
                // frame, as this can lead to unexpected behavior (may not happen
                // in the intended order!)
                throw new InvalidOperationException("Cannot remove an object in the same frame as it was added.");
            }
            _removes[frame].Add(entityUid);

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
        /// Serialize a state to a packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public Packet Packetize(Packet packet)
        {
            packet.Write(CurrentFrame)
                .Write(_simulations[_simulations.Length - 1])

                .Write(_adds.Count);
            foreach (var add in _adds)
            {
                packet.Write(add.Key);
                packet.Write(add.Value);
            }

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

            packet.Write(_commands.Count);
            foreach (var command in _commands)
            {
                packet.Write(command.Key);
                packet.WriteWithTypeInfo(command.Value);
            }

            return packet;
        }

        /// <summary>
        /// Deserialize a state from a packet.
        /// </summary>
        /// <param name="packet">the packet to read the data from.</param>
        public void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Unwrap the trailing state and mirror it to all the newer ones.
            packet.ReadPacketizableInto(_simulations[_simulations.Length - 1]);
            MirrorSimulation(_simulations[_simulations.Length - 1], _simulations.Length - 2);

            // Find adds / removes / commands that our out of date now, but keep newer ones.
            PrunePastEvents();

            // Continue with reading the list of adds.
            int numAdds = packet.ReadInt32();
            for (int addIdx = 0; addIdx < numAdds; ++addIdx)
            {
                long key = packet.ReadInt64();
                if (!_adds.ContainsKey(key))
                {
                    _adds.Add(key, new List<Entity>());
                }
                _adds[key].AddRange(packet.ReadPacketizables<Entity>());
            }

            // Then the removes.
            int numRemoves = packet.ReadInt32();
            for (int removeIdx = 0; removeIdx < numRemoves; ++removeIdx)
            {
                long key = packet.ReadInt64();
                if (!_removes.ContainsKey(key))
                {
                    _removes.Add(key, new List<int>());
                }
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    _removes[key].Add(packet.ReadInt32());
                }
            }

            // And finally the commands.
            int numCommands = packet.ReadInt32();
            for (int commandIdx = 0; commandIdx < numCommands; ++commandIdx)
            {
                long key = packet.ReadInt64();
                if (!_commands.ContainsKey(key))
                {
                    _commands.Add(key, new List<Command>());
                }
                foreach (var command in packet.ReadPacketizablesWithTypeInfo<Command>())
                {
                    _commands[key].Add(command);
                }
            }

            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            TrailingState.Hash(hasher);
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public ISimulation DeepCopy()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public ISimulation DeepCopy(ISimulation into)
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
        /// <param name="frame">the frame up to which to run.</param>
        private void FastForward(long frame)
        {
#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Fast-forwarding TSS to frame {0}.", frame);
            }
#endif

            // Start threads for the non-trailing frames.
#if TSS_THREADING
            var tasks = new System.Threading.Tasks.Task[_threadData.Length];
            for (int i = 0; i < _threadData.Length; i++)
            {
                _threadData[i].simulation = _simulations[i];
                _threadData[i].frame = frame - _delays[i];
                tasks[i] = TaskStarter(_threadData[i]);
            }
#endif

#if DEBUG && !TSS_THREADING && GAMELOG
            // Enable logging for the trailing state, and the trailing state
            // only (so we only get the output of one simulation, the one
            // that has the last say!)
            TrailingState.GameLogEnabled = true;
#endif
            // Process the trailing state, see if we need a roll-back.
            bool needsRewind = false;
            while (TrailingState.CurrentFrame + _delays[_simulations.Length - 1] < frame)
            {
                // It needs running, so prepare it for that.
                PrepareForUpdate(TrailingState);

                // Then check if any of the commands were tentative.
                if (TrailingState.SkipTentativeCommands())
                {
                    needsRewind = true;
                }

                // Do the actual stepping for the state.
                TrailingState.Update();
            }

#if TSS_THREADING
            // Wait for our worker threads to finish.
            System.Threading.Tasks.Task.WaitAll(tasks);

            // Check if we had trailing tentative commands.
            if (needsRewind)
            {
                logger.Trace("Pruned non-authoritative commands, mirroring trailing state.");
                MirrorSimulation(TrailingState, _simulations.Length - 2);

                // Update the other states once more.
                FastForward(frame);
            }
            else
            {
                // Clean up stuff that's too old to keep.
                PrunePastEvents();
            }
#else
            // Check if we had trailing tentative commands.
            if (needsRewind)
            {
                logger.Trace("Pruned non-authoritative commands, mirroring trailing state.");
                MirrorSimulation(TrailingState, _simulations.Length - 2);
            }

            // Fast-forward the remaining states. Do not log in those, we only
            // want to log the trailing state.
            for (int i = 0; i < _simulations.Length - 1; i++)
            {
                while (_simulations[i].CurrentFrame < frame - _delays[i])
                {
                    PrepareForUpdate(_simulations[i]);
                    _simulations[i].Update();
                }
            }

            // Clean up stuff that's too old to keep.
            PrunePastEvents();
#endif
        }

        /// <summary>
        /// Prepares a simulation for its next update, by pushing commands for
        /// that frame, as well as adding and removing entities.
        /// </summary>
        /// <param name="simulation">The simulation to prepare.</param>
        private void PrepareForUpdate(IAuthoritativeSimulation simulation)
        {
            // The frame the state is now in, and that will be executed.
            long frame = simulation.CurrentFrame;

            // Check if we need to add objects.
            if (_adds.ContainsKey(frame))
            {
                // Add a copy of it.
                foreach (var entity in _adds[frame])
                {
                    simulation.EntityManager.AddEntity(entity.DeepCopy());
                }
            }

            // Check if we need to remove objects.
            if (_removes.ContainsKey(frame))
            {
                // Add a copy of it.
                foreach (var entityUid in _removes[frame])
                {
                    simulation.EntityManager.RemoveEntity(entityUid);
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

#if TSS_THREADING
        /// <summary>
        /// Utility method for argument binding.
        /// </summary>
        /// <param name="data">The thread data to bind.</param>
        /// <returns>A new task for the given thread data.</returns>
        private System.Threading.Tasks.Task TaskStarter(ThreadData data)
        {
            return System.Threading.Tasks.Task.Factory.StartNew(() => ThreadedUpdate(data));
        }

        /// <summary>
        /// Perform a threaded update of a simulation.
        /// </summary>
        /// <param name="data"></param>
        private void ThreadedUpdate(object data)
        {
            var info = (ThreadData)data;

            try
            {
                while (info.simulation.CurrentFrame < info.frame)
                {
                    PrepareForUpdate(info.simulation);
                    info.simulation.Update();
                }
            }
            catch (Exception ex)
            {
                logger.WarnException("Error in threaded update.", ex);
                throw;
            }
        }
#endif

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
#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Rewinding TSS to frame {0}.", frame);
            }
#endif

            // Find first state that's not past the frame.
            for (int i = 0; i < _simulations.Length; ++i)
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
        private void MirrorSimulation(IAuthoritativeSimulation state, int start)
        {
            for (int i = start; i >= 0; --i)
            {
                _simulations[i] = (IAuthoritativeSimulation)state.DeepCopy(_simulations[i]);
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
            List<long> deprecated = new List<long>();
            foreach (var key in _adds.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                _adds.Remove(key);
            }

            deprecated.Clear();
            foreach (var key in _removes.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                _removes.Remove(key);
            }

            deprecated.Clear();
            foreach (var key in _commands.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                _commands.Remove(key);
            }
        }

#if TSS_THREADING
        #region Thread parameter wrapper

        /// <summary>
        /// Wrapper for parameters passed to a updater thread.
        /// </summary>
        private struct ThreadData
        {
            /// <summary>
            /// The simulation to update.
            /// </summary>
            public IAuthoritativeSimulation simulation;

            /// <summary>
            /// The frame to run to.
            /// </summary>
            public long frame;
        }

        #endregion
#endif

        #endregion

        #region Manager-Wrappers

        private class TSSEntityManager : IEntityManager
        {
            #region Properties

            public IComponentSystemManager SystemManager { get { return _systemManager; } set { throw new NotSupportedException(); } }

            #endregion

            #region Fields

            /// <summary>
            /// The TSS this wrapper is associated to.
            /// </summary>
            private TSS _tss;

            private IComponentSystemManager _systemManager;

            #endregion

            #region Constructor

            public TSSEntityManager(TSS tss)
            {
                this._tss = tss;
                this._systemManager = new TSSComponentSystemManager(_tss);
            }

            #endregion

            #region Entities

            public int AddEntity(Entity entity)
            {
                _tss.AddEntity(entity, _tss.CurrentFrame);
                return -1;
            }

            public Entity RemoveEntity(int entityUid)
            {
                _tss.RemoveEntity(entityUid, _tss.CurrentFrame);
                return null;
            }

            public Entity GetEntity(int entityUid)
            {
                return _tss.LeadingState.EntityManager.GetEntity(entityUid);
            }

            public bool Contains(int entityUid)
            {
                return _tss.LeadingState.EntityManager.Contains(entityUid);
            }

            #endregion

            #region Unsupported

            public event EventHandler<EntityEventArgs> Added;

            public event EventHandler<EntityEventArgs> Removed;

            public ReadOnlyCollection<Entity> Entities { get { throw new NotSupportedException(); } }

#if DEBUG && GAMELOG
            public bool GameLogEnabled
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
#endif

            public void RemoveEntity(Entity entity)
            {
                throw new NotSupportedException();
            }

            public Packet Packetize(Packet packet)
            {
                throw new NotSupportedException();
            }

            public void Depacketize(Packet packet)
            {
                throw new NotSupportedException();
            }

            public void Hash(Hasher hasher)
            {
                throw new NotSupportedException();
            }

            public IEntityManager DeepCopy()
            {
                throw new NotSupportedException();
            }

            public IEntityManager DeepCopy(IEntityManager into)
            {
                throw new NotSupportedException();
            }

            public void SendEntityMessage<T>(ref T message) where T : struct
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// Helper for system initialization and accessing systems of the leading state.
        /// </summary>
        private class TSSComponentSystemManager : IComponentSystemManager
        {
            #region Fields

            /// <summary>
            /// The TSS this wrapper is associated to.
            /// </summary>
            private TSS _tss;

            #endregion

            #region Constructor

            public TSSComponentSystemManager(TSS tss)
            {
                this._tss = tss;
            }

            #endregion

            #region Interfaces

            public IComponentSystemManager AddSystem(IComponentSystem system)
            {
                if (_tss.CurrentFrame > 0)
                {
                    throw new InvalidOperationException("Cannot add systems after simulation has started.");
                }
                foreach (var state in _tss._simulations)
                {
                    state.EntityManager.SystemManager.AddSystem(system.DeepCopy());
                }
                return this;
            }

            public void AddSystems(IEnumerable<IComponentSystem> systems)
            {
                foreach (var system in systems)
                {
                    AddSystem(system);
                }
            }

            public T GetSystem<T>() where T : IComponentSystem
            {
                return _tss.LeadingState.EntityManager.SystemManager.GetSystem<T>();
            }

            /// <summary>
            /// Only render passes supported, based on leading state.
            /// </summary>
            public void Draw(GameTime gameTime, long frame)
            {
                _tss.LeadingState.EntityManager.SystemManager.Draw(gameTime, frame);
            }

            #endregion

            #region Unsupported

            public ReadOnlyCollection<IComponentSystem> Systems
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public IEntityManager EntityManager
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public void Update(long frame)
            {
                throw new NotSupportedException();
            }

            public void RemoveSystem(IComponentSystem system)
            {
                throw new NotSupportedException();
            }

            public IComponentSystemManager AddComponent(AbstractComponent component)
            {
                throw new NotSupportedException();
            }

            public void RemoveComponent(AbstractComponent component)
            {
                throw new NotSupportedException();
            }

            public void ClearComponents()
            {
                throw new NotSupportedException();
            }

            public void SendSystemMessage<T>(ref T message) where T : struct
            {
                throw new NotSupportedException();
            }

            public void SendComponentMessage<T>(ref T message) where T : struct
            {
                throw new NotImplementedException();
            }

            public Packet Packetize(Packet packet)
            {
                throw new NotSupportedException();
            }

            public void Depacketize(Packet packet)
            {
                throw new NotSupportedException();
            }

            public void Hash(Hasher hasher)
            {
                throw new NotSupportedException();
            }

            public IComponentSystemManager DeepCopy()
            {
                throw new NotSupportedException();
            }

            public IComponentSystemManager DeepCopy(IComponentSystemManager into)
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        #endregion
    }
}