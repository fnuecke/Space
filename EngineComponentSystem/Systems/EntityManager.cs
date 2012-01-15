using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Manages a list of entities, and their components (adding them to the
    /// associated component system manager / removing them).
    /// </summary>
    public class EntityManager : IEntityManager
    {
        #region Events

        /// <summary>
        /// Triggered when an entity was added to this manager.
        /// </summary>
        public event EventHandler<EntityEventArgs> Added;

        /// <summary>
        /// Triggered when an entity was removed from this manager.
        /// </summary>
        public event EventHandler<EntityEventArgs> Removed;

        #endregion

        #region Properties

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        public IComponentSystemManager SystemManager { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of child entities this state drives.
        /// </summary>
        private Dictionary<int, Entity> _entityMap = new Dictionary<int, Entity>();

        /// <summary>
        /// Manager for entity ids.
        /// </summary>
        private IdManager _idManager = new IdManager();

        #endregion

        #region Constructor

        public EntityManager()
        {
            SystemManager = new ComponentSystemManager();
            SystemManager.EntityManager = this;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add an entity object to this manager. This will add all the
        /// entity's components to the associated component system manager.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The id that was assigned to the entity.</returns>
        public int AddEntity(Entity entity)
        {
            if (entity.Manager == this)
            {
                return entity.UID;
            }
            else if (entity.Manager != null)
            {
                throw new ArgumentException("Entity is already part of an entity manager.", "entity");
            }
            else
            {
                entity.UID = _idManager.GetId();
                AddEntityUnchecked(entity);
                return entity.UID;
            }
        }

        /// <summary>
        /// Remove an entity from this manager. This will remove all the
        /// entity's components from the associated component system manager.
        /// </summary>
        /// <param name="updateable">The entity to remove.</param>
        public void RemoveEntity(Entity entity)
        {
            if (entity.Manager != this)
            {
                return;
            }
            RemoveEntity(entity.UID);
        }

        /// <summary>
        /// Remove a entity by its id from this manager. This will remove all the
        /// entity's components from the associated component system manager.
        /// </summary>
        /// <param name="entityUid">The id of the entity to remove.</param>
        /// <returns>The removed entity, or <c>null</c> if this manager has
        /// no entity with the specified id.</returns>
        public Entity RemoveEntity(int entityUid)
        {
            var entity = GetEntity(entityUid);
            if (entity != null)
            {
                foreach (var component in entity.Components)
                {
                    SystemManager.RemoveComponent(component);
                }
                _entityMap.Remove(entityUid);
                _idManager.ReleaseId(entity.UID);
                entity.Manager = null;
                entity.UID = -1;
                OnRemoved(new EntityEventArgs(entity, entityUid));
                return entity;
            }
            return null;
        }

        /// <summary>
        /// Get a entity's current representation in this manager by its id.
        /// </summary>
        /// <param name="entityUid">The id of the entity to look up.</param>
        /// <returns>The current representation in this manager.</returns>
        public Entity GetEntity(int entityUid)
        {
            //if (_entityMap.ContainsKey(entityUid))
            //{
                return _entityMap[entityUid];
            //}
            //return null;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Internal use, does not give the entity a new UID. Used for cloning.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        private void AddEntityUnchecked(Entity entity)
        {
            _entityMap.Add(entity.UID, entity);
            entity.Manager = this;
            foreach (var component in entity.Components)
            {
                SystemManager.AddComponent(component);
            }
            OnAdded(new EntityEventArgs(entity, -1));
        }

        #endregion

        #region Event dispatching

        private void OnAdded(EntityEventArgs e)
        {
            if (Added != null)
            {
                Added(this, e);
            }
        }

        private void OnRemoved(EntityEventArgs e)
        {
            if (Removed != null)
            {
                Removed(this, e);
            }
        }

        #endregion

        #region Serialization / Hashing

        public Packet Packetize(Packet packet)
        {
            // Systems that need synchronization.
            packet.Write(SystemManager);

            // Write the list of entities we track.
            packet.Write(_entityMap.Values);

            // Id manager.
            packet.Write(_idManager);

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            // Clear component lists.
            SystemManager.ClearComponents();
            packet.ReadPacketizableInto(SystemManager);

            // Read back all entities to add.
            _entityMap.Clear();
            foreach (var entity in packet.ReadPacketizables<Entity>())
            {
                AddEntityUnchecked(entity);
            }

            // Id manager.
            packet.ReadPacketizableInto(_idManager);
        }

        public void Hash(Hasher hasher)
        {
            foreach (var entity in _entityMap.Values)
            {
                entity.Hash(hasher);
            }
        }

        #endregion

        #region Copying

        public IEntityManager DeepCopy()
        {
            return DeepCopy(null);
        }

        public IEntityManager DeepCopy(IEntityManager into)
        {
            var copy = (EntityManager)(into ?? MemberwiseClone());

            if (copy == into)
            {
                // Not a shallow copy, also copy fields.

                // Clone the id manager.
                copy._idManager = _idManager.DeepCopy(copy._idManager);

                // Clone system manager.
                copy.SystemManager = SystemManager.DeepCopy(copy.SystemManager);

                // Clone all entities.
                copy._entityMap.Clear();

                // Get a list of entities for re-use.
                var copyValues = new Stack<Entity>(copy._entityMap.Values);
                copy._entityMap.Clear();

                // Copy actual entities over.
                foreach (var entity in _entityMap.Values)
                {
                    Entity entityCopy;
                    if (copyValues.Count > 0)
                    {
                        entityCopy = copyValues.Pop();
                    }
                    else
                    {
                        entityCopy = new Entity();
                    }
                    copy.AddEntityUnchecked(entity.DeepCopy(entityCopy));
                }
            }
            else
            {
                // Copy of this instance, create new instances for reference
                // types.

                // Clone the id manager.
                copy._idManager = _idManager.DeepCopy();

                // Clone system manager.
                copy.SystemManager = SystemManager.DeepCopy();

                // Clone all entities.
                copy._entityMap = new Dictionary<int, Entity>();
                foreach (var entity in _entityMap.Values)
                {
                    copy.AddEntityUnchecked(entity.DeepCopy());
                }
            }

            // Set the entity manager for the clone's system manager.
            copy.SystemManager.EntityManager = copy;

            return copy;
        }

        #endregion
    }
}
