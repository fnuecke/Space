using Engine.ComponentSystem.Entities;
using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for reversible controllers.
    /// </summary>
    public interface IReversibleSimulationController<TSession> : ISimulationController<TSession>
        where TSession : ISession
    {
        /// <summary>
        /// Add a entity to the simulation. Will be inserted at the
        /// current leading frame. The entity will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="entity">the entity to add.</param>
        /// <param name="frame">the frame in which to add the entity.</param>
        void AddEntity(IEntity entity, long frame);

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the given frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        /// <param name="frame">the frame in which to remove the entity.</param>
        void RemoveEntity(long entityUid, long frame);
    }
}
