using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Utility base class for component systems, pre-implementing adding / removal
    /// of components.
    /// </summary>
    /// <typeparam name="TUpdateParameterization">the type of parameterization used in this system</typeparam>
    public abstract class AbstractComponentSystem<TUpdateParameterization> : IComponentSystem
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        public IComponentSystemManager Manager { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        protected List<IComponent> components = new List<IComponent>();

        #endregion

        #region Interface

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(IComponent component)
        {
            if (component.SupportsParameterization(typeof(TUpdateParameterization)))
            {
                components.Add(component);
                HandleComponentAdded(component);
            }
        }

        /// <summary>
        /// Removes the component from the system, if it's in it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(IComponent component)
        {
            if (components.Remove(component))
            {
                HandleComponentRemoved(component);
            }
        }

        /// <summary>
        /// Add all components of the specified entity that can be handled by this system.
        /// </summary>
        /// <param name="entity">the entity of which to add the components.</param>
        public void AddEntity(IEntity entity)
        {
            foreach (var component in entity.Components)
            {
                AddComponent(component);
            }
        }

        /// <summary>
        /// Remove all components of the specified entity that can be handled by this system.
        /// </summary>
        /// <param name="entity">the entity of which to remove the components.</param>
        public void RemoveEntity(IEntity entity)
        {
            foreach (var component in entity.Components)
            {
                RemoveComponent(component);
            }
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Creates a shallow copy, with a clear component list, and no
        /// attached manager. When having members that need clearing or
        /// decoupling in subclasses, override this method, call it and
        /// clear them in the returned copy.
        /// </summary>
        /// <returns>A shallow, cleared copy of this system.</returns>
        public virtual object Clone()
        {
            // Copy any members of subclasses.
            var copy = (AbstractComponentSystem<TUpdateParameterization>)MemberwiseClone();
            // But use a different list.
            copy.components = new List<IComponent>();
            // And belong to no manager at first.
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
