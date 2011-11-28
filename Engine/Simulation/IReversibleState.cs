using System;

namespace Engine.Simulation
{
    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleState<TState, TSteppable, TCommandType> : IState<TState, TSteppable, TCommandType>
        where TState : IState<TState, TSteppable, TCommandType>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
    {
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, and feed it back to the
        /// state (e.g. using Depacketize()).
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
    }
}
