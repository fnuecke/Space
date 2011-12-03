using Engine.Commands;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for controllers managing a game state.
    /// </summary>
    public interface IStateController<TState, TSteppable, TSession, TProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : IController<TSession, TProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TProtocol : IProtocol
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Add a steppable to the simulation. Will be inserted at the
        /// current leading frame. The steppable will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="steppable">the steppable to add.</param>
        /// <returns>the id the steppable was assigned.</returns>
        long AddSteppable(TSteppable steppable);

        /// <summary>
        /// Add a steppable to the simulation. Will be inserted at the
        /// current leading frame. The steppable will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="steppable">the steppable to add.</param>
        /// <param name="frame">the frame in which to add the steppable.</param>
        /// <returns>the id the steppable was assigned.</returns>
        long AddSteppable(TSteppable steppable, long frame);

        /// <summary>
        /// Removes a steppable with the given id from the simulation.
        /// The steppable will be removed at the current frame.
        /// </summary>
        /// <param name="steppableId">the id of the steppable to remove.</param>
        void RemoveSteppable(long steppableUid);

        /// <summary>
        /// Removes a steppable with the given id from the simulation.
        /// The steppable will be removed at the given frame.
        /// </summary>
        /// <param name="steppableId">the id of the steppable to remove.</param>
        /// <param name="frame">the frame in which to remove the steppable.</param>
        void RemoveSteppable(long steppableUid, long frame);
    }
}
