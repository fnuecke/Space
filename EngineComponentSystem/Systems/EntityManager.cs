using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    public class EntityManager : IEntityManager
    {
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
        //private List<IEntity> _entities = new List<IEntity>();

        private Dictionary<int, IEntity> _entityMap = new Dictionary<int, IEntity>();

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
        public int AddEntity(IEntity entity)
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
        public void RemoveEntity(IEntity entity)
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
        public IEntity RemoveEntity(int entityUid)
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
                entity.UID = -1;
                entity.Manager = null;
                return entity;
            }
            return null;
        }

        /// <summary>
        /// Get a entity's current representation in this manager by its id.
        /// </summary>
        /// <param name="entityUid">The id of the entity to look up.</param>
        /// <returns>The current representation in this manager.</returns>
        public IEntity GetEntity(int entityUid)
        {
            if (_entityMap.ContainsKey(entityUid))
            {
                return _entityMap[entityUid];
            }
            return null;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Internal use, does not give the entity a new UID. Used for cloning.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        private void AddEntityUnchecked(IEntity entity)
        {
            _entityMap.Add(entity.UID, entity);
            entity.Manager = this;
            foreach (var component in entity.Components)
            {
                SystemManager.AddComponent(component);
            }
        }

        #endregion

        #region Hashing / Cloning / Serialization

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
            // Clear component lists. Just do a clone, which preserves the
            // non-entity bound components for us.
            SystemManager = (IComponentSystemManager)SystemManager.Clone();
            packet.ReadPacketizableInto(SystemManager);

            // And finally the objects. Remove the one we know before that.
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

        public object Clone()
        {
            var copy = (EntityManager)MemberwiseClone();

            // Clone system manager.
            copy.SystemManager = (IComponentSystemManager)SystemManager.Clone();
            copy.SystemManager.EntityManager = copy;

            // Clone all entities.
            copy._entityMap = new Dictionary<int, IEntity>();
            foreach (var entity in _entityMap.Values)
            {
                copy.AddEntityUnchecked((IEntity)entity.Clone());
            }

            // Clone the id manager.
            copy._idManager = (IdManager)_idManager.Clone();

            return copy;
        }

        #endregion
    }
}
