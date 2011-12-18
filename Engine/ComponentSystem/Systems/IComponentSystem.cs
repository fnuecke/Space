using System;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Update types for a component system, based on the different update
    /// stages of a game loop (normally at least update + draw).
    /// </summary>
    public enum ComponentSystemUpdateType
    {
        /// <summary>
        /// Performs a game logic pass.
        /// </summary>
        Logic,

        /// <summary>
        /// Performs rendering pass.
        /// </summary>
        Display
    }

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
        /// A list of components registered in this system.
        /// </summary>
        ReadOnlyCollection<IComponent> Components { get; }

        /// <summary>
        /// Update all components in this system.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        void Update(ComponentSystemUpdateType updateType);

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
    }
}
