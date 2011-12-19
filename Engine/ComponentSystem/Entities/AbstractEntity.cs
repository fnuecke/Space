using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Entities
{
    /// <summary>
    /// Base class for entities, implementing logic for distributing unique ids.
    /// </summary>
    public abstract class AbstractEntity : IEntity
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public long UID { get; set; }

        /// <summary>
        /// A list of all of components this entity is composed of.
        /// </summary>
        public ReadOnlyCollection<IComponent> Components { get { return _components.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all this entity's components.
        /// </summary>
        private List<IComponent> _components = new List<IComponent>();

        /// <summary>
        /// Cached lookup of other components of the same element.
        /// </summary>
        private Dictionary<Type, IComponent> _mapping = new Dictionary<Type, IComponent>();

        #endregion

        #region Construction

        protected AbstractEntity()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        /// <summary>
        /// Registers a new component with this entity.
        /// </summary>
        /// <param name="component">the component to add.</param>
        protected void AddComponent(IComponent component)
        {
            _components.Add(component);
            component.Entity = this;
        }

        #endregion

        #region Component-lookup

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
        public T GetComponent<T>() where T : IComponent
        {
            // Get the type object representing the generic type.
            Type type = typeof(T);

            // See if we have that one cached.
            if (_mapping.ContainsKey(type))
            {
                // Yes, return it.
                return (T)_mapping[type];
            }

            // No, look it up and cache it.
            foreach (var component in _components)
            {
                if (component.GetType() == type)
                {
                    _mapping[type] = component;
                    return (T)component;
                }
            }

            // Not found at all, cache as null and return.
            _mapping[type] = null;
            return default(T);
        }

        #endregion

        #region Component messaging

        /// <summary>
        /// Send a message to all components of this entity.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(object message)
        {
            foreach (var component in _components)
            {
                component.HandleMessage(message);
            }
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public virtual void Packetize(Serialization.Packet packet)
        {
            packet.Write(UID);
            foreach (var component in _components)
            {
                component.Packetize(packet);
            }
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">the packet to read from.</param>
        public virtual void Depacketize(Serialization.Packet packet)
        {
            UID = packet.ReadInt64();
            foreach (var component in _components)
            {
                component.Depacketize(packet);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Util.Hasher hasher)
        {
            foreach (var component in _components)
            {
                component.Hash(hasher);
            }
        }

        /// <summary>
        /// Create a deep copy of the object, duplicating all its components.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference properties / fields
        /// they introduce.
        /// </para>
        /// </summary>
        /// <returns>A deep copy of this entity.</returns>
        public virtual object Clone()
        {
            // Start with a quick, shallow copy.
            var copy = (AbstractEntity)MemberwiseClone();

            // Give it its own mapper.
            copy._mapping = new Dictionary<Type, IComponent>();

            // And its own component list, then clone the components.
            copy._components = new List<IComponent>();
            foreach (var component in _components)
            {
                var componentCopy = (IComponent)component.Clone();
                // Assign the copy as the belonging entity.
                componentCopy.Entity = copy;
                copy._components.Add(componentCopy);
            }

            return copy;
        }

        #endregion
    }
}
