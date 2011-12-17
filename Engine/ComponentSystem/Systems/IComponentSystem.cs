using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface for component systems (which are responsible for
    /// updating a specific type of component with an agreed upon
    /// parameterization).
    /// 
    /// <para>
    /// Note that cloning systems will simply result in a new
    /// instance of the same type. Components will not be
    /// copied.
    /// </para>
    /// </summary>
    public interface IComponentSystem : ICloneable
    {
        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        IComponentSystemManager Manager { get; set; }

        /// <summary>
        /// Update all components in this system.
        /// </summary>
        void Update();

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        void AddComponent(IComponent component);

        /// <summary>
        /// Removes the component from the system, if it's in it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        void RemoveComponent(IComponent component);

        /// <summary>
        /// Add all components of the specified entity that can be handled by this system.
        /// </summary>
        /// <param name="entity">the entity of which to add the components.</param>
        void AddEntity(IEntity entity);

        /// <summary>
        /// Remove all components of the specified entity that can be handled by this system.
        /// </summary>
        /// <param name="entity">the entity of which to remove the components.</param>
        void RemoveEntity(IEntity entity);
    }
}
