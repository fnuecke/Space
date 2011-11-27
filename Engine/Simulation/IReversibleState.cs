using System;

namespace Engine.Simulation
{
    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleState<TState, TSteppable> : IState<TState, TSteppable>
        where TState : IState<TState, TSteppable>
        where TSteppable : ISteppable<TState, TSteppable>
    {
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, which is fed back to this
        /// state using the Synchronize() method.
        /// </summary>
        event EventHandler<EventArgs> ThresholdExceeded;

        /// <summary>
        /// The frame when the last complete synchronization took place,
        /// i.e. the point we don't roll back past.
        /// </summary>
        long LastSynchronization { get; }

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        bool WaitingForSynchronization { get; }

        /// <summary>
        /// Resynchronize the state to the server using the given snapshot,
        /// which must be an authoritative snapshot.
        /// </summary>
        /// <param name="state">the authoritative snapshot to use for resynchronization.</param>
        void Synchronize(TState state);
    }
}
