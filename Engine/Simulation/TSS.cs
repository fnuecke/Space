using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Implements a Trailing State Synchronization.
    /// </summary>
    /// <see cref="http://warriors.eecs.umich.edu/games/papers/netgames02-tss.pdf"/>
    public class TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : IReversibleState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        #region Events
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate an authoritative command. The handler must trigger the
        /// process of getting a valid snapshot, which is fed back to this
        /// state using the <c>Synchronize()</c> method.
        /// </summary>
        public event EventHandler<EventArgs> ThresholdExceeded;

        #endregion

        #region Properties

        /// <summary>
        /// Enumerator over all children of the leading state.
        /// </summary>
        public IEnumerable<TSteppable> Children { get { return LeadingState.Children; } }

        /// <summary>
        /// The current frame of the leading state.
        /// </summary>
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// Packetizer used for serialization purposes.
        /// </summary>
        public IPacketizer<TPacketizerContext> Packetizer { get { return LeadingState.Packetizer; } }

        /// <summary>
        /// The frame number of the trailing state, i.e. the point we cannot roll
        /// back past.
        /// </summary>
        public long TrailingFrame { get { return states[states.Length - 1].CurrentFrame; } }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        public bool WaitingForSynchronization { get; protected set; }

        /// <summary>
        /// Get the leading state.
        /// </summary>
        public TState LeadingState { get { return states[0]; } }

        #endregion

        #region Fields

        /// <summary>
        /// The list of running states. They are ordered in in increasing delay, i.e.
        /// the state at slot 0 is the leading one, 1 is the next newest, and so on.
        /// </summary>
        protected TState[] states;

        /// <summary>
        /// The delays of the individual states.
        /// </summary>
        protected uint[] delays;

        /// <summary>
        /// List of objects to add to delayed states when they reach the given frame.
        /// </summary>
        protected Dictionary<long, List<TSteppable>> adds = new Dictionary<long, List<TSteppable>>();

        /// <summary>
        /// List of object ids to remove from delayed states when they reach the given frame.
        /// </summary>
        protected Dictionary<long, List<long>> removes = new Dictionary<long, List<long>>();

        #endregion

        /// <summary>
        /// Creates a new TSS based meta state.
        /// </summary>
        /// <param name="delays">The delays to use for trailing states, with the delays in frames.</param>
        public TSS(uint[] delays, TState initialState)
        {
            this.delays = new uint[delays.Length + 1];
            delays.CopyTo(this.delays, 1);
            Array.Sort(this.delays);

            // Generate initial states.
            states = new TState[this.delays.Length];

            // Initialize to empty state.
            MirrorState(initialState, states.Length - 1);
        }

        /// <summary>
        /// Add an object in a specific time frame. This will roll back, if
        /// necessary, to insert the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppable">the object to insert.</param>
        /// <param name="frame">the frame to insert it at.</param>
        public void Add(TSteppable steppable, long frame)
        {
            // Remember original frame.
            long currentFrame = CurrentFrame;

            if (frame < CurrentFrame)
            {
                // Rewind to the frame we need to insert in.
                Rewind(frame);
            }

            if (frame == CurrentFrame)
            {
                // Live insert, add to current state, too.
                LeadingState.Add((TSteppable)steppable.Clone());
            }

            // Store it to be inserted in trailing states.
            if (!adds.ContainsKey(frame))
            {
                adds.Add(frame, new List<TSteppable>());
            }
            adds[frame].Add((TSteppable)steppable.Clone());

            // Fast forward again.
            if (!WaitingForSynchronization)
            {
                FastForward(currentFrame);
            }
        }

        /// <summary>
        /// Add a new object.
        /// 
        /// This will add the object to the leading state, and add it to the delayed
        /// states when they reach the current frame of the leading state.
        /// </summary>
        /// <param name="steppable">the object to add.</param>
        public void Add(TSteppable steppable)
        {
            Add(steppable, CurrentFrame);
        }

        /// <summary>
        /// Remove an object in a specific time frame. This will roll back, if
        /// necessary, to remove the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppableId">the id of the object to remove.</param>
        /// <param name="frame">the frame to remove it at.</param>
        public void Remove(long steppableUid, long frame)
        {
            // Remember original frame.
            long currentFrame = CurrentFrame;

            if (frame < CurrentFrame)
            {
                // Rewind to the frame we need to insert in.
                Rewind(frame);
            }

            if (frame == CurrentFrame)
            {
                // Live insert, add to current state, too.
                LeadingState.Remove(steppableUid);
            }

            // Store it to be removed in trailing states.
            if (!removes.ContainsKey(frame))
            {
                removes.Add(frame, new List<long>());
            }
            removes[frame].Add(steppableUid);

            // Fast forward again.
            if (!WaitingForSynchronization)
            {
                FastForward(currentFrame);
            }
        }

        /// <summary>
        /// Not supported for TSS.
        /// </summary>
        public void Remove(TSteppable steppable)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove a steppable object by its id. Will always return <c>null</c> for TSS.
        /// </summary>
        /// <param name="steppableUid">the remove object.</param>
        public TSteppable Remove(long steppableUid)
        {
            Remove(steppableUid, CurrentFrame);

            return default(TSteppable);
        }
        
        /// <summary>
        /// Get a steppable's current representation in this state by its id.
        /// </summary>
        /// <param name="steppableUid">the id of the steppable to look up.</param>
        /// <returns>the current representation in this state.</returns>
        public TSteppable Get(long steppableUid)
        {
            return LeadingState.Get(steppableUid);
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
                CurrentFrame = frame;
            }
            else if (CurrentFrame < TrailingFrame)
            {
                // Cannot rewind that far, request resync.
                OnThresholdExceeded(new ThresholdExceededEventArgs());
            }
            else
            {
                // In range for reverting.
                Rewind(frame);
                CurrentFrame = frame;
            }
        }

        /// <summary>
        /// Push a command to all sub states.
        /// 
        /// This will lead to a rollback of all states that have already passed
        /// the command's frame. They will be fast-forwarded appropriately in
        /// the next Step().
        /// </summary>
        /// <param name="command">the command to push.</param>
        public void PushCommand(ISimulationCommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException();
            }

            // Check if we have a chance of applying this state.
            if (command.Frame <= TrailingFrame)
            {
                // Not a chance. If it's a server command we have to rewind.
                if (!command.IsTentative)
                {
                    OnThresholdExceeded(new ThresholdExceededEventArgs());
                }
                return;
            }

            // Passed checks, so rewinding will work.
            Rewind(command.Frame);
            for (int i = 0; i < states.Length; ++i)
            {
                states[i].PushCommand(command);
            }
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
        }

        /// <summary>
        /// Deserialize a state from a packet.
        /// </summary>
        /// <param name="packet">the packet to read the data from.</param>
        public void Depacketize(Packet packet, TPacketizerContext context)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Find adds / removes that our out of date now, but keep newer ones.
            List<long> deprecated = new List<long>();
            foreach (var key in adds.Keys)
            {
                if (key <= CurrentFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var frame in deprecated)
            {
                adds.Remove(frame);
            }

            deprecated.Clear();
            foreach (var key in removes.Keys)
            {
                if (key <= CurrentFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var frame in deprecated)
            {
                removes.Remove(frame);
            }

            // Unwrap the trailing state and mirror it to all the newer ones.
            states[states.Length - 1].Depacketize(packet, context);
            MirrorState(states[states.Length - 1], states.Length - 2);

            // Continue with reading the list of adds / removes.
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

            WaitingForSynchronization = false;
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public object Clone()
        {
            throw new NotImplementedException();
        }
        
        protected void OnThresholdExceeded(ThresholdExceededEventArgs e)
        {
            WaitingForSynchronization = true;
            if (ThresholdExceeded != null)
            {
                ThresholdExceeded(this, e);
            }
        }

        /// <summary>
        /// Update the simulation by advancing forwards.
        /// </summary>
        /// <param name="frame">the frame up to which to run.</param>
        private void FastForward(long frame)
        {
            // Update states. Run back to front, to allow rewinding future states
            // if the trailing state must skip tentative commands all in one go.
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
                    // to apply that removal retroactively.
                    if (i == states.Length - 1 && states[i].SkipTentativeCommands())
                    {
                        needsRewind = true;
                    }

                    // Do the actual stepping for the state.
                    states[i].Update();

                    // Check if we need to add objects.
                    if (adds.ContainsKey(states[i].CurrentFrame))
                    {
                        // Add a copy of it.
                        foreach (var steppable in adds[states[i].CurrentFrame])
                        {
                            states[i].Add((TSteppable)steppable.Clone());
                        }
                    }

                    // Check if we need to remove objects.
                    if (removes.ContainsKey(states[i].CurrentFrame))
                    {
                        // Add a copy of it.
                        foreach (var steppableUid in removes[states[i].CurrentFrame])
                        {
                            states[i].Remove(steppableUid);
                        }
                    }
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
        /// Rewind the simulation to the given frame. If this fails (too far
        /// in the past) this will trigger a resync request.
        /// </summary>
        /// <param name="frame">the frame to rewind to.</param>
        private void Rewind(long frame)
        {
            // Find first state that's not past the frame.
            for (int i = 0; i < states.Length; ++i)
            {
                if (states[i].CurrentFrame < frame)
                {
                    // Success, mirror the state to all newer ones.
                    MirrorState(states[i], i - 1);
                    break;
                }
            }
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
                if (key <= TrailingFrame)
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
                if (key <= TrailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                removes.Remove(key);
            }
        }
    }
}