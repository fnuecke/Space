using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Implements a Trailing State Synchronization.
    /// </summary>
    /// <see cref="http://warriors.eecs.umich.edu/games/papers/netgames02-tss.pdf"/>
    public sealed class TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> :
        IReversibleState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
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
        public long TrailingFrame { get { return states[states.Length - 1].CurrentFrame; } }

        /// <summary>
        /// Enumerator over all children of the leading state.
        /// </summary>
        public IEnumerable<TSteppable> Children { get { return LeadingState.Children; } }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        public bool WaitingForSynchronization { get; private set; }

        /// <summary>
        /// Packetizer used for serialization purposes.
        /// </summary>
        public IPacketizer<TPlayerData, TPacketizerContext> Packetizer { get { return LeadingState.Packetizer; } }

        /// <summary>
        /// Get the trailing state.
        /// </summary>
        private TState TrailingState { get { return states[states.Length - 1]; } }

        /// <summary>
        /// Get the leading state.
        /// </summary>
        private TState LeadingState { get { return states[0]; } }

        #endregion

        #region Fields

        /// <summary>
        /// The delays of the individual states.
        /// </summary>
        private uint[] delays;

        /// <summary>
        /// The list of running states. They are ordered in in increasing delay, i.e.
        /// the state at slot 0 is the leading one, 1 is the next newest, and so on.
        /// </summary>
        private TState[] states;

        /// <summary>
        /// List of objects to add to delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<TSteppable>> adds = new Dictionary<long, List<TSteppable>>();

        /// <summary>
        /// List of object ids to remove from delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<long>> removes = new Dictionary<long, List<long>>();

        /// <summary>
        /// List of commands to execute in delayed states when they reach the given frame.
        /// </summary>
        private Dictionary<long, List<ICommand<TCommandType, TPlayerData, TPacketizerContext>>> commands = new Dictionary<long, List<ICommand<TCommandType, TPlayerData, TPacketizerContext>>>();

        #endregion

        /// <summary>
        /// Creates a new TSS based meta state.
        /// </summary>
        /// <param name="delays">The delays to use for trailing states, with the delays in frames.</param>
        public TSS(uint[] delays)
        {
            this.delays = new uint[delays.Length + 1];
            delays.CopyTo(this.delays, 1);
            Array.Sort(this.delays);

            // Generate initial states.
            states = new TState[this.delays.Length];

            // Mark us for need of sync.
            WaitingForSynchronization = true;
        }

        /// <summary>
        /// Initialize the TSS to the given state. This also clears the
        /// <c>WaitingForSynchronization</c> flag.
        /// </summary>
        /// <param name="state">the state to initialize this TSS to.</param>
        public void Initialize(TState state)
        {
            MirrorState(state, states.Length - 1);
            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Mark the state as invalid (desynchronized). Will trigger a new
        /// <c>ThresholdExceeded</c> event.
        /// </summary>
        public void Invalidate()
        {
            OnInvalidated(EventArgs.Empty);
        }

        /// <summary>
        /// Add a new object.
        /// 
        /// This will add the object to the leading state, and add it to the delayed
        /// states when they reach the current frame of the leading state.
        /// </summary>
        /// <param name="steppable">the object to add.</param>
        public void AddSteppable(TSteppable steppable)
        {
            AddSteppable(steppable, CurrentFrame);
        }

        /// <summary>
        /// Add an object in a specific time frame. This will roll back, if
        /// necessary, to insert the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppable">the object to insert.</param>
        /// <param name="frame">the frame to insert it at.</param>
        public void AddSteppable(TSteppable steppable, long frame)
        {
            // Store it to be inserted in trailing states.
            if (!adds.ContainsKey(frame))
            {
                adds.Add(frame, new List<TSteppable>());
            }
            else if (adds[frame].Contains(steppable))
            {
                // Don't insert the same add to the list twice.
                return;
            }
            else if (removes.ContainsKey(frame) && removes[frame].Contains(steppable.UID))
            {
                // Do not allow removal and adding of the same object in the same
                // frame, as this can lead to unexpected behavior (may not happen
                // in the intended order!)
                throw new InvalidOperationException("Cannot add an object in the same frame as it will be removed.");
            }
            adds[frame].Add((TSteppable)steppable.Clone());

            // Rewind to the frame to retroactively apply changes.
            if (frame < CurrentFrame)
            {
                Rewind(frame);
            }
        }

        /// <summary>
        /// Not supported for TSS.
        /// </summary>
        public void RemoveSteppable(TSteppable steppable)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove a steppable object by its id. Will always return <c>null</c> for TSS.
        /// </summary>
        /// <param name="steppableUid">the remove object.</param>
        public TSteppable RemoveSteppable(long steppableUid)
        {
            RemoveSteppable(steppableUid, CurrentFrame);
            return default(TSteppable);
        }

        /// <summary>
        /// Remove an object in a specific time frame. This will roll back, if
        /// necessary, to remove the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppableId">the id of the object to remove.</param>
        /// <param name="frame">the frame to remove it at.</param>
        public void RemoveSteppable(long steppableUid, long frame)
        {
            // Store it to be removed in trailing states.
            if (!removes.ContainsKey(frame))
            {
                removes.Add(frame, new List<long>());
            }
            else if (removes[frame].Contains(steppableUid))
            {
                // Don't insert the same remove to the list twice.
                return;
            }
            else if (adds.ContainsKey(frame) && adds[frame].Find(a => a.UID == steppableUid) != null)
            {
                // Do not allow removal and adding of the same object in the same
                // frame, as this can lead to unexpected behavior (may not happen
                // in the intended order!)
                throw new InvalidOperationException("Cannot remove an object in the same frame as it was added.");
            }
            removes[frame].Add(steppableUid);

            // Rewind to the frame to retroactively apply changes.
            if (frame < CurrentFrame)
            {
                Rewind(frame);
            }
        }

        /// <summary>
        /// Get a steppable's current representation in this state by its id.
        /// </summary>
        /// <param name="steppableUid">the id of the steppable to look up.</param>
        /// <returns>the current representation in this state.</returns>
        public TSteppable GetSteppable(long steppableUid)
        {
            return LeadingState.GetSteppable(steppableUid);
        }

        /// <summary>
        /// Push a command to all sub states.
        /// 
        /// This will lead to a rollback of all states that have already passed
        /// the command's frame. They will be fast-forwarded appropriately in
        /// the next Step().
        /// </summary>
        /// <param name="command">the command to push.</param>
        public void PushCommand(ICommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            PushCommand(command, CurrentFrame);
        }

        /// <summary>
        /// Push a command to be executed at the given frame.  This will roll
        /// back, if necessary, to remove the object, meaning it can trigger
        /// desyncs.
        /// </summary>
        /// <param name="command">the command to push.</param>
        /// <param name="frame">the frame in which to execute the command.</param>
        public void PushCommand(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, long frame)
        {
            // Store it to be removed in trailing states.
            if (!commands.ContainsKey(frame))
            {
                // No such command yet, push it.
                commands.Add(frame, new List<ICommand<TCommandType, TPlayerData, TPacketizerContext>>());
            }
            // We don't need to check for duplicate / replacing authoritative here,
            // because the sub-state will do that itself.
            commands[frame].Add(command);

            // Rewind to the frame to retroactively apply changes.
            if (frame < CurrentFrame)
            {
                Rewind(frame);
            }
        }

        /// <summary>
        /// Advance all states
        /// </summary>
        public void Update()
        {
            // Advance the simulation.
            FastForward(++CurrentFrame);
        }

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
        public object Clone()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Serialize a state to a packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public void Packetize(Packet packet)
        {
            packet.Write(CurrentFrame);

            states[states.Length - 1].Packetize(packet);

            packet.Write(adds.Count);
            foreach (var add in adds)
            {
                packet.Write(add.Key);
                packet.Write(add.Value.Count);
                foreach (var item in add.Value)
                {
                    Packetizer.Packetize(item, packet);
                }
            }

            packet.Write(removes.Count);
            foreach (var remove in removes)
            {
                packet.Write(remove.Key);
                packet.Write(remove.Value.Count);
                foreach (var item in remove.Value)
                {
                    packet.Write(item);
                }
            }

            packet.Write(commands.Count);
            foreach (var command in commands)
            {
                packet.Write(command.Key);
                packet.Write(command.Value.Count);
                foreach (var item in command.Value)
                {
                    Packetizer.Packetize(item, packet);
                }
            }
        }

        /// <summary>
        /// Deserialize a state from a packet.
        /// </summary>
        /// <param name="packet">the packet to read the data from.</param>
        public void Depacketize(Packet packet, TPacketizerContext context)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Unwrap the trailing state and mirror it to all the newer ones.
            states[states.Length - 1].Depacketize(packet, context);
            MirrorState(states[states.Length - 1], states.Length - 2);

            // Find adds / removes / commands that our out of date now, but keep newer ones.
            PrunePastEvents();

            // Continue with reading the list of adds.
            int numAdds = packet.ReadInt32();
            for (int addIdx = 0; addIdx < numAdds; ++addIdx)
            {
                long key = packet.ReadInt64();
                if (!adds.ContainsKey(key))
                {
                    adds.Add(key, new List<TSteppable>());
                }
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    adds[key].Add(Packetizer.Depacketize<TSteppable>(packet));
                }
            }

            // Then the removes.
            int numRemoves = packet.ReadInt32();
            for (int removeIdx = 0; removeIdx < numRemoves; ++removeIdx)
            {
                long key = packet.ReadInt64();
                if (!removes.ContainsKey(key))
                {
                    removes.Add(key, new List<long>());
                }
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    removes[key].Add(packet.ReadInt64());
                }
            }

            // And finally the commands.
            int numCommands = packet.ReadInt32();
            for (int commandIdx = 0; commandIdx < numCommands; ++commandIdx)
            {
                long key = packet.ReadInt64();
                if (!commands.ContainsKey(key))
                {
                    commands.Add(key, new List<ICommand<TCommandType, TPlayerData, TPacketizerContext>>());
                }
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    commands[key].Add(Packetizer.Depacketize<ICommand<TCommandType, TPlayerData, TPacketizerContext>>(packet));
                }
            }

            WaitingForSynchronization = false;
        }

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
            // Update states. Run back to front, to allow rewinding future states
            // (if the trailing state must skip tentative commands) all in one go.
            for (int i = states.Length - 1; i >= 0; --i)
            {
                // Check if we need to rewind because the trailing state was left
                // with a tentative command.
                bool needsRewind = false;

                // Update while we're still delaying.
                while (states[i].CurrentFrame + delays[i] < frame)
                {
                    // If this is the trailing state, don't bring in tentative
                    // commands. Prune them instead. If there were any, rewind
                    // to apply that removal retroactively. Do this after the
                    // loop, though, to avoid unnecessary work.
                    if (i == states.Length - 1 && states[i].SkipTentativeCommands())
                    {
                        needsRewind = true;
                    }

                    // Apply the update for the state's current frame.
                    UpdateSubstate(states[i]);
                }

                // Check if we had trailing tentative commands.
                if (needsRewind)
                {
                    MirrorState(states[states.Length - 1], states.Length - 2);
                }
            }

            // Clean up stuff that's too old to keep.
            PrunePastEvents();
        }

        /// <summary>
        /// Perform an update to a sub-state, applying all adds/removes/commands to it.
        /// </summary>
        /// <param name="state">the state to update.</param>
        private void UpdateSubstate(TState state)
        {
            // The frame the state is now in, and that will be executed.
            long stateFrame = state.CurrentFrame;

            // Check if we need to add objects.
            if (adds.ContainsKey(stateFrame))
            {
                // Add a copy of it.
                foreach (var steppable in adds[stateFrame])
                {
                    state.AddSteppable((TSteppable)steppable.Clone());
                }
            }

            // Check if we need to remove objects.
            if (removes.ContainsKey(stateFrame))
            {
                // Add a copy of it.
                foreach (var steppableUid in removes[stateFrame])
                {
                    state.RemoveSteppable(steppableUid);
                }
            }

            // Check if we have commands to execute in that frame.
            if (commands.ContainsKey(stateFrame))
            {
                foreach (var command in commands[stateFrame])
                {
                    state.PushCommand(command);
                }
            }

            // Do the actual stepping for the state.
            state.Update();
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
            for (int i = 0; i < states.Length; ++i)
            {
                if (states[i].CurrentFrame <= frame)
                {
                    // Success, mirror the state to all newer ones.
                    MirrorState(states[i], i - 1);
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
        private void MirrorState(TState state, int start)
        {
            for (int i = start; i >= 0; --i)
            {
                states[i] = (TState)state.Clone();
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
            foreach (var key in adds.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                adds.Remove(key);
            }

            deprecated.Clear();
            foreach (var key in removes.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                removes.Remove(key);
            }

            deprecated.Clear();
            foreach (var key in commands.Keys)
            {
                if (key < TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                commands.Remove(key);
            }
        }
    }
}