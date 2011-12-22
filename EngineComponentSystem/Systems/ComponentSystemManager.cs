using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// A multi-system manager, holding multiple component systems and making them
    /// available to each other.
    /// </summary>
    public sealed class ComponentSystemManager : IComponentSystemManager
    {
        #region Properties

        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        public ReadOnlyCollection<IComponentSystem> Systems { get { return _systems.AsReadOnly(); } }

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        public IEntityManager EntityManager { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of all systems registered with this manager.
        /// </summary>
        private List<IComponentSystem> _systems = new List<IComponentSystem>();

        /// <summary>
        /// Lookup table for quick access to component by type.
        /// </summary>
        private Dictionary<Type, IComponentSystem> _mapping = new Dictionary<Type, IComponentSystem>();

        #endregion

        #region Interface

        /// <summary>
        /// Update all known systems.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(ComponentSystemUpdateType updateType, long frame)
        {
            foreach (var system in _systems)
            {
                system.Update(updateType, frame);
            }
        }

        /// <summary>
        /// Add the component to supported subsystems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        public IComponentSystemManager AddComponent(IComponent component)
        {
            foreach (var system in _systems)
            {
                system.AddComponent(component);
            }
            return this;
        }

        /// <summary>
        /// Removes the component from supported subsystems.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(IComponent component)
        {
            foreach (var system in _systems)
            {
                system.RemoveComponent(component);
            }
        }

        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        public IComponentSystemManager AddSystem(IComponentSystem system)
        {
            _systems.Add(system);
            system.Manager = this;
            return this;
        }

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        public void RemoveSystem(IComponentSystem system)
        {
            _systems.Remove(system);
            system.Manager = null;
        }

        #endregion

        #region System-lookup

        /// <summary>
        /// Get a system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>A system of the given type, or <c>null</c> if no such system exits.</returns>
        public T GetSystem<T>() where T : IComponentSystem
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
            foreach (var system in _systems)
            {
                if (system.GetType() == type)
                {
                    _mapping[type] = system;
                    return (T)system;
                }
            }

            // Not found at all, cache as null and return.
            _mapping[type] = null;
            return default(T);
        }

        #endregion

        #region Cloning

        public object Clone()
        {
            // Start with a quick, shallow copy.
            var copy = (ComponentSystemManager)MemberwiseClone();

            // Give it its own lookup table.
            copy._mapping = new Dictionary<Type, IComponentSystem>();

            // Create clones of all subsystems.
            copy._systems = new List<IComponentSystem>();
            foreach (var system in _systems)
            {
                copy.AddSystem((IComponentSystem)system.Clone());
            }

            return copy;
        }

        #endregion
    }
}
