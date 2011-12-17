using Engine.ComponentSystem.Entities;
namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface to component system managers, which hold multiple systems
    /// which may communicate with each other via the manager.
    /// </summary>
    public interface IComponentSystemManager
    {
        /// <summary>
        /// Update all subsystems.
        /// </summary>
        void Update();

        /// <summary>
        /// Add all components of the specified entity to all known systems.
        /// </summary>
        /// <param name="entity">the entity of which to add the components.</param>
        void AddEntity(IEntity entity);

        /// <summary>
        /// Remove all components of the specified entity from all known systems.
        /// </summary>
        /// <param name="entity">the entity of which to remove the components.</param>
        void RemoveEntity(IEntity entity);

        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        void AddSystem(IComponentSystem system);

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        void RemoveSystem(IComponentSystem system);

        /// <summary>
        /// Get the first system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The first system of the given type, or <c>null</c> if no such system exits.</returns>
        T GetSystem<T>() where T : IComponentSystem;
    }
}
