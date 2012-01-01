using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Entities
{
    /// <summary>
    /// Base class for entities, implementing logic for distributing unique ids.
    /// </summary>
    public sealed class Entity : IEntity
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public int UID { get; set; }

        /// <summary>
        /// A list of all of components this entity is composed of.
        /// </summary>
        public ReadOnlyCollection<IComponent> Components { get { return _components.AsReadOnly(); } }

        /// <summary>
        /// The entity manager this entity is currently in.
        /// </summary>
        public IEntityManager Manager { get; set; }

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
        
        /// <summary>
        /// Running counter to uniquely number components.
        /// </summary>
        private IdManager _idManager = new IdManager();

        #endregion

        #region Construction

        public Entity()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        #endregion

        #region Components

        /// <summary>
        /// Registers a new component with this entity. If the entity is in a managed
        /// system, the component will be registered with all applicable component systems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(IComponent component)
        {
            if (component.Entity == this)
            {
                return;
            }
            if (component.Entity != null)
            {
                throw new ArgumentException("Component is already part of an entity.", "component");
            }
            else
            {
                component.UID = _idManager.GetId();
                AddComponentUnchecked(component);
            }
        }

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
            return (T)GetComponent(typeof(T));
        }

        /// <summary>
        /// Similar to the generic variant of this method, but takes a type
        /// parameter instead.
        /// </summary>
        /// <param name="componentType">The type of the component to get.</param>
        /// <returns>The component, or <c>null</c> if the entity has none of this type.</returns>
        public IComponent GetComponent(Type componentType)
        {
            // See if we have that one cached.
            if (_mapping.ContainsKey(componentType))
            {
                // Yes, return it.
                return _mapping[componentType];
            }

            // No, look it up and cache it.
            foreach (var component in _components)
            {
                if (component.GetType() == componentType)
                {
                    _mapping[componentType] = component;
                    return component;
                }
            }

            // Not found at all, cache as null and return.
            _mapping[componentType] = null;
            return null;
        }

        /// <summary>
        /// Get a component by its id.
        /// </summary>
        /// <param name="componentId">The id of the component to get.</param>
        /// <returns>The component, or <c>null</c> if there is no component
        /// with the specified id.</returns>
        public IComponent GetComponent(int componentId)
        {
            if (componentId > 0)
            {
                return _components.Find(c => c.UID == componentId);
            }
            return null;
        }

        /// <summary>
        /// Removes a component from this entity. If the entity is in a managed
        /// system, the component will be removed from all applicable component
        /// systems.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(IComponent component)
        {
            if (component.Entity != this)
            {
                return;
            }
            RemoveComponent(component.UID);
        }

        /// <summary>
        /// Removes a component by its id from this entity. If the entity is in
        /// a managed system, the component will be removed from all applicable
        /// component systems.
        /// </summary>
        /// <param name="componentUid">The id of the component to remove.</param>
        /// <returns>The removed component, or <c>null</c> if this entity has no
        /// component with the specified id.</returns>
        public IComponent RemoveComponent(int componentUid)
        {
            if (componentUid > 0)
            {
                int index = _components.FindIndex(c => c.UID == componentUid);
                if (index >= 0)
                {
                    var component = _components[index];
                    if (Manager != null)
                    {
                        Manager.SystemManager.RemoveComponent(component);
                    }
                    _idManager.ReleaseId(component.UID);
                    _components.RemoveAt(index);
                    component.UID = -1;
                    component.Entity = null;
                    return component;
                }
            }
            return null;
        }

        #endregion

        #region Utility methods

        private void AddComponentUnchecked(IComponent component)
        {
            _components.Add(component);
            component.Entity = this;
            if (Manager != null)
            {
                Manager.SystemManager.AddComponent(component);
            }
        }

        #endregion

        #region Component messaging

        /// <summary>
        /// Send a message to all components of this entity.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(ValueType message)
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
        public Packet Packetize(Packet packet)
        {
            // Id of this entity.
            return packet.Write(UID)

            // All components in this entity.
            .WriteWithTypeInfo(_components)

            // Id manager.
            .Write(_idManager);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">the packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            // Id of this entity.
            UID = packet.ReadInt32();

            // All components in this entity.
            _components.Clear();
            foreach (var component in packet.ReadPacketizablesWithTypeInfo<IComponent>())
            {
                AddComponentUnchecked(component);
            }

            // Id manager.
            packet.ReadPacketizableInto(_idManager);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
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
        public object Clone()
        {
            // Start with a quick, shallow copy.
            var copy = (Entity)MemberwiseClone();

            // Not belonging to a manager for now, has to be re-set.
            copy.Manager = null;

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

            // Clone id manager.
            copy._idManager = (IdManager)_idManager.Clone();

            return copy;
        }

        #endregion
    }
}
