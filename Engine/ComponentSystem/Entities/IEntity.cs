using System;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
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
        long UID { get; set; }

        /// <summary>
        /// A list of all of this entities components.
        /// </summary>
        ReadOnlyCollection<IComponent> Components { get; }

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
        /// Send a message to all components of this entity.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessage(object message);
    }
}
