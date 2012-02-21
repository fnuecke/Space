using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
namespace Engine.ComponentSystem
{
    /// <summary>
    /// Manager for a complete component system. Tracks live entities and
    /// components, and allows lookup of components for entities.
    /// </summary>
    public sealed class Manager : IManager
    {
        #region Fields
        
        /// <summary>
        /// Lookup table for quick access to component by type.
        /// </summary>
        private Dictionary<Type, AbstractSystem> _systems = new Dictionary<Type, AbstractSystem>();

        /// <summary>
        /// Maps entity ids to a mapping of component type to component, which
        /// allows quick lookup of components for a specific entity, and of a
        /// specific component type.
        /// </summary>
        private Dictionary<int, Dictionary<Type, Component>> _entities = new Dictionary<int, Dictionary<Type, Component>>();

        /// <summary>
        /// Component mapping id to instance (because there can be gaps
        /// </summary>
        private Dictionary<int, Component> _components = new Dictionary<int, Component>();

        /// <summary>
        /// Manager for entity ids.
        /// </summary>
        private IdManager _entityIds = new IdManager();

        /// <summary>
        /// Manager for entity ids.
        /// </summary>
        private IdManager _componentIds = new IdManager();

        #endregion

        #region Logic

        /// <summary>
        /// Update all registered systems.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(GameTime gameTime, long frame)
        {
            foreach (var system in _systems.Values)
            {
                system.Update(gameTime, frame);
            }

            // Make released component instances from the last update available
            // for reuse, as we can be sure they're not referenced in our
            // systems anymore.
            ReleaseDirty();
        }

        /// <summary>
        /// Renders all registered systems.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame to render.</param>
        public void Draw(GameTime gameTime, long frame)
        {
            foreach (var system in _systems.Values)
            {
                system.Draw(gameTime, frame);
            }
        }
        
        #endregion

        #region Systems

        /// <summary>
        /// Add the specified system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This manager, for chaining.</returns>
        public IManager AddSystem(AbstractSystem system)
        {
            _systems.Add(system.GetType(), system);
            system.Manager = this;
            return this;
        }

        /// <summary>
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        public void AddSystems(IEnumerable<AbstractSystem> systems)
        {
            foreach (var system in systems)
            {
                AddSystem(system);
            }
        }

        /// <summary>
        /// Removes the specified system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        /// <returns>Whether the system was successfully removed.</returns>
        public bool RemoveSystem(AbstractSystem system)
        {
            if (_systems.Remove(system.GetType()))
            {
                system.Manager = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get a system of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The system with the specified type.</returns>
        public T GetSystem<T>() where T : AbstractSystem
        {
            return (T)_systems[typeof(T)];
        }

        #endregion

        #region Entities and Components

        /// <summary>
        /// Creates a new entity and returns its ID.
        /// </summary>
        /// <returns>The id of the new entity.</returns>
        public int AddEntity()
        {
            // Allocate a new entity id and a component mapping for the entity.
            var entity = _entityIds.GetId();
            _entities[entity] = GetMapping();
            return entity;
        }

        /// <summary>
        /// Test whether the specified entity exists.
        /// </summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        public bool HasEntity(int entity)
        {
            return _entityIds.InUse(entity);
        }

        /// <summary>
        /// Removes an entity and all its components from the system.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveEntity(int entity)
        {
            // Make sure that entity exists.
            if (!HasEntity(entity))
            {
                throw new ArgumentException("No such entity in the system.", "entity");
            }

            // Remove all of the components attached to that entity.
            var mapping = _entities[entity];
            foreach (var component in mapping.Values)
            {
                RemoveComponent(component);
            }

            // Then free up the entity itself and push the mapping back to
            // the pool for reuse.
            _entityIds.ReleaseId(entity);
            _entities.Remove(entity);
            ReleaseMapping(mapping);

            // Send a message to all interested systems.
            EntityRemoved message;
            message.Entity = entity;
            SendMessage(ref message);
        }

        /// <summary>
        /// Creates a new component for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="entity">The entity to attach the component to.</param>
        /// <returns>The new component.</returns>
        public T AddComponent<T>(int entity) where T : Component, new()
        {
            // Make sure that entity exists.
            if (!HasEntity(entity))
            {
                throw new ArgumentException("No such entity in the system.", "entity");
            }

            // The create the component and set it up.
            var component = GetInstance<T>();
            component.Manager = this;
            component.Id = _componentIds.GetId();
            component.Entity = entity;
            component.Enabled = true;
            _entities[entity].Add(typeof(T), component);
            _components[component.Id] = component;

            // Send a message to all interested systems.
            ComponentAdded message;
            message.Component = component;
            SendMessage(ref message);

            // Return the created component.
            return component;
        }

        /// <summary>
        /// Test whether the component with the specified id exists.
        /// </summary>
        /// <param name="componentId">The id of the component to check for.</param>
        /// <returns>Whether the manager contains the component or not.</returns>
        public bool HasComponent(int componentId)
        {
            return _componentIds.InUse(componentId);
        }

        /// <summary>
        /// Removes the specified component from the system.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(Component component)
        {
            // Validate the component.
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }
            if (!HasComponent(component.Id))
            {
                throw new ArgumentException("No such component in the system.", "component");
            }

            // Remove it from the mapping and release the id for reuse.
            _entities[component.Entity].Remove(component.GetType());
            _components.Remove(component.Id);
            _componentIds.ReleaseId(component.Id);

            // Send a message to all interested systems.
            ComponentRemoved message;
            message.Component = component;
            SendMessage(ref message);

            // This will reset the component, so do that after sending the
            // event, to allow listeners to do something sensible with the
            // component before that.
            ReleaseInstance(component);
        }

        /// <summary>
        /// Gets the component of the specified type for an entity.
        /// </summary>
        /// <typeparam name="T">The type of component to get.</typeparam>
        /// <param name="entity">The entity to get the component of.</param>
        /// <returns>The component.</returns>
        public T GetComponent<T>(int entity) where T : Component
        {
            return (T)GetComponent(entity, typeof(T));
        }

        /// <summary>
        /// Gets the component of the specified type for an entity.
        /// </summary>
        /// <param name="entity">The entity to get the component of.</param>
        /// <param name="type">The type of the component to get.</param>
        /// <returns>The component.</returns>
        public Component GetComponent(int entity, Type type)
        {
            // Make sure that entity exists.
            if (!HasEntity(entity))
            {
                throw new ArgumentException("No such entity in the system.", "entity");
            }

            return _entities[entity][type];
        }

        /// <summary>
        /// Get a component by its id.
        /// </summary>
        /// <param name="componentId">The if of the component to retrieve.</param>
        /// <returns>The component with the specified id.</returns>
        public Component GetComponentById(int componentId)
        {
            // Make sure that component exists.
            if (!HasComponent(componentId))
            {
                throw new ArgumentException("No such component in the system.", "componentId");
            }

            return _components[componentId];
        }

        /// <summary>
        /// Allows enumerating over all components of the specified entity.
        /// </summary>
        /// <param name="entity">The entity for which to get the components.</param>
        /// <returns>An enumerable listing all components of that entity.</returns>
        public IEnumerable<Component> GetComponents(int entity)
        {
            // Make sure that entity exists.
            if (!HasEntity(entity))
            {
                throw new ArgumentException("No such entity in the system.", "entity");
            }

            return _entities[entity].Values;
        }
        
        #endregion

        #region Messaging

        /// <summary>
        /// Inform all interested systems of a message.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public void SendMessage<T>(ref T message) where T : struct
        {
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public Packet Packetize(Packet packet)
        {
            // Write systems, with their types, as these will only be read back
            // via <c>ReadPacketizableInto()</c> to keep some variables that
            // can only passed in the constructor.
            packet.Write(_systems.Count);
            foreach (var system in _systems.Values)
            {
                packet.Write(system.GetType().AssemblyQualifiedName);
                packet.Write(system);
            }

            // Write the components, which are enough to implicitly restore the
            // entity to component mapping as well, so we don't need to write
            // the entity mapping.
            packet.Write(_components.Count);
            foreach (var component in _components.Values)
            {
                packet.Write(component.GetType().AssemblyQualifiedName);
                packet.Write(component);
            }

            // And finally, the managers for used ids.
            packet.Write(_entityIds);
            packet.Write(_componentIds);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            int numSystems = packet.ReadInt32();
            for (int i = 0; i < numSystems; i++)
            {
                var typeName = packet.ReadString();
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new PacketException(string.Format("Invalid system type, not known locally: {0}.", typeName));
                }
                packet.ReadPacketizableInto(_systems[type]);
                _systems[type].Manager = this;
            }

            // Release all current objects.
            foreach (var mapping in _entities.Values)
            {
                ReleaseMapping(mapping);
            }
            _entities.Clear();
            foreach (var component in _components.Values)
            {
                ReleaseInstance(component);
            }
            _components.Clear();

            // Read back all components, fill in entity info as well, as that
            // is stored implicitly in the components.
            int numComponents = packet.ReadInt32();
            for (int i = 0; i < numComponents; i++)
            {
                var type = Type.GetType(packet.ReadString());
                var component = packet.ReadPacketizableInto(GetInstance(type));
                component.Manager = this;
                if (!_entities.ContainsKey(component.Entity))
                {
                    _entities.Add(component.Entity, GetMapping());
                }
                _entities[component.Entity].Add(component.GetType(), component);
                _components.Add(component.Id, component);
            }

            // And finally, the managers for ids.
            packet.ReadPacketizableInto(_entityIds);
            packet.ReadPacketizableInto(_componentIds);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        public IManager DeepCopy()
        {
            return DeepCopy(null);
        }

        public IManager DeepCopy(IManager into)
        {
            var copy = (Manager)((into is Manager) ? into : new Manager());

            if (copy == into)
            {
                // Copy systems.
                foreach (var item in _systems)
                {
                    item.Value.DeepCopy(copy._systems[item.Key]);
                }

                // Free entities.
                foreach (var mapping in copy._entities.Values)
                {
                    ReleaseMapping(mapping);
                }
                copy._entities.Clear();

                // Copy components, free components.
                foreach (var component in copy._components.Values)
                {
                    ReleaseInstance(component);
                }
                copy._components.Clear();

                // Copy id managers.
                copy._entityIds = _entityIds.DeepCopy(copy._entityIds);
                copy._componentIds = _componentIds.DeepCopy(copy._componentIds);
            }
            else
            {
                copy._systems = new Dictionary<Type, AbstractSystem>();
                copy._entities = new Dictionary<int, Dictionary<Type, Component>>();
                copy._components = new Dictionary<int, Component>();

                // Copy systems.
                foreach (var item in _systems)
                {
                    copy._systems.Add(item.Key, item.Value.DeepCopy());
                }

                // Copy id managers.
                copy._entityIds = _entityIds.DeepCopy();
                copy._componentIds = _componentIds.DeepCopy();
            }

            // Copy components.
            foreach (var component in _components.Values)
            {
                if (!copy._entities.ContainsKey(component.Entity))
                {
                    copy._entities.Add(component.Entity, GetMapping());
                }
                copy._entities[component.Id].Add(component.GetType(), component);
                copy._components.Add(component.Id, component);
            }

            return copy;
        }

        #endregion

        #region Pooling

        /// <summary>
        /// Objects that were released this very update run, so we don't want
        /// to reuse them until we enter the next update, to avoid reuse of
        /// components that might still be referenced in running system update
        /// loops.
        /// </summary>
        private readonly Dictionary<Type, Stack<Component>> _dirtyPool = new Dictionary<Type, Stack<Component>>();

        /// <summary>
        /// Object pool for reusable instances.
        /// </summary>
        private static readonly Dictionary<Type, Stack<Component>> Pool = new Dictionary<Type, Stack<Component>>();

        /// <summary>
        /// Pool for component mappings (used in component list).
        /// </summary>
        private static readonly Stack<Dictionary<Type, Component>> MapPool = new Stack<Dictionary<Type, Component>>();

        /// <summary>
        /// Try fetching an instance from the pool, if that fails, create a new one.
        /// </summary>
        /// <typeparam name="T">The type of component to get.</typeparam>
        /// <returns>A new component of the specified type.</returns>
        private static T GetInstance<T>() where T : Component, new()
        {
            var type = typeof(T);
            lock (Pool)
            {
                if (Pool.ContainsKey(type) && Pool[type].Count > 0)
                {
                    return (T)Pool[type].Pop();
                }
            }
            return new T();
        }

        /// <summary>
        /// Try fetching an instance from the pool, if that fails, create a new one.
        /// </summary>
        /// <param name="type">The type of component to get.</param>
        /// <returns>A new component of the specified type.</returns>
        private static Component GetInstance(Type type)
        {
            lock (Pool)
            {
                if (Pool.ContainsKey(type) && Pool[type].Count > 0)
                {
                    return Pool[type].Pop();
                }
            }
            return (Component)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Releases a component for later reuse.
        /// </summary>
        /// <param name="component">The component to release.</param>
        private void ReleaseInstance(Component component)
        {
            component.Reset();
            var type = component.GetType();
            if (!_dirtyPool.ContainsKey(type))
            {
                _dirtyPool.Add(type, new Stack<Component>());
            }
            _dirtyPool[type].Push(component);
        }

        /// <summary>
        /// Releases all instances that were possibly still referenced when
        /// they were removed from the manager in running system update
        /// loops.
        /// </summary>
        private void ReleaseDirty()
        {
            lock (Pool)
            {
                foreach (var type in _dirtyPool)
                {
                    if (!Pool.ContainsKey(type.Key))
                    {
                        Pool.Add(type.Key, new Stack<Component>());
                    }
                    foreach (var instance in type.Value)
                    {
                        Pool[type.Key].Push(instance);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a reusable component mapping or creates a new one.
        /// </summary>
        /// <returns>A new component mapping.</returns>
        private static Dictionary<Type, Component> GetMapping()
        {
            lock (MapPool)
            {
                if (MapPool.Count > 0)
                {
                    return MapPool.Pop();
                }
            }
            return new Dictionary<Type, Component>();
        }

        /// <summary>
        /// Releases a component mapping for later reuse.
        /// </summary>
        /// <param name="mapping">The mapping to free.</param>
        private static void ReleaseMapping(Dictionary<Type, Component> mapping)
        {
            mapping.Clear();
            lock (MapPool)
            {
                MapPool.Push(mapping);
            }
        }

        #endregion
    }
}
