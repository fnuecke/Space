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
        protected Dictionary<long, List<TSteppable>> delayedAdds =
            new Dictionary<long, List<TSteppable>>();

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

            if (!delayedAdds.ContainsKey(CurrentFrame + 1))
            {
                delayedAdds.Add(CurrentFrame + 1, new List<TSteppable>());
            }
            delayedAdds[CurrentFrame + 1].Add((TSteppable)steppable.Clone());
        }

        /// <summary>
        /// Not supported for TSS. All objects must remove themselves from their actual
        /// state.
        /// </summary>
        /// <param name="updateable"></param>
        public void Remove(TSteppable steppable)
        {
            throw new NotSupportedException();
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
                    // Check if we need to push a new object.
                    if (delayedAdds.ContainsKey(states[i].CurrentFrame))
                    {
                        // Add a copy of it.
                        foreach (var steppable in delayedAdds[states[i].CurrentFrame])
                        {
                            states[i].Add((TSteppable)steppable.Clone());
                        }
                    }

                    // Do the actual stepping for the state.
                    states[i].Update();
                }
            }

            // Remove commands from the to-add list that have been added
            // to the state trailing furthest behind at this point.
            long trailingFrame = states[states.Length - 1].CurrentFrame;
            List<long> deprecatedKeys = new List<long>();
            foreach (var key in delayedAdds.Keys)
            {
                if (key < trailingFrame)
                {
                    deprecatedKeys.Add(key);
                }
            }
            foreach (var key in deprecatedKeys)
            {
                delayedAdds.Remove(key);
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
            packet.Write(delayedAdds.Count);
            foreach (var add in delayedAdds)
            {
                packet.Write(add.Key);
                packet.Write(add.Value.Count);
                foreach (var item in add.Value)
                {
                    Packetizer.Packetize(item, packet);
                }
            }
            states[states.Length - 1].Packetize(packet);
        }

        /// <summary>
        /// Deserialize a state from a packet.
        /// </summary>
        /// <param name="packet">the packet to read the data from.</param>
        /// <returns>deserialzed state.</returns>
        public void Depacketize(Packet packet)
        {
            CurrentFrame = packet.ReadInt64();
            int numDelayedAdds = packet.ReadInt32();
            for (int delayedAdd = 0; delayedAdd < numDelayedAdds; ++delayedAdd)
            {
                long key = packet.ReadInt64();
                int numValues = packet.ReadInt32();
                for (int valueIdx = 0; valueIdx < numValues; ++valueIdx)
                {
                    TSteppable value = Packetizer.Depacketize<TSteppable>(packet);
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

        public void Synchronize()
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