using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;

namespace Engine.ComponentSystem
{
    /// <summary>
    ///     Manager for a complete component system. Tracks live entities and components, and allows lookup of components
    ///     for entities.
    /// </summary>
    /// <remarks>
    ///     All reading operations on the manager are thread safe. These are:
    ///     <see cref="GetSystem(int)"/>, <see cref="HasEntity(int)"/>, <see cref="HasComponent(int)"/>,
    ///     <see cref="GetComponent(int, int)"/>, <see cref="GetComponentById(int)"/> and
    ///     <see cref="GetComponents(int, int)"/>.
    ///     <para>
    ///         It is <em>not</em> thread safe for all writing operations (adding, removing).
    ///     </para>
    ///     <para>
    ///         Note that this does <em>not</em> guarantee the thread safety of the individual components.
    ///     </para>
    /// </remarks>
    [Packetizable, DebuggerDisplay("Systems = {_systems.Count}, Components = {_componentIds.Count}")]
    public sealed partial class Manager : IManager
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>A list of all systems registered with this manager.</summary>
        public IEnumerable<AbstractSystem> Systems
        {
            get { return _systems; }
        }

        /// <summary>A list of all components currently registered with this manager, in order of their ID.</summary>
        public IEnumerable<IComponent> Components
        {
            get { return _componentIds.Select(id => _components[id]); }
        }

        /// <summary>Number of systems currently registered in this manager.</summary>
        public int SystemCount
        {
            get { return _systems.Count; }
        }

        /// <summary>Number of entities currently registered in this manager.</summary>
        public int EntityCount
        {
            get { return _entityIds.Count; }
        }

        /// <summary>Number of components currently registered in this manager.</summary>
        public int ComponentCount
        {
            get { return _componentIds.Count; }
        }

        #endregion

        #region Fields

        /// <summary>List of systems registered with this manager.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly List<AbstractSystem> _systems = new List<AbstractSystem>();

        /// <summary>Lookup table for quick access to systems by their type id.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly SparseArray<AbstractSystem> _systemsByTypeId = new SparseArray<AbstractSystem>();

        /// <summary>Manager for entity ids.</summary>
        private readonly IdManager _entityIds = new IdManager();

        /// <summary>Keeps track of entity->component relationships.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly SparseArray<Entity> _entities = new SparseArray<Entity>();

        /// <summary>Manager for entity ids.</summary>
        private readonly IdManager _componentIds = new IdManager();

        /// <summary>Lookup table for quick access to components by their id.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly SparseArray<Component> _components = new SparseArray<Component>();

        /// <summary>Table of registered message callbacks, by type.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly Dictionary<Type, Delegate> _messageCallbacks = new Dictionary<Type, Delegate>();

        /// <summary>
        ///     Used to track the nesting of <see cref="SendMessage{T}"/> calls, to know when removed components can be released.
        /// </summary>
        private int _messageDepth;

        #endregion

        #region Initialization

        /// <summary>
        ///     Determine type ids for all loaded assemblies. This helps getting a more deterministic order when registering
        ///     types, thus not messing with serialized data. It should be called before any other program logic is performed.
        /// </summary>
        [PublicAPI]
        public static void Initialize()
        {
            Logger.Info("Checking for components...");
            var count = 0;
            foreach (var type in Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsSubclassOf(typeof (Component)))
                .OrderBy(t => t.AssemblyQualifiedName))
            {
                var id = GetComponentTypeId(type);

                ++count;
                Logger.Trace("  {0}: {1}", id, type.FullName);
            }
            Logger.Info("Found {0} component types.", count);

            Logger.Trace("Checking for systems...");
            count = 0;
            foreach (var type in Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsSubclassOf(typeof (AbstractSystem)))
                .OrderBy(t => t.AssemblyQualifiedName))
            {
                var id = GetSystemTypeId(type);
                ++count;
                Logger.Trace("  {0}: {1}", id, type.FullName);

                // Run static constructors to force deterministic order when initializing
                // static fields (such as index ids).
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
            Logger.Info("Found {0} system types.", count);
        }

        #endregion

        #region Logic

        /// <summary>Update all registered systems.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            Update message;
            message.Frame = frame;
            SendMessage(message);
        }

        /// <summary>Renders all registered systems.</summary>
        /// <param name="frame">The frame to render.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            Draw message;
            message.Frame = frame;
            message.ElapsedMilliseconds = elapsedMilliseconds;
            SendMessage(message);
        }

        #endregion

        #region Systems

        /// <summary>Add the specified system to this manager.</summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This manager, for chaining.</returns>
        public IManager AddSystem(AbstractSystem system)
        {
            // Get type ID for that system.
            var systemTypeId = GetSystemTypeId(system.GetType());

            // Don't allow adding the same system twice.
            if (_systemsByTypeId[systemTypeId] != null)
            {
                throw new ArgumentException("You can not add more than one system of a single type.");
            }

            // Look for message callbacks in the system. This throws if a callback signature is invalid,
            // which is why we want to do this before registering the system.
            var messageTypes = GetMessageCallbackTypes(system.GetType());

            // Add to general list, for serialization and hashing and general iteration.
            _systems.Add(system);

            // Register the system by type, for fast lookup.
            while (systemTypeId != 0)
            {
                _systemsByTypeId[systemTypeId] = system;
                systemTypeId = SystemHierarchy[systemTypeId];
            }

            // Set the manager so that the system knows it belongs to us.
            system.Manager = this;

            // Rebuild message dispatchers for all changed types.
            foreach (var messageType in messageTypes)
            {
                RebuildMessageDispatcher(messageType);
            }

            // Tell the system it was added.
            system.OnAddedToManager();

            return this;
        }

        /// <summary>Add multiple systems to this manager.</summary>
        /// <param name="systems">The systems to add.</param>
        public void AddSystems(IEnumerable<AbstractSystem> systems)
        {
            foreach (var system in systems)
            {
                AddSystem(system);
            }
        }

        /// <summary>Adds a copy of the specified system.</summary>
        /// <param name="system">The system to copy.</param>
        public void CopySystem(AbstractSystem system)
        {
            var systemTypeId = GetSystemTypeId(system.GetType());
            if (_systemsByTypeId[systemTypeId] == null)
            {
                AddSystem(system.NewInstance());
            }
            Debug.Assert(_systemsByTypeId[systemTypeId] != null);
            system.CopyInto(_systemsByTypeId[systemTypeId]);
        }

        /// <summary>Removes the specified system from this manager.</summary>
        /// <param name="system">The system to remove.</param>
        /// <returns>Whether the system was successfully removed.</returns>
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
            _systems.Remove(system);

            while (systemTypeId != 0)
            {
                _systemsByTypeId[systemTypeId] = null;
                systemTypeId = SystemHierarchy[systemTypeId];
            }

            system.Manager = null;

            foreach (var messageType in GetMessageCallbackTypes(system.GetType()))
            {
                RebuildMessageDispatcher(messageType);
            }

            return true;
        }

        /// <summary>Get a system of the specified type.</summary>
        /// <param name="typeId">The type of the system to get.</param>
        /// <returns>The system with the specified type.</returns>
        public AbstractSystem GetSystem(int typeId)
        {
            return _systemsByTypeId[typeId];
        }

        private static IEnumerable<Type> GetMessageCallbackTypes(Type systemType)
        {
            var callbacks = new HashSet<Type>();
            foreach (var method in systemType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsDefined(typeof (MessageCallbackAttribute), true)))
            {
                if (method.ReturnType != typeof (void))
                {
                    var declaringType = method.DeclaringType ?? systemType;
                    throw new ArgumentException(
                        string.Format(
                            "Invalid message callback {0}.{1}, must have void return type.",
                            declaringType.Name,
                            method.Name));
                }

                if (method.IsGenericMethodDefinition)
                {
                    var declaringType = method.DeclaringType ?? systemType;
                    throw new ArgumentException(
                        string.Format(
                            "Invalid message callback {0}.{1}, must not be generic.",
                            declaringType.Name,
                            method.Name));
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1 ||
                    parameters[0].IsOut ||
                    parameters[0].ParameterType.IsByRef)
                {
                    var declaringType = method.DeclaringType ?? systemType;
                    throw new ArgumentException(
                        string.Format(
                            "Invalid message callback {0}.{1}, must have exactly one argument, which must not be a ref and not be an out argument.",
                            declaringType.Name,
                            method.Name));
                }

                // All green!
                callbacks.Add(parameters[0].ParameterType);
            }
            return callbacks;
        }

        private void RebuildMessageDispatcher(Type messageType)
        {
            var messageParameter = Expression.Parameter(messageType);
            _messageCallbacks[messageType] =
                Expression.Lambda(
                    Expression.Block(
                        from system in _systems
                        from method in system.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        where method.IsDefined(typeof (MessageCallbackAttribute), true)
                        where method.GetParameters()[0].ParameterType == messageType
                        select Expression.Call(Expression.Constant(system), method, messageParameter)),
                    messageParameter).Compile();
        }

        #endregion

        #region Entities and Components

        /// <summary>Creates a new entity and returns its ID.</summary>
        /// <returns>The id of the new entity.</returns>
        public int AddEntity()
        {
            // Allocate a new entity id and a component mapping for the entity.
            var entity = _entityIds.GetId();
            _entities[entity] = AllocateEntity();
            return entity;
        }

        /// <summary>Removes an entity and all its components from the system.</summary>
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
            EntityRemoved message;
            message.Entity = entity;
            SendMessage(message);
        }

        /// <summary>Test whether the specified entity exists.</summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        public bool HasEntity(int entity)
        {
            return _entityIds.InUse(entity);
        }

        /// <summary>Removes all entities (and their components) from the system.</summary>
        public void Clear()
        {
            lock (this)
            {
                var next = _entityIds.GetId();
                _entityIds.ReleaseId(next);

                for (var i = next - 1; i >= 1; i--)
                {
                    if (HasEntity(i))
                    {
                        RemoveEntity(i);
                    }
                }

                Debug.Assert(_entityIds.Count == 0 && _componentIds.Count == 0);
            }
        }

        /// <summary>Creates a new component for the specified entity.</summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="entity">The entity to attach the component to.</param>
        /// <returns>The new component.</returns>
        public T AddComponent<T>(int entity) where T : Component, new()
        {
            // Make sure that entity exists.
            Debug.Assert(HasEntity(entity), "No such entity in the system.");

            // The create the component and set it up.
            var component = (T) AllocateComponent(typeof (T));
            component.Manager = this;
            component.Id = _componentIds.GetId();
            component.Entity = entity;
            component.Enabled = true;
            _components[component.Id] = component;

            // Add to entity index.
            _entities[entity].Add(component);

            // Send a message to all systems.
            ComponentAdded message;
            message.Component = component;
            SendMessage(message);

            // Return the created component.
            return component;
        }

        /// <summary>Removes the specified component from the system.</summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(Component component)
        {
            // Validate the component.
            Debug.Assert(component != null);
            Debug.Assert(HasComponent(component.Id), "No such component in the system.");

            // Send a message to all systems.
            ComponentRemoved message;
            message.Component = component;
            SendMessage(message);

            // Remove it from the mapping and release the id for reuse.
            _entities[component.Entity].Remove(component);
            _components[component.Id] = null;
            _componentIds.ReleaseId(component.Id);
            ReleaseComponent(component);
        }

        /// <summary>Removes the specified component from the system.</summary>
        /// <param name="componentId">The id of the component to remove.</param>
        public void RemoveComponent(int componentId)
        {
            // Validate the component.
            Debug.Assert(HasComponent(componentId), "No such component in the system.");

            // Re-use instance based removal.
            RemoveComponent(_components[componentId]);
        }

        /// <summary>Test whether the component with the specified id exists.</summary>
        /// <param name="componentId">The id of the component to check for.</param>
        /// <returns>Whether the manager contains the component or not.</returns>
        public bool HasComponent(int componentId)
        {
            return _componentIds.InUse(componentId);
        }

        /// <summary>
        ///     Gets a component of the specified type for an entity. If there are multiple components of the same type attached to
        ///     the entity, use the <c>index</c> parameter to select which one to get.
        /// </summary>
        /// <param name="entity">The entity to get the component of.</param>
        /// <param name="typeId">The type of the component to get.</param>
        /// <returns>The component.</returns>
        public Component GetComponent(int entity, int typeId)
        {
            Debug.Assert(HasEntity(entity), "No such entity in the system.");
            return _entities[entity].GetComponent(typeId);
        }

        /// <summary>Get a component by its id.</summary>
        /// <param name="componentId">The if of the component to retrieve.</param>
        /// <returns>The component with the specified id.</returns>
        public Component GetComponentById(int componentId)
        {
            Debug.Assert(HasComponent(componentId), "No such component in the system.");
            return _components[componentId];
        }

        /// <summary>Allows enumerating over all components of the specified entity.</summary>
        /// <param name="entity">The entity for which to get the components.</param>
        /// <param name="typeId">The type of components to get.</param>
        /// <returns>An enumerable listing all components of that entity.</returns>
        public IEnumerable<Component> GetComponents(int entity, int typeId)
        {
            Debug.Assert(HasEntity(entity), "No such entity in the system.");
            return _entities[entity].GetComponents(typeId);
        }

        #endregion

        #region Messaging

        /// <summary>Inform all interested systems of a message.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The sent message.</param>
        public void SendMessage<T>(T message)
        {
            Delegate dispatcher;
            if (_messageCallbacks.TryGetValue(typeof (T), out dispatcher))
            {
                ++_messageDepth;
                try
                {
                    ((Action<T>) dispatcher)(message);
                }
                finally
                {
                    if (--_messageDepth == 0)
                    {
                        // Make released component instances from the last update available
                        // for reuse, as we can be sure they're not referenced in our
                        // systems anymore.
                        ReleaseDirty();
                    }
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            // Write the components, which are enough to implicitly restore the
            // entity to component mapping as well, so we don't need to write
            // the entity mapping. Entities without any components are already
            // known via the entity ids.    
            packet.Write(_componentIds.Count);
            foreach (var component in Components)
            {
                // Store type manually to allow going through object pool on
                // deserialization.
                packet.Write(component.GetType());
                packet.Write(component);
            }

            // Write systems, with their types, as these will only be read back
            // via ReadPacketizableInto() to keep some variables that can only
            // passed in the constructor (in particular for presentation systems).
            packet.Write(_systems.Count(Packetizable.IsPacketizable));
            foreach (var system in _systems.Where(Packetizable.IsPacketizable))
            {
                packet.Write(system.GetType());
                packet.Write(system);
            }

            return packet;
        }

        [OnPreDepacketize]
        public void PreDepacketize()
        {
            // Release all current objects. We need to do this before the rest of
            // the depacketization process because the auto-packetizer will read
            // the entity and component ids.

            foreach (var entity in _entityIds)
            {
                ReleaseEntity(_entities[entity]);
            }
            _entities.Clear();

            foreach (var component in _componentIds.Select(id => _components[id]))
            {
                ReleaseComponent(component);
            }
            _components.Clear();
        }

        [OnPostDepacketize]
        public void PostDepacketize(IReadablePacket packet)
        {
            // Restore entity objects (which we use for faster lookup of components
            // on an entity). This in particular re-creates empty entities, i.e.
            // entities with no components.
            foreach (var entityId in _entityIds)
            {
                _entities[entityId] = AllocateEntity();
            }

            // Read back all components.
            var componentCount = packet.ReadInt32();
            for (var i = 0; i < componentCount; ++i)
            {
                var type = packet.ReadType();
                var component = AllocateComponent(type);
                packet.ReadPacketizableInto(component);
                component.Manager = this;
                _components[component.Id] = component;
                _entities[component.Entity].Add(component);
            }

            // Read back all systems.
            var systemCount = packet.ReadInt32();
            for (var i = 0; i < systemCount; ++i)
            {
                var type = packet.ReadType();
                var instance = _systemsByTypeId[GetSystemTypeId(type)];
                Debug.Assert(instance != null);
                packet.ReadPacketizableInto(instance);
            }

            // All done, send message to allow post-processing, e.g. for fetching
            // their components again, which is why this has to come last.
            Initialize message;
            SendMessage(message);
        }

        /// <summary>
        ///     Write a complete entity, meaning all its components, to the specified packet. Entities saved this way can be
        ///     restored using the <see cref="DepacketizeEntity"/> method. Note that this has no knowledge about components'
        ///     internal states, so if they keep references to other entities or components via their id, these ids will obviously
        ///     be wrong after depacketizing. You will have to take care of fixing these references yourself.
        ///     <para/>
        ///     This uses the components' serialization facilities.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="entity">The entity to write.</param>
        /// <returns>The packet after writing the entity's components.</returns>
        public IWritablePacket PacketizeEntity(IWritablePacket packet, int entity)
        {
            return packet.WriteWithTypeInfo((ICollection<Component>) _entities[entity].Components);
        }

        /// <summary>
        ///     Reads an entity from the specified packet, meaning all its components. This will create a new entity, with an id
        ///     that may differ from the id the entity had when it was written.
        ///     <para/>
        ///     In particular, all re-created components will likely have different different ids as well, so this method is not
        ///     suited for storing components that reference other components, even if just by their ID.
        ///     <para/>
        ///     This will act as though all of the written components were added, i.e. for each restored component all systems'
        ///     <see cref="AbstractSystem.OnComponentAdded"/> method will be called.
        ///     <para/>
        ///     This uses the components' serialization facilities.
        /// </summary>
        /// <param name="packet">The packet to read the entity from.</param>
        /// <param name="componentIdMap">A mapping of how components' ids changed due to serialization, mapping old id to new id.</param>
        /// <returns>The id of the read entity.</returns>
        public int DepacketizeEntity(IReadablePacket packet, Dictionary<int, int> componentIdMap = null)
        {
            // Keep track of what we already did, to allow unwinding if something
            // bad happens. Then get an entity id and try to read the components.
            var undo = new Stack<Action>();
            var entity = AddEntity();
            undo.Push(() => RemoveEntity(entity));
            try
            {
                // Read all components that were written for this entity. This
                // does not yet mess with our internal state.
                var components = packet.ReadPacketizablesWithTypeInfo<Component>();

                // Now we need to inject the components into our system, so we assign
                // an id to each one and link it to our entity id.
                foreach (var component in components)
                {
                    // Link stuff together.
                    var id = _componentIds.GetId();
                    if (componentIdMap != null)
                    {
                        componentIdMap.Add(component.Id, id);
                    }
                    component.Id = id;
                    component.Entity = entity;
                    component.Manager = this;
                    _components[component.Id] = component;

                    // Add to entity index.
                    _entities[entity].Add(component);

                    // Push to undo queue in case a message handler throws.
                    undo.Push(() => RemoveComponent(id));

                    // Send a message to all systems.
                    ComponentAdded message;
                    message.Component = component;
                    SendMessage(message);
                }

                // Yay, all went well. Return the id of the read entity.
                return entity;
            }
            catch (Exception)
            {
                // Undo all we did.
                while (undo.Count > 0)
                {
                    undo.Pop()();
                }
                throw;
            }
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            // Write the components.
            w.AppendIndent(indent).Write("ComponentCount = ");
            w.Write(_componentIds.Count);
            w.AppendIndent(indent).Write("Components = {");
            foreach (var component in Components)
            {
                w.AppendIndent(indent + 1).Write(component.GetType());
                w.Write(" = ");
                w.Dump(component, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            // Write systems.
            w.AppendIndent(indent).Write("SystemCount (excluding IDrawingSystems) = ");
            w.Write(_systems.Count(Packetizable.IsPacketizable));
            w.AppendIndent(indent).Write("Systems = {");
            for (int i = 0, j = _systems.Count; i < j; ++i)
            {
                if (!Packetizable.IsPacketizable(_systems[i]))
                {
                    continue;
                }
                w.AppendIndent(indent + 1).Write(_systems[i].GetType());
                w.Write(" = ");
                w.Dump(_systems[i], indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Copying

        /// <summary>
        ///     Not supported, to avoid issues with assumptions on how presentation related systems should be handled. Manually
        ///     create a new instance instead and use <see cref="CopyInto"/> on it.
        /// </summary>
        /// <returns>The copy.</returns>
        public IManager NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>Creates a deep copy of the object, reusing the given object.</summary>
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
            var copy = (Manager) into;

            // Copy id managers.
            _entityIds.CopyInto(copy._entityIds);
            _componentIds.CopyInto(copy._componentIds);

            // Copy components and entities.
            copy._components.Clear();
            foreach (var component in _componentIds.Select(id => _components[id]))
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
            foreach (var system in _systems
                .Where(system => !system.GetType().IsDefined(typeof (PresentationOnlyAttribute), true)))
            {
                copy.CopySystem(system);
            }

            // All done, send message to allow post-processing.
            Initialize message;
            SendMessage(message);
        }

        #endregion
    }
}