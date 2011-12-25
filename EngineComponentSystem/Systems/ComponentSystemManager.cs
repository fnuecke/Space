using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

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
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        public void AddSystems(IEnumerable<IComponentSystem> systems)
        {
            foreach (var system in systems)
            {
                AddSystem(system);
            }
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

        #region System messaging

        /// <summary>
        /// Send a message to all systems of this component system manager.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(ValueType message)
        {
            foreach (var system in _systems)
            {
                system.HandleMessage(message);
            }
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public Packet Packetize(Packet packet)
        {
            int numToSync = 0;
            foreach (var system in _systems)
            {
                if (system.ShouldSynchronize)
                {
                    ++numToSync;
                }
            }
            packet.Write(numToSync);

            foreach (var system in _systems)
            {
                if (system.ShouldSynchronize)
                {
                    packet.Write(system.GetType().AssemblyQualifiedName);
                    packet.Write(system);
                }
            }

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            int numToSync = packet.ReadInt32();
            for (int i = 0; i < numToSync; i++)
            {
                Type type = Type.GetType(packet.ReadString());
                packet.ReadPacketizableInto(_mapping[type]);
            }
        }

        public void Hash(Hasher hasher)
        {
            foreach (var system in _systems)
            {
                if (system.ShouldSynchronize)
                {
                    system.Hash(hasher);
                }
            }
        }

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
