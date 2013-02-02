using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;

namespace Engine.ComponentSystem
{
    /// <summary>Signature for functions handling messages.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to handle.</param>
    public delegate void MessageCallback<in T>(T message);

    /// <summary>Interface for component system managers.</summary>
    public interface IManager : ICopyable<IManager>
    {
        #region Properties

        /// <summary>A list of all systems registered with this manager.</summary>
        [PublicAPI]
        IEnumerable<AbstractSystem> Systems { get; }

        /// <summary>A list of all components currently registered with this manager, in order of their ID.</summary>
        [PublicAPI]
        IEnumerable<IComponent> Components { get; }

        /// <summary>Number of systems currently registered in this manager.</summary>
        [PublicAPI]
        int SystemCount { get; }

        /// <summary>Number of entities currently registered in this manager.</summary>
        [PublicAPI]
        int EntityCount { get; }

        /// <summary>Number of components currently registered in this manager.</summary>
        [PublicAPI]
        int ComponentCount { get; }

        #endregion

        #region Logic

        /// <summary>Update all registered systems.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        [PublicAPI]
        void Update(long frame);

        /// <summary>Renders all registered systems.</summary>
        /// <param name="frame">The frame to render.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        [PublicAPI]
        void Draw(long frame, float elapsedMilliseconds);

        #endregion

        #region Systems

        /// <summary>Add the specified system to this manager.</summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This manager, for chaining.</returns>
        [PublicAPI]
        IManager AddSystem(AbstractSystem system);

        /// <summary>Add multiple systems to this manager.</summary>
        /// <param name="systems">The systems to add.</param>
        [PublicAPI]
        void AddSystems(IEnumerable<AbstractSystem> systems);

        /// <summary>Adds a copy of the specified system.</summary>
        /// <param name="system">The system to copy.</param>
        [PublicAPI]
        void CopySystem(AbstractSystem system);

        /// <summary>Removes the specified system from this manager.</summary>
        /// <param name="system">The system to remove.</param>
        /// <returns>Whether the system was successfully removed.</returns>
        [PublicAPI]
        bool RemoveSystem(AbstractSystem system);

        /// <summary>Get a system of the specified type.</summary>
        /// <param name="typeId">The type of the system to get.</param>
        /// <returns>The system with the specified type.</returns>
        [PublicAPI]
        AbstractSystem GetSystem(int typeId);

        #endregion

        #region Entities and Components

        /// <summary>Creates a new entity and returns its ID.</summary>
        /// <returns>The id of the new entity.</returns>
        [PublicAPI]
        int AddEntity();

        /// <summary>Removes an entity and all its components from the system.</summary>
        /// <param name="entity">The entity to remove.</param>
        [PublicAPI]
        void RemoveEntity(int entity);

        /// <summary>Test whether the specified entity exists.</summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        [PublicAPI]
        bool HasEntity(int entity);

        /// <summary>Creates a new component for the specified entity.</summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="entity">The entity to attach the component to.</param>
        /// <returns>The new component.</returns>
        [PublicAPI]
        T AddComponent<T>(int entity) where T : Component, new();

        /// <summary>Removes the specified component from the system.</summary>
        /// <param name="component">The component to remove.</param>
        [PublicAPI]
        void RemoveComponent(Component component);

        /// <summary>Removes the specified component from the system.</summary>
        /// <param name="componentId">The id of the component to remove.</param>
        [PublicAPI]
        void RemoveComponent(int componentId);

        /// <summary>Test whether the component with the specified id exists.</summary>
        /// <param name="componentId">The id of the component to check for.</param>
        /// <returns>Whether the manager contains the component or not.</returns>
        [PublicAPI]
        bool HasComponent(int componentId);

        /// <summary>Get a component by its id.</summary>
        /// <param name="componentId">The if of the component to retrieve.</param>
        /// <returns>The component with the specified id.</returns>
        [PublicAPI]
        Component GetComponentById(int componentId);

        /// <summary>
        ///     Gets a component of the specified type for an entity. If there are multiple components of the same type attached to
        ///     the entity, use the <c>index</c> parameter to select which one to get.
        /// </summary>
        /// <param name="entity">The entity to get the component of.</param>
        /// <param name="typeId">The type of the component to get.</param>
        /// <returns>The component.</returns>
        [PublicAPI]
        Component GetComponent(int entity, int typeId);

        /// <summary>Allows enumerating over all components of the specified entity.</summary>
        /// <param name="entity">The entity for which to get the components.</param>
        /// <param name="typeId">The type of components to iterate.</param>
        /// <returns>An enumerable listing all components of that type of that entity.</returns>
        [PublicAPI]
        IEnumerable<Component> GetComponents(int entity, int typeId);

        #endregion

        #region Messaging

        /// <summary>Inform all interested systems of a message.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The sent message.</param>
        [PublicAPI]
        void SendMessage<T>(T message);

        #endregion

        #region Serialization

        /// <summary>
        ///     Write a complete entity, meaning all its components, to the specified packet. Entities saved this way can be
        ///     restored using the <see cref="DepacketizeEntity"/> method.
        ///     <para/>
        ///     This uses the components' serialization facilities.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="entity">The entity to write.</param>
        /// <returns>The packet after writing the entity's components.</returns>
        [PublicAPI]
        IWritablePacket PacketizeEntity(IWritablePacket packet, int entity);

        /// <summary>
        ///     Reads an entity from the specified packet, meaning all its components. This will create a new entity, with an id
        ///     that may differ from the id the entity had when it was written.
        ///     <para/>
        ///     In particular, all re-created components will likely have different different ids as well, so this method is not
        ///     suited for storing components that reference other components, even if just by their ID.
        ///     <para/>
        ///     This will act as though all of the written components were added, i.e. for each restored component all systems'
        ///     <see cref="AbstractSystem.OnComponentAdded"/> method will be called.
        ///     <para/>
        ///     This uses the components' serialization facilities.
        /// </summary>
        /// <param name="packet">The packet to read the entity from.</param>
        /// <param name="componentIdMap">A mapping of how components' ids changed due to serialization, mapping old id to new id.</param>
        /// <returns>The id of the read entity.</returns>
        [PublicAPI]
        int DepacketizeEntity(IReadablePacket packet, Dictionary<int, int> componentIdMap = null);

        #endregion
    }
}