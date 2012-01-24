using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// A multi-system manager, holding multiple component systems and making them
    /// available to each other.
    /// </summary>
    public sealed class SystemManager : ISystemManager
    {
        #region Properties

        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        public ReadOnlyCollection<ISystem> Systems { get { return _systems.AsReadOnly(); } }

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        public IEntityManager EntityManager { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of all systems registered with this manager.
        /// </summary>
        private List<ISystem> _systems = new List<ISystem>();

        /// <summary>
        /// Lookup table for quick access to component by type.
        /// </summary>
        private Dictionary<Type, ISystem> _mapping = new Dictionary<Type, ISystem>();

        #endregion

        #region Interface

        /// <summary>
        /// Update all known systems.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            foreach (var system in _systems)
            {
                system.Update(frame);
            }
        }

        /// <summary>
        /// Draw all known systems.
        /// </summary>
        /// <param name="frame">The frame in which the draw is applied.</param>
        public void Draw(GameTime gameTime, long frame)
        {
            foreach (var system in _systems)
            {
                system.Draw(gameTime, frame);
            }
        }

        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        public ISystemManager AddSystem(ISystem system)
        {
            // Clear our mapping (system may have been cached via less
            // specific type).
            _mapping.Clear();

            _systems.Add(system);
            system.Manager = this;

            return this;
        }

        /// <summary>
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        public void AddSystems(IEnumerable<ISystem> systems)
        {
            // Clear our mapping (system may have been cached via less
            // specific type).
            _mapping.Clear();

            foreach (var system in systems)
            {
                _systems.Add(system);
                system.Manager = this;
            }
        }

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        public void RemoveSystem(ISystem system)
        {
            // Clear our mapping (system may have been cached via less
            // specific type).
            _mapping.Clear();

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
        public T GetSystem<T>() where T : ISystem
        {
            return (T)GetSystem(typeof(T));
        }

        /// <summary>
        /// Performs actual look-up.
        /// </summary>
        private ISystem GetSystem(Type type)
        {
            // See if we have that one cached.
            if (_mapping.ContainsKey(type))
            {
                // Yes, return it.
                return _mapping[type];
            }

            // No, look it up and cache it.
            foreach (var system in _systems)
            {
                if (system.GetType() == type)
                {
                    _mapping[type] = system;
                    return system;
                }
            }

            // Not found at all, cache as null and return.
            _mapping[type] = null;
            return null;
        }

        #endregion

        #region System messaging

        /// <summary>
        /// Send a message to all systems of this component system manager.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage<T>(ref T message) where T : struct
        {
            foreach (var system in _systems)
            {
                system.HandleMessage(ref message);
            }
        }

        #endregion

        #region Serialization / Hashing / Cloning

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
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

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            int numToSync = packet.ReadInt32();
            for (int i = 0; i < numToSync; i++)
            {
                Type type = Type.GetType(packet.ReadString());
                packet.ReadPacketizableInto(GetSystem(type));
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
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

        /// <summary>
        /// Create a deep copy of the object.
        /// </summary>
        /// <returns>A deep copy of this entity.</returns>
        public ISystemManager DeepCopy()
        {
            return DeepCopy(null);
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public ISystemManager DeepCopy(ISystemManager into)
        {
            var copy = (SystemManager)(into ?? MemberwiseClone());

            // No entity manager, has to be re-set.
            copy.EntityManager = null;

            if (copy == into)
            {
                if (_systems.Count != copy._systems.Count)
                {
                    throw new ArgumentException("System count mismatch.", "into");
                }

                for (int i = 0; i < _systems.Count; ++i)
                {
                    copy._systems[i] = _systems[i].DeepCopy(copy._systems[i]);
                    copy._systems[i].Manager = copy;
                }

                copy._mapping.Clear();
            }
            else
            {
                // Create clones of all subsystems.
                copy._systems = new List<ISystem>();
                foreach (var system in _systems)
                {
                    copy.AddSystem(system.DeepCopy());
                }

                // Give it its own lookup table.
                copy._mapping = new Dictionary<Type, ISystem>();
            }

            return copy;
        }

        #endregion
    }
}
