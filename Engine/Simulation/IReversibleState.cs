using System;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleState<TState, TSteppable, TCommandType, TPlayerData> : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, and feed it back to the
        /// state (e.g. using Depacketize()).
        /// </summary>
        event EventHandler<EventArgs> ThresholdExceeded;

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        bool WaitingForSynchronization { get; }
    }
}
