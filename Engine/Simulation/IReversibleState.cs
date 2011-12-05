﻿using System;
using Engine.Commands;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, and feed it back to the
        /// state (e.g. using Depacketize()).
        /// </summary>
        event EventHandler<EventArgs> Invalidated;

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        bool WaitingForSynchronization { get; }

        /// <summary>
        /// Mark the state as invalid (desynchronized). Will trigger a new
        /// <c>ThresholdExceeded</c> event.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Run the simulation to the given frame, which may be in the past.
        /// </summary>
        /// <param name="frame">the frame to run to.</param>
        void RunToFrame(long frame);

        /// <summary>
        /// Add an object in a specific time frame. This will roll back, if
        /// necessary, to insert the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppable">the object to insert.</param>
        /// <param name="frame">the frame to insert it at.</param>
        void AddSteppable(TSteppable steppable, long frame);

        /// <summary>
        /// Remove an object in a specific time frame. This will roll back, if
        /// necessary, to remove the object, meaning it can trigger desyncs.
        /// </summary>
        /// <param name="steppableUid">the id of the object to remove.</param>
        /// <param name="frame">the frame to remove it at.</param>
        void RemoveSteppable(long steppableUid, long frame);

        /// <summary>
        /// Push a command to be executed at the given frame.  This will roll
        /// back, if necessary, to remove the object, meaning it can trigger
        /// desyncs.
        /// </summary>
        /// <param name="command">the command to push.</param>
        /// <param name="frame">the frame in which to execute the command.</param>
        void PushCommand(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, long frame);
    }
}
