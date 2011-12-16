using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for controllers managing a game state.
    /// </summary>
    public interface IStateController<TSession, TCommand, TPlayerData, TPacketizerContext>
        : IController<TSession, TCommand, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Add a entity to the simulation. Will be inserted at the
        /// current leading frame. The entity will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="entity">the entity to add.</param>
        /// <returns>the id the entity was assigned.</returns>
        long AddEntity(IEntity<TPlayerData, TPacketizerContext> entity);

        /// <summary>
        /// Add a entity to the simulation. Will be inserted at the
        /// current leading frame. The entity will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="entity">the entity to add.</param>
        /// <param name="frame">the frame in which to add the entity.</param>
        /// <returns>the id the entity was assigned.</returns>
        long AddEntity(IEntity<TPlayerData, TPacketizerContext> entity, long frame);

        /// <summary>
        /// Get a entity in this simulation based on its unique identifier.
        /// </summary>
        /// <param name="entityUid">the id of the object.</param>
        /// <returns>the object, if it exists.</returns>
        IEntity<TPlayerData, TPacketizerContext> GetEntity(long entityUid);

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the current frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        void RemoveEntity(long entityUid);

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the given frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        /// <param name="frame">the frame in which to remove the entity.</param>
        void RemoveEntity(long entityUid, long frame);
    }
}
