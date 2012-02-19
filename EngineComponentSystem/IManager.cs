using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Interface for component system managers.
    /// </summary>
    public interface IManager : ICopyable<IManager>, IPacketizable, IHashable
    {
        #region Logic

        /// <summary>
        /// Update all registered systems.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(GameTime gameTime, long frame);

        /// <summary>
        /// Renders all registered systems.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame to render.</param>
        void Draw(GameTime gameTime, long frame);
        
        #endregion

        #region Systems

        /// <summary>
        /// Add the specified system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This manager, for chaining.</returns>
        IManager AddSystem(AbstractSystem system);

        /// <summary>
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        void AddSystems(IEnumerable<AbstractSystem> systems);

        /// <summary>
        /// Removes the specified system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        /// <returns>Whether the system was successfully removed.</returns>
        bool RemoveSystem(AbstractSystem system);

        /// <summary>
        /// Get a system of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The system with the specified type.</returns>
        T GetSystem<T>() where T : AbstractSystem;
        
        #endregion

        #region Entities and Components

        /// <summary>
        /// Creates a new entity and returns its ID.
        /// </summary>
        /// <returns>The id of the new entity.</returns>
        int AddEntity();

        /// <summary>
        /// Test whether the specified entity exists.
        /// </summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        bool HasEntity(int entity);

        /// <summary>
        /// Removes an entity and all its components from the system.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        void RemoveEntity(int entity);

        /// <summary>
        /// Creates a new component for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="entity">The entity to attach the component to.</param>
        /// <returns>The new component.</returns>
        Component AddComponent<T>(int entity) where T : Component, new();
        
        /// <summary>
        /// Test whether the component with the specified id exists.
        /// </summary>
        /// <param name="componentId">The id of the component to check for.</param>
        /// <returns>Whether the manager contains the component or not.</returns>
        bool HasComponent(int componentId);

        /// <summary>
        /// Removes the specified component from the system.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        void RemoveComponent(Component component);

        /// <summary>
        /// Gets the component of the specified type for an entity.
        /// </summary>
        /// <typeparam name="T">The type of component to get.</typeparam>
        /// <param name="entity">The entity to get the component of.</param>
        /// <returns>The component.</returns>
        T GetComponent<T>(int entity) where T : Component;
        
        /// <summary>
        /// Get a component by its id.
        /// </summary>
        /// <param name="componentId">The if of the component to retrieve.</param>
        /// <returns>The component with the specified id.</returns>
        Component GetComponentById(int componentId);

        /// <summary>
        /// Allows enumerating over all components of the specified entity.
        /// </summary>
        /// <param name="entity">The entity for which to get the components.</param>
        /// <returns>An enumerable listing all components of that entity.</returns>
        IEnumerable<Component> GetComponents(int entity);
        
        #endregion

        #region Messaging

        /// <summary>
        /// Inform all interested systems of a message.
        /// </summary>
        /// <param name="message">The sent message.</param>
        void SendMessage<T>(ref T message) where T : struct;
        
        #endregion
    }
}
