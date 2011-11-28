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
    public class TSS<TState, TSteppable, TCommandType> : IReversibleState<TState, TSteppable, TCommandType>
        where TState : IState<TState, TSteppable, TCommandType>, new()
        where TSteppable : ISteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
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
        public IEnumerator<TSteppable> Children { get { return LeadingState.Children; } }

        /// <summary>
        /// The current frame of the leading state.
        /// </summary>
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// The frame when the last complete synchronization took place,
        /// i.e. the point we don't roll back past.
        /// </summary>
        public long LastSynchronization { get; protected set; }

        /// <summary>
        /// Get the leading state.
        /// </summary>
        public TState LeadingState { get { return states[0]; } }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        public bool WaitingForSynchronization { get; protected set; }

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
        protected int[] delays;

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
        public TSS(int[] delays)
        {
            this.delays = new int[delays.Length + 1];
            delays.CopyTo(this.delays, 1);
            Array.Sort(this.delays);

            // Generate initial states.
            states = new TState[this.delays.Length];

            // Initialize to empty state.
            states[states.Length - 1] = new TState();
            Synchronize();
        }

        /// <summary>
        /// Add a new object.
        /// 
        /// This will add the object to the leading state, and add it to the delayed
        /// states when they reach the current frame of the leading state.
        /// </summary>
        /// <param name="updateable">the object to add.</param>
        public void Add(TSteppable steppable)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException("Waiting for synchronization.");
            }

            LeadingState.Add(steppable);
            if (!adds.ContainsKey(CurrentFrame))
            {
                adds.Add(CurrentFrame, new List<TSteppable>());
            }
            adds[CurrentFrame].Add((TSteppable)steppable.Clone());
        }

        /// <summary>
        /// Not supported for TSS.
        /// </summary>
        public void Remove(TSteppable steppable)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove a steppable object by its id.
        /// </summary>
        /// <param name="steppableUid">the remove object.</param>
        public TSteppable Remove(long steppableUid)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException("Waiting for synchronization.");
            }

            var result = LeadingState.Remove(steppableUid);
            if (result != null)
            {
                // If the removal was a success in the leading state, schedule the removal
                // all trailing states.
                if (!removes.ContainsKey(CurrentFrame))
                {
                    removes.Add(CurrentFrame, new List<long>());
                }
                removes[CurrentFrame].Add(steppableUid);
            }
            return result;
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
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException("Waiting for synchronization.");
            }

            // Advance the simulation.
            ++CurrentFrame;

            // Update states.
            for (int i = states.Length - 1; i >= 0; --i)
            {
                // Update while we're still delaying.
                while (states[i].CurrentFrame + delays[i] < CurrentFrame)
                {
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
            }

            // Remove adds / removes from the to-add list that have been added
            // to the state trailing furthest behind at this point.
            long trailingFrame = states[states.Length - 1].CurrentFrame;

            List<long> deprecated = new List<long>();

            foreach (var key in adds.Keys)
            {
                if (key < trailingFrame)
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
                if (key < trailingFrame)
                {
                    deprecated.Add(key);
                }
            }
            foreach (var key in deprecated)
            {
                removes.Remove(key);
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
        public void PushCommand(ISimulationCommand<TCommandType> command)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException();
            }

            // Ignore frames past the last synchronization and tentative
            // commands past our trailing frame.
            if (command.Frame <= LastSynchronization ||
               (command.Frame <= states[states.Length - 1].CurrentFrame &&
                command.IsTentative))
            {
                return;
            }

            // Find first state that's not past the command.
            for (int i = 0; i < states.Length; ++i)
            {
                if (states[i].CurrentFrame < command.Frame)
                {
                    // Success, push it and mirror the state to all newer ones.
                    states[i].PushCommand(command);
                    for (int j = i - 1; j >= 0; --j)
                    {
                        states[j] = (TState)states[i].Clone();
                    }

#if DEBUG
                    if (i > 0)
                    {
                        Console.WriteLine("Rolled back to state " + i);
                    }
#endif

                    // Also apply it to the remaining trailing states, no checks.
                    for (int j = i + 1; j < states.Length; ++j)
                    {
                        states[j].PushCommand(command);
                    }

                    // Done.
                    return;
                }
            }

            // We need to resynchronize our complete state, we couldn't handle
            // an authoritative command.
            WaitingForSynchronization = true;
            OnThresholdExceeded(new ThresholdExceededEventArgs(CurrentFrame));
        }

        /// <summary>
        /// Serialize a state to a packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public void Packetize(Packet packet)
        {
            packet.Write(CurrentFrame);

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

            states[states.Length - 1].Packetize(packet);
        }

        /// <summary>
        /// Deserialize a state from a packet.
        /// </summary>
        /// <param name="packet">the packet to read the data from.</param>
        public void Depacketize(Packet packet)
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

            // Continue with reading the list of adds / removes.
            int numAdds = packet.ReadInt32();
            for (int addIdx = 0; addIdx < numAdds; ++addIdx)
            {
                long key = packet.ReadInt64();
                adds.Add(key, new List<TSteppable>());
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
                removes.Add(key, new List<long>());
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    removes[key].Add(packet.ReadInt64());
                }
            }

            states[states.Length - 1].Depacketize(packet);

            Synchronize();
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public object Clone()
        {
            throw new NotImplementedException();
        }

        private void Synchronize()
        {
            for (int i = states.Length - 2; i >= 0; --i)
            {
                states[i] = (TState)states[states.Length - 1].Clone();
            }
            
            LastSynchronization = CurrentFrame;
            WaitingForSynchronization = false;

#if DEBUG
            Console.WriteLine("Synchronized to state @ " + CurrentFrame);
#endif
        }

        protected void OnThresholdExceeded(ThresholdExceededEventArgs e)
        {
            if (ThresholdExceeded != null)
            {
                ThresholdExceeded(this, e);
            }
        }
    }
}