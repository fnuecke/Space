using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// This file contains the component pooling logic for the component
    /// system manager. This is used to allow quick re-use of components
    /// without having to re-allocate memory and initialize it.
    /// </summary>
    public sealed partial class Manager
    {
        /// <summary>
        /// Objects that were released this very update run, so we don't want
        /// to reuse them until we enter the next update, to avoid reuse of
        /// components that might still be referenced in running system update
        /// loops.
        /// </summary>
        [PacketizerIgnore]
        private readonly SparseArray<Stack<Component>> _dirtyPool = new SparseArray<Stack<Component>>();

        /// <summary>
        /// Object pool for reusable instances.
        /// </summary>
        [PacketizerIgnore]
        private static readonly SparseArray<Stack<Component>> ComponentPool = new SparseArray<Stack<Component>>();

        /// <summary>
        /// Pool for entities (index structure only, used for faster component queries).
        /// </summary>
        [PacketizerIgnore]
        private static readonly Stack<Entity> EntityPool = new Stack<Entity>();

        /// <summary>
        /// Try fetching an instance from the pool, if that fails, create a new one.
        /// </summary>
        /// <param name="type">The type of component to get.</param>
        /// <returns>
        /// A new component of the specified type.
        /// </returns>
        private static Component AllocateComponent(Type type)
        {
            var typeId = GetComponentTypeId(type);
            lock (ComponentPool)
            {
                if (ComponentPool[typeId] != null && ComponentPool[typeId].Count > 0)
                {
                    return ComponentPool[typeId].Pop();
                }
            }
            return (Component)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Releases a component for later reuse.
        /// </summary>
        /// <param name="component">The component to release.</param>
        private void ReleaseComponent(Component component)
        {
            component.Reset();
            var typeId = component.GetTypeId();
            if (_dirtyPool[typeId] == null)
            {
                _dirtyPool[typeId] = new Stack<Component>();
            }
            //Debug.Assert(!_dirtyPool[typeId].Contains(component));
            _dirtyPool[typeId].Push(component);
        }

        /// <summary>
        /// Releases all instances that were possibly still referenced when
        /// they were removed from the manager in running system update
        /// loops.
        /// </summary>
        private void ReleaseDirty()
        {
            lock (ComponentPool)
            {
                foreach (var typeId in ComponentTypeIds)
                {
                    if (_dirtyPool[typeId] == null || _dirtyPool[typeId].Count == 0)
                    {
                        continue;
                    }
                    if (ComponentPool[typeId] == null)
                    {
                        ComponentPool[typeId] = new Stack<Component>();
                    }
                    foreach (var instance in _dirtyPool[typeId])
                    {
                        //Debug.Assert(!ComponentPool[typeId].Contains(instance));
                        ComponentPool[typeId].Push(instance);
                    }
                    _dirtyPool[typeId].Clear();
                }
            }
        }

        /// <summary>
        /// Allocates an entity for indexing.
        /// </summary>
        /// <returns>A new entity.</returns>
        private static Entity AllocateEntity()
        {
            lock (EntityPool)
            {
                if (EntityPool.Count > 0)
                {
                    return EntityPool.Pop();
                }
            }
            return new Entity();
        }

        /// <summary>
        /// Releases the entity for later reuse.
        /// </summary>
        /// <param name="entity">The entity.</param>
        private static void ReleaseEntity(Entity entity)
        {
            entity.Components.Clear();
            entity.TypeCache.Clear();
            lock (EntityPool)
            {
                EntityPool.Push(entity);
            }
        }
    }
}
