using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Manages a list of entities, and their components (adding them to the
    /// associated component system manager / removing them).
    /// </summary>
    public class EntityManager : IEntityManager
    {
        #region Properties

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        public ISystemManager SystemManager { get; set; }

#if DEBUG && GAMELOG
        /// <summary>
        /// Whether to log any game state changes in detail, for debugging.
        /// </summary>
        public bool GameLogEnabled { get; set; }
#endif

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

        /// <summary>
        /// Creates a new entity manager.
        /// </summary>
        public EntityManager()
        {
            SystemManager = new SystemManager();
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
                _entityMap.Remove(entityUid);
                _idManager.ReleaseId(entity.UID);

                EntityRemoved message;
                message.Entity = entity;
                SendMessage(ref message);

                entity.Manager = null;
                entity.UID = -1;

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
            return _entityMap[entityUid];
        }

        /// <summary>
        /// Test whether the specified entity is in this manager.
        /// </summary>
        /// <param name="entityUid">The id of the entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        public bool Contains(int entityUid)
        {
            return _entityMap.ContainsKey(entityUid);
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

            EntityAdded message;
            message.Entity = entity;
            SendMessage(ref message);
        }

        /// <summary>
        /// Remove all entities from this manager.
        /// </summary>
        private void Clear()
        {
            _entityMap.Clear();

            EntitiesCleared message;
            SendMessage(ref message);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Inform all entities in this system of a message.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public void SendMessage<T>(ref T message) where T : struct
        {
            SystemManager.SendMessage(ref message);

            // Cannot use single-allocated list here, because this can cascade.
            foreach (var entity in new List<Entity>(_entityMap.Values))
            {
                entity.SendMessage(ref message);
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
            // Systems that need synchronization.
            packet.Write(SystemManager);

            // Write the list of entities we track.
            packet.Write(_entityMap.Values);

            // Id manager.
            packet.Write(_idManager);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            // Clear component lists.
            packet.ReadPacketizableInto(SystemManager);

            // Read back all entities to add.
            Clear();
            foreach (var entity in packet.ReadPacketizables<Entity>())
            {
                AddEntityUnchecked(entity);
            }

            // Id manager.
            packet.ReadPacketizableInto(_idManager);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            SystemManager.Hash(hasher);
            foreach (var entity in _entityMap.Values)
            {
                entity.Hash(hasher);
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Create a deep copy of the object.
        /// </summary>
        /// <returns>A deep copy of this entity.</returns>
        public IEntityManager DeepCopy()
        {
            return DeepCopy(null);
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
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

                // Get a list of entities for re-use.
                var copyValues = new Stack<Entity>(copy._entityMap.Values);
                copy.Clear();

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
