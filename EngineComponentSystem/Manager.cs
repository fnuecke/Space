using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Manager for a complete component system. Tracks live entities and
    /// components, and allows lookup of components for entities.
    /// </summary>
    /// <remarks>
    /// All reading operations on the manager are thread safe. These are:
    /// <see cref="GetSystem(int)"/>, <see cref="HasEntity(int)"/>, <see cref="HasComponent(int)"/>,
    /// <see cref="GetComponent(int, int)"/>, <see cref="GetComponentById(int)"/> and
    /// <see cref="GetComponents(int, int)"/>.
    /// 
    /// <para>
    /// It is <em>not</em> thread safe for all writing operations (adding, removing).
    /// </para>
    /// 
    /// <para>
    /// Note that this does <em>not</em> guarantee the thread safety of the individual components.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Systems = {_systems.Count}, Components = {_componentIds.Count}")]
    public sealed partial class Manager : IManager
    {
        #region Properties

        /// <summary>
        /// A list of all components currently registered with this manager,
        /// in order of their ID.
        /// </summary>
        public IEnumerable<Component> Components
        {
            get
            {
                foreach (var id in _componentIds)
                {
                    yield return _components[id];
                }
            }
        }

        /// <summary>
        /// A list of all systems registered with this manager.
        /// </summary>
        public IEnumerable<AbstractSystem> Systems
        {
            get { return _systems; }
        }

        /// <summary>
        /// Number of components currently registered in this system.
        /// </summary>
        public int ComponentCount
        {
            get { return _componentIds.Count; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Manager for entity ids.
        /// </summary>
        private IdManager _entityIds = new IdManager();

        /// <summary>
        /// Manager for entity ids.
        /// </summary>
        private IdManager _componentIds = new IdManager();

        /// <summary>
        /// List of systems registered with this manager.
        /// </summary>
        private readonly List<AbstractSystem> _systems = new List<AbstractSystem>(); 

        /// <summary>
        /// List of all updating systems registered with this manager.
        /// </summary>
        private readonly List<IUpdatingSystem> _updatingSystems = new List<IUpdatingSystem>();

        /// <summary>
        /// List of all messaging systems registered with this manager.
        /// </summary>
        private readonly List<IMessagingSystem> _messagingSystems = new List<IMessagingSystem>();

        /// <summary>
        /// List of all drawing systems registered with this manager.
        /// </summary>
        private readonly List<IDrawingSystem> _drawingSystems = new List<IDrawingSystem>();

        /// <summary>
        /// Lookup table for quick access to systems by their type id.
        /// </summary>
        private readonly SparseArray<AbstractSystem> _systemsByTypeId = new SparseArray<AbstractSystem>();

        /// <summary>
        /// Keeps track of entity->component relationships.
        /// </summary>
        private readonly SparseArray<Entity> _entities = new SparseArray<Entity>();

        /// <summary>
        /// Lookup table for quick access to components by their id.
        /// </summary>
        private readonly SparseArray<Component> _components = new SparseArray<Component>();

        #endregion

        #region Logic

        /// <summary>
        /// Update all registered systems.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            for (int i = 0, j = _updatingSystems.Count; i < j; ++i)
            {
                _updatingSystems[i].Update(frame);
            }

            // Make released component instances from the last update available
            // for reuse, as we can be sure they're not referenced in our
            // systems anymore.
            ReleaseDirty();
        }

        /// <summary>
        /// Renders all registered systems.
        /// </summary>
        /// <param name="frame">The frame to render.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            for (int i = 0, j = _drawingSystems.Count; i < j; ++i)
            {
                if (_drawingSystems[i].IsEnabled)
                {
                    _drawingSystems[i].Draw(frame, elapsedMilliseconds);
                }
            }
        }

        #endregion

        #region Systems

        /// <summary>
        /// Add the specified system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>
        /// This manager, for chaining.
        /// </returns>
        public IManager AddSystem(AbstractSystem system)
        {
            // We do not allow systems to be both logic and presentation related.
            if ((system is IUpdatingSystem || system is IMessagingSystem) && system is IDrawingSystem)
            {
                throw new ArgumentException("Systems must be either logic related (IUpdatingSystem, IMessagingSystem) or presentation related (IDrawingSystem), but never both.");
            }

            // Get type ID for that system.
            var systemTypeId = GetSystemTypeId(system.GetType());

            // Don't allow adding the same system twice.
            if (_systemsByTypeId[systemTypeId] != null)
            {
                throw new ArgumentException("Must not add the same system twice.");
            }

            // Register the system.
            while (systemTypeId != 0)
            {
                _systemsByTypeId[systemTypeId] = system;
                systemTypeId = SystemHierarchy[systemTypeId];
            }

            // Add to general list, for serialization and hashing.
            _systems.Add(system);

            // Add to updating list, for update iteration.
            if (system is IUpdatingSystem)
            {
                _updatingSystems.Add((IUpdatingSystem)system);
            }
            // Add to messaging list, for iteration for message distribution.
            if (system is IMessagingSystem)
            {
                _messagingSystems.Add((IMessagingSystem)system);
            }
            // Add to drawing list, for draw iteration.
            if (system is IDrawingSystem)
            {
                _drawingSystems.Add((IDrawingSystem)system);
            }

            // Set the manager so that the system knows it belongs to us.
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
        /// Adds a copy of the specified system.
        /// </summary>
        /// <param name="system">The system to copy.</param>
        public void CopySystem(AbstractSystem system)
        {
            // Make sure we have a valid system.
            if(system is IDrawingSystem)
            {
                throw new ArgumentException("Cannot copy presentation systems.", "system");
            }

            var systemTypeId = GetSystemTypeId(system.GetType());
            if (_systemsByTypeId[systemTypeId] == null)
            {
                AddSystem(system.NewInstance());
            }
            system.CopyInto(_systemsByTypeId[systemTypeId]);
        }

        /// <summary>
        /// Removes the specified system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        /// <returns>
        /// Whether the system was successfully removed.
        /// </returns>
        public bool RemoveSystem(AbstractSystem system)
        {
            // Make sure we have a valid system.
            Debug.Assert(system != null);

            // Get type ID for that system.
            var systemTypeId = GetSystemTypeId(system.GetType());

            // Check if we even have that system.
            if (_systemsByTypeId[systemTypeId] == null)
            {
                return false;
            }

            // Unregister the system.
            while (systemTypeId != 0)
            {
                _systemsByTypeId[systemTypeId] = null;
                systemTypeId = SystemHierarchy[systemTypeId];
            }
            _systems.Remove(system);
            _updatingSystems.Remove(system as IUpdatingSystem);
            _drawingSystems.Remove(system as IDrawingSystem);
            system.Manager = null;

            return true;
        }

        /// <summary>
        /// Get a system of the specified type.
        /// </summary>
        /// <param name="typeId">The type of the system to get.</param>
        /// <returns>
        /// The system with the specified type.
        /// </returns>
        public AbstractSystem GetSystem(int typeId)
        {
            return _systemsByTypeId[typeId];
        }

        #endregion

        #region Entities and Components

        /// <summary>
        /// Creates a new entity and returns its ID.
        /// </summary>
        /// <returns>
        /// The id of the new entity.
        /// </returns>
        public int AddEntity()
        {
            // Allocate a new entity id and a component mapping for the entity.
            var entity = _entityIds.GetId();
            _entities[entity] = AllocateEntity();
            return entity;
        }

        /// <summary>
        /// Removes an entity and all its components from the system.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveEntity(int entity)
        {
            // Make sure that entity exists.
            Debug.Assert(HasEntity(entity), "No such entity in the system.");

            // Remove all of the components attached to that entity and free up
            // the entity object itself, and release the id.
            var components = _entities[entity].Components;
            while (components.Count > 0)
            {
                RemoveComponent(components[components.Count - 1]);
            }
            var instance = _entities[entity];
            _entities[entity] = null;
            _entityIds.ReleaseId(entity);
            ReleaseEntity(instance);

            // Send a message to all systems.
            foreach (var system in _systems)
            {
                system.OnEntityRemoved(entity);
            }
        }

        /// <summary>
        /// Test whether the specified entity exists.
        /// </summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>
        /// Whether the manager contains the entity or not.
        /// </returns>
        public bool HasEntity(int entity)
        {
            return _entityIds.InUse(entity);
        }

        /// <summary>
        /// Creates a new component for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="entity">The entity to attach the component to.</param>
        /// <returns>
        /// The new component.
        /// </returns>
        public T AddComponent<T>(int entity) where T : Component, new()
        {
            // Make sure that entity exists.
            Debug.Assert(HasEntity(entity), "No such entity in the system.");

            // The create the component and set it up.
            var component = (T)AllocateComponent(typeof(T));
            component.Manager = this;
            component.Id = _componentIds.GetId();
            component.Entity = entity;
            component.Enabled = true;
            _components[component.Id] = component;

            // Add to entity index.
            _entities[entity].Add(component);

            // Send a message to all systems.
            foreach (var system in _systems)
            {
                system.OnComponentAdded(component);
            }

            // Return the created component.
            return component;
        }

        /// <summary>
        /// Removes the specified component from the system.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(Component component)
        {
            // Validate the component.
            Debug.Assert(component != null);
            Debug.Assert(HasComponent(component.Id), "No such component in the system.");

            // Send a message to all systems.
            foreach (var system in _systems)
            {
                system.OnComponentRemoved(component);
            }

            // Remove it from the mapping and release the id for reuse.
            _entities[component.Entity].Remove(component);
            _components[component.Id] = null;
            _componentIds.ReleaseId(component.Id);
            ReleaseComponent(component);
        }

        /// <summary>
        /// Test whether the component with the specified id exists.
        /// </summary>
        /// <param name="componentId">The id of the component to check for.</param>
        /// <returns>
        /// Whether the manager contains the component or not.
        /// </returns>
        public bool HasComponent(int componentId)
        {
            return _componentIds.InUse(componentId);
        }

        /// <summary>
        /// Gets a component of the specified type for an entity. If there are
        /// multiple components of the same type attached to the entity, use
        /// the <c>index</c> parameter to select which one to get.
        /// </summary>
        /// <param name="entity">The entity to get the component of.</param>
        /// <param name="typeId">The type of the component to get.</param>
        /// <returns>
        /// The component.
        /// </returns>
        public Component GetComponent(int entity, int typeId)
        {
            Debug.Assert(HasEntity(entity), "No such entity in the system.");
            return _entities[entity].GetComponent(typeId);
        }

        /// <summary>
        /// Get a component by its id.
        /// </summary>
        /// <param name="componentId">The if of the component to retrieve.</param>
        /// <returns>The component with the specified id.</returns>
        public Component GetComponentById(int componentId)
        {
            Debug.Assert(HasComponent(componentId), "No such component in the system.");
            return _components[componentId];
        }

        /// <summary>
        /// Allows enumerating over all components of the specified entity.
        /// </summary>
        /// <param name="entity">The entity for which to get the components.</param>
        /// <param name="typeId">The type of components to get.</param>
        /// <returns>
        /// An enumerable listing all components of that entity.
        /// </returns>
        public IEnumerable<Component> GetComponents(int entity, int typeId)
        {
            Debug.Assert(HasEntity(entity), "No such entity in the system.");
            return _entities[entity].GetComponents(typeId);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Inform all interested systems of a message.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The sent message.</param>
        public void SendMessage<T>(ref T message) where T : struct
        {
            for (int i = 0, j = _messagingSystems.Count; i < j; ++i)
            {
                _messagingSystems[i].Receive(ref message);
            }
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
            // Write the managers for used ids.
            packet.Write(_entityIds);
            packet.Write(_componentIds);

            // Write the components, which are enough to implicitly restore the
            // entity to component mapping as well, so we don't need to write
            // the entity mapping.
            packet.Write(_componentIds.Count);
            foreach (var component in Components)
            {
                packet.Write(component.GetType());
                packet.Write(component);
            }

            // Write systems, with their types, as these will only be read back
            // via <c>ReadPacketizableInto()</c> to keep some variables that
            // can only passed in the constructor.
            packet.Write(_systems.Count);
            for (int i = 0, j = _systems.Count; i < j; ++i)
            {
                packet.Write(_systems[i].GetType());
                packet.Write(_systems[i]);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            // Release all current objects.
            foreach (var entity in _entityIds)
            {
                ReleaseEntity(_entities[entity]);
            }
            _entities.Clear();
            foreach (var component in Components)
            {
                ReleaseComponent(component);
            }
            _components.Clear();

            // Get the managers for ids (restores "known" ids before restoring components).
            packet.ReadPacketizableInto(ref _entityIds);
            packet.ReadPacketizableInto(ref _componentIds);

            // Read back all components, fill in entity info as well, as that
            // is stored implicitly in the components.
            var numComponents = packet.ReadInt32();
            for (var i = 0; i < numComponents; ++i)
            {
                var type = packet.ReadType();
                var component = AllocateComponent(type);
                packet.ReadPacketizableInto(ref component);
                component.Manager = this;
                _components[component.Id] = component;

                // Add to entity mapping, create entries as necessary.
                if (_entities[component.Entity] == null)
                {
                    _entities[component.Entity] = AllocateEntity();
                }
                _entities[component.Entity].Add(component);
            }
            // Fill in empty entities. This is to re-create empty entities, i.e.
            // entities with no components.
            foreach (var entityId in _entityIds)
            {
                if (_entities[entityId] == null)
                {
                    _entities[entityId] = AllocateEntity();
                }
            }

            // Read back all systems. This must be done after reading the components,
            // because the systems will fetch their components again at this point.
            var numSystems = packet.ReadInt32();
            for (var i = 0; i < numSystems; ++i)
            {
                var type = packet.ReadType();
                if (!SystemTypes.ContainsKey(type))
                {
                    throw new PacketException("Could not depacketize system of unknown type " + type.FullName);
                }
                var instance = _systemsByTypeId[GetSystemTypeId(type)];
                packet.ReadPacketizableInto(ref instance);
            }

            // All done, send message to allow post-processing.
            foreach (var system in _systems)
            {
                system.OnDepacketized();
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
                system.Hash(hasher);
            }
            foreach (var component in Components)
            {
                component.Hash(hasher);
            }
        }

        /// <summary>
        /// Write a complete entity, meaning all its components, to the
        /// specified packet. Entities saved this way can be restored using
        /// the <c>ReadEntity()</c> method.
        /// <para/>
        /// This uses the components' <c>Packetize</c> facilities.
        /// </summary>
        /// <param name="entity">The entity to write.</param>
        /// <param name="packet">The packet to write to.</param>
        /// <returns>
        /// The packet after writing the entity's components.
        /// </returns>
        public Packet PacketizeEntity(int entity, Packet packet)
        {
            return packet.WriteWithTypeInfo(_entities[entity].Components);
        }

        /// <summary>
        /// Reads an entity from the specified packet, meaning all its
        /// components. This will create a new entity, with an id that
        /// may differ from the id the entity had when it was written.
        /// <para/>
        /// In particular, all re-created components will likely have different
        /// different ids as well, so this method is not suited for storing
        /// components that reference other components, even if just by their
        /// ID.
        /// <para/>
        /// This will act as though all of the written components were added,
        /// i.e. each restored component will send a <c>ComponentAdded</c>
        /// message.
        /// <para/>
        /// This uses the components' <c>Depacketize</c> facilities.
        /// </summary>
        /// <param name="packet">The packet to read the entity from.</param>
        /// <returns>The id of the read entity.</returns>
        public int DepacketizeEntity(Packet packet)
        {
            var entity = AddEntity();
            try
            {
                var components = packet.ReadPacketizablesWithTypeInfo<Component>();
                foreach (var component in components)
                {
                    component.Id = _componentIds.GetId();
                    component.Entity = entity;
                    component.Manager = this;
                    _components[component.Id] = component;

                    // Add to entity index.
                    _entities[entity].Add(component);

                    // Send a message to all systems.
                    foreach (var system in _systems)
                    {
                        system.OnComponentAdded(component);
                    }
                }
                return entity;
            }
            catch (Exception)
            {
                RemoveEntity(entity);
                throw;
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported, to avoid issues with assumptions on how presentation
        /// related systems should be handled. Manually create a new instance
        /// instead and use <see cref="CopyInto"/> on it.
        /// </summary>
        /// <returns>The copy.</returns>
        public IManager NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(IManager into)
        {
            // Validate input.
            if (into == this)
            {
                throw new ArgumentException("Cannot copy a manager into itself.");
            }

            // Get the properly typed version.
            var copy = (Manager)into;

            // Copy id managers.
            _entityIds.CopyInto(copy._entityIds);
            _componentIds.CopyInto(copy._componentIds);

            // Copy components and entities.
            copy._components.Clear();
            foreach (var component in Components)
            {
                // The create the component and set it up.
                var componentCopy = AllocateComponent(component.GetType()).Initialize(component);
                componentCopy.Id = component.Id;
                componentCopy.Entity = component.Entity;
                componentCopy.Manager = copy;
                copy._components[componentCopy.Id] = componentCopy;
            }

            copy._entities.Clear();
            foreach (var entity in _entityIds)
            {
                // Create copy.
                var entityCopy = AllocateEntity();
                copy._entities[entity] = entityCopy;

                // Assign copied components.
                foreach (var component in _entities[entity].Components)
                {
                    entityCopy.Add(copy.GetComponentById(component.Id));
                }
            }

            // Copy systems after copying components so they can fetch their
            // components again.
            foreach (var item in _systems)
            {
                copy.CopySystem(item);
            }

            // All done, send message to allow post-processing.
            foreach (var system in copy._systems)
            {
                system.OnCopied();
            }
        }

        #endregion
    }
}
