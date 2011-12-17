using System.Collections.Generic;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems
{
    public class CompositeComponentSystem : IComponentSystemManager
    {
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
        public void Update()
        {
            foreach (var item in _systems)
            {
                item.Update();
            }
        }

        /// <summary>
        /// Add all components of the specified entity to all known systems.
        /// </summary>
        /// <param name="entity">the entity of which to add the components.</param>
        public void AddEntity(IEntity entity)
        {
            foreach (var system in _systems)
            {
                system.AddEntity(entity);
            }
        }

        /// <summary>
        /// Remove all components of the specified entity from all known systems.
        /// </summary>
        /// <param name="entity">the entity of which to remove the components.</param>
        public void RemoveEntity(IEntity entity)
        {
            foreach (var system in _systems)
            {
                system.RemoveEntity(entity);
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
            var copy = new CompositeComponentSystem();
            foreach (var item in _systems)
            {
                copy.AddSystem((IComponentSystem)item.Clone());
            }
            return copy;
        }

        #endregion
    }
}
