using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Utility base class for component systems, pre-implementing adding / removal
    /// of components.
    /// 
    /// <para>
    /// Subclasses should take note that when cloning they must take care of
    /// duplicating reference types, to complete the deep-copy of the object.
    /// Caches, i.e. lists / dictionaries / etc. to quickly look up components
    /// should be reset.
    /// </para>
    /// </summary>
    /// <typeparam name="TUpdateParameterization">the type of parameterization used in this system</typeparam>
    public abstract class AbstractComponentSystem<TUpdateParameterization> : IComponentSystem
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        public IComponentSystemManager Manager { get; set; }

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        public ReadOnlyCollection<IComponent> Components { get { return new List<IComponent>(_components).AsReadOnly(); } }

        /// <summary>
        /// Tells if this component system should be packetized and sent via
        /// the network (server to client). This should only be true for logic
        /// related systems, that affect functionality that has to work exactly
        /// the same on both server and client.
        /// 
        /// <para>
        /// If the game has no network functionality, this flag is irrelevant.
        /// </para>
        /// </summary>
        public bool ShouldSynchronize { get; protected set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private readonly bool _isNullParameterized = (typeof(TUpdateParameterization) == typeof(NullParameterization));

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private HashSet<IComponent> _components = new HashSet<IComponent>();

        #endregion

        #region Interface

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Update(ComponentSystemUpdateType updateType, long frame)
        {
        }

        /// <summary>
        /// Call from subclasses to actually update a component, performs some
        /// additional checks.
        /// </summary>
        /// <param name="component">The component to update.</param>
        /// <param name="parameterization">The parameterization to use.</param>
        protected void UpdateComponent(IComponent component, object parameterization)
        {
            if (component.Enabled)
            {
                component.Update(parameterization);
            }
        }

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system, for chaining.</returns>
        public IComponentSystem AddComponent(IComponent component)
        {
            if (!_isNullParameterized && !_components.Contains(component))
            {
                if (component.SupportsParameterization(typeof(TUpdateParameterization)))
                {
                    _components.Add(component);
                    HandleComponentAdded(component);
                }
            }
            return this;
        }

        /// <summary>
        /// Removes the component from the system, if it's in it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(IComponent component)
        {
            if (!_isNullParameterized && _components.Remove(component))
            {
                HandleComponentRemoved(component);
            }
        }

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public virtual void HandleMessage(ValueType message)
        {
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public virtual Packet Packetize(Packet packet)
        {
            throw new NotSupportedException();
        }

        public virtual void Depacketize(Packet packet)
        {
            throw new NotSupportedException();
        }

        public virtual void Hash(Hasher hasher)
        {
        }

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public virtual object Clone()
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TUpdateParameterization>)MemberwiseClone();

            // If we're not null parameterized, use a different list. Copy over
            // non-entity components.
            if (!_isNullParameterized)
            {
                copy._components = new HashSet<IComponent>();
                foreach (var component in _components)
                {
                    if (component.Entity == null)
                    {
                        copy._components.Add((IComponent)component.Clone());
                    }
                }
            }
            
            // No manager at first. Must be re-set in (e.g. in cloned manager).
            copy.Manager = null;

            return copy;
        }

        #endregion

        #region Internal components tracking

        /// <summary>
        /// Perform actions for newly added components.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        protected virtual void HandleComponentAdded(IComponent component)
        {
        }

        /// <summary>
        /// Perform actions for removed components.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        protected virtual void HandleComponentRemoved(IComponent component)
        {
        }

        #endregion
    }
}
