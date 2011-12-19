using System;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface to component system managers, which hold multiple systems
    /// which may communicate with each other via the manager.
    /// </summary>
    public interface IComponentSystemManager : ICloneable
    {
        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        ReadOnlyCollection<IComponentSystem> Systems { get; }

        /// <summary>
        /// Update all subsystems.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        void Update(ComponentSystemUpdateType updateType);

        /// <summary>
        /// Add the component to supported subsystems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        IComponentSystemManager AddComponent(IComponent component);

        /// <summary>
        /// Removes the component from supported subsystems.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        void RemoveComponent(IComponent component);

        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        IComponentSystemManager AddSystem(IComponentSystem system);

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        void RemoveSystem(IComponentSystem system);

        /// <summary>
        /// Get the first system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The first system of the given type, or <c>null</c> if no such system exits.</returns>
        T GetSystem<T>() where T : IComponentSystem;
    }
}
