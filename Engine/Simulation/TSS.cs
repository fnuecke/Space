using System;
using System.Collections.Generic;
using Engine.Commands;

namespace Engine.Simulation
{

    /// <summary>
    /// Implements a Trailing State Synchronization.
    /// </summary>
    /// <see cref="http://warriors.eecs.umich.edu/games/papers/netgames02-tss.pdf"/>
    public class TSS<TState, TSteppable> : IReversibleState<TState, TSteppable>
        where TState : IState<TSteppable>, new()
        where TSteppable : ICloneable
    {

        /// <summary>
        /// Enumerator over all children of the leading state.
        /// </summary>
        public IEnumerator<TSteppable> Children { get { return states[0].Children; } }

        /// <summary>
        /// Get the leading state.
        /// </summary>
        public TState LeadingState { get { return states[0]; } }

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


            // Create state instances to avoid nullpointers, but wait for initial sync.
            Synchronize(new TState());
            WaitingForSynchronization = true;
        }

#region IState implementation

        /// <summary>
        /// The current frame of the leading state.
        /// </summary>
        public long CurrentFrame { get; protected set; }

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
                throw new InvalidOperationException();
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
                throw new InvalidOperationException();
            }

            // Advance the simulation.
            ++CurrentFrame;

            // Update sub states. We can orient ourselves on the leading frame,
            // because it will be the one rolled back the furthest (in case of
            // a roll back due to a past command being received).
            while (states[0].CurrentFrame < CurrentFrame)
            {
                // Update states.
                for (int i = states.Length - 1; i >= 0; --i)
                {
                    // Don't update if still delaying.
                    if (states[i].CurrentFrame + delays[i] < CurrentFrame)
                    {
                        // Check if we need to push a new object.
                        if (delayedAdds.ContainsKey(states[i].CurrentFrame))
                        {
                            // Add a copy of it.
                            foreach (var steppable in delayedAdds[states[i].CurrentFrame])
                            {
                                states[i].Add((TSteppable)steppable.Clone());
                            }

                            // Are we the last state?
                            if (i == states.Length - 1)
                            {
                                // Yes, so we don't need to keep track of that add any longer.
                                //delayedAdds.Remove(states[i].CurrentFrame);
                            }
                        }

                        // Do the actual stepping for the state.
                        states[i].Update();
                    }
                }
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
        public void PushCommand(ISimulationCommand command)
        {
            // Do not allow changes while waiting for synchronization.
            if (WaitingForSynchronization)
            {
                throw new InvalidOperationException();
            }

            // Ignore frames past the last synchronization.
            if (command.Frame < LastSynchronization)
            {
                return;
            }

            // Find first state that's not past the command.
            for (int i = 0; i < states.Length; ++i)
            {
                if (states[i].CurrentFrame < command.Frame)
                {
                    // Success, push it and mirror the state to all past ones.
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
                    return;
                }
            }

            // If we come here, all states are past the command's frame.
            if (command.IsTentative)
            {
                // Ignore tentative commands in that case.
                return;
            }

            // Otherwise it's a command the server accepted, and we need to
            // resynchronize our complete state.
            WaitingForSynchronization = true;
            OnThresholdExceeded(CurrentFrame);
        }

        /// <summary>
        /// Not available for TSS.
        /// </summary>
        public object Clone()
        {
            throw new NotImplementedException();
        }

#endregion

#region IReversibleState implementation

        public event ThresholdExceededEventHandler OnThresholdExceeded;

        public bool WaitingForSynchronization { get; protected set; }

        public long LastSynchronization { get; protected set; }

        public void Synchronize(TState state)
        {
            for (int i = 0; i < states.Length; ++i)
            {
                states[i] = (TState)state.Clone();
            }
            CurrentFrame = state.CurrentFrame;
            LastSynchronization = CurrentFrame;
            WaitingForSynchronization = false;

#if DEBUG
            Console.WriteLine("Synchronized to state @ " + CurrentFrame);
#endif

        }

#endregion

    }
}