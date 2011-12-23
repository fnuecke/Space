using System;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Entities
{
    /// <summary>
    /// Minimal functionality of a world entity that can be used in simulations
    /// and updated via the component system. The entity must know its components
    /// and delegate the <c>Update</c> call to its components.
    /// </summary>
    public interface IEntity : IPacketizable, ICloneable, IHashable
    {
        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        int UID { get; set; }

        /// <summary>
        /// A list of all of this entities components.
        /// </summary>
        ReadOnlyCollection<IComponent> Components { get; }

        /// <summary>
        /// The entity manager this entity is currently in.
        /// </summary>
        IEntityManager Manager { get; set; }
                /// <summary>
        /// Registers a new component with this entity. If the entity is in a managed
        /// system, the component will be registered with all applicable component systems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        void AddComponent(IComponent component);

        /// <summary>
        /// Get a component of the specified type from this entity, if it
        /// has one.
        /// 
        /// <para>
        /// This performs caching internally, so subsequent calls should
        /// be relatively fast.
        /// </para>
        /// </summary>
        /// <typeparam name="T">the type of the component to get.</typeparam>
        /// <returns>the component, or <c>null</c> if the entity has none of this type.</returns>
        T GetComponent<T>() where T : IComponent;

        /// <summary>
        /// Get a component by its id.
        /// </summary>
        /// <param name="componentId">The id of the component to get.</param>
        /// <returns>The component, or <c>null</c> if there is no component
        /// with the specified id.</returns>
        IComponent GetComponent(int componentId);

        /// <summary>
        /// Removes a component from this entity. If the entity is in a managed
        /// system, the component will be removed from all applicable component
        /// systems.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        void RemoveComponent(IComponent component);
        
        /// <summary>
        /// Removes a component by its id from this entity. If the entity is in
        /// a managed system, the component will be removed from all applicable
        /// component systems.
        /// </summary>
        /// <param name="componentUid">The id of the component to remove.</param>
        /// <returns>The removed component, or <c>null</c> if this entity has no
        /// component with the specified id.</returns>
        IComponent RemoveComponent(int componentUid);

        /// <summary>
        /// Send a message to all components of this entity.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessage(ValueType message);
    }
}
