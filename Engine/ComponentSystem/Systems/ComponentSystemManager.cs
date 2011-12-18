using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    public class CompositeComponentSystem : IComponentSystemManager
    {
        #region Properties

        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        public ReadOnlyCollection<IComponentSystem> Systems { get { return _systems.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all systems registered with this manager.
        /// </summary>
        private List<IComponentSystem> _systems = new List<IComponentSystem>();

        #endregion

        #region Interface

        /// <summary>
        /// Update all known systems.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        public void Update(ComponentSystemUpdateType updateType)
        {
            foreach (var system in _systems)
            {
                system.Update(updateType);
            }
        }

        /// <summary>
        /// Add the component to supported subsystems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(IComponent component)
        {
            foreach (var system in _systems)
            {
                system.AddComponent(component);
            }
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
        public void AddSystem(IComponentSystem system)
        {
            _systems.Add(system);
            system.Manager = this;
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

        #region Accessor

        /// <summary>
        /// Get a system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>A system of the given type, or <c>null</c> if no such system exits.</returns>
        public T GetSystem<T>() where T : IComponentSystem
        {
            foreach (var system in _systems)
            {
                if (system.GetType().Equals(typeof(T)))
                {
                    return (T)system;
                }
            }
            return default(T);
        }

        #endregion

        #region Cloning

        public object Clone()
        {
            var copy = (CompositeComponentSystem)MemberwiseClone();

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
