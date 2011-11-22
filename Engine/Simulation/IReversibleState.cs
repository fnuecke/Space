using System;

namespace Engine.Simulation
{

    /// <summary>
    /// Used for resynchronization in case we cannot accommodate a server command.
    /// </summary>
    /// <param name="frame">The frame the server snapshot should at least have.</param>
    public delegate void ThresholdExceededEventHandler(long frame);

    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleState<TState, TUpdateable> : IState<TUpdateable>
        where TState : IState<TUpdateable>
        where TUpdateable : ICloneable
    {

        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, which is fed back to this
        /// state using the Synchronize() method.
        /// </summary>
        event ThresholdExceededEventHandler OnThresholdExceeded;

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        bool WaitingForSynchronization { get; }

        /// <summary>
        /// The frame when the last complete synchronization took place,
        /// i.e. the point we don't roll back past.
        /// </summary>
        long LastSynchronization { get; }

        /// <summary>
        /// Resynchronize the state to the server using the given snapshot,
        /// which must be a server snapshot.
        /// </summary>
        /// <param name="state">the server snapshot to use for resynchronization.</param>
        void Synchronize(TState state);

    }
}
