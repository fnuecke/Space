using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    public class EntityManager : IEntityManager
    {
        #region Properties

        /// <summary>
        /// <summary>
        /// All entities registered with this manager.
        /// </summary>
        public ReadOnlyCollection<IEntity> Entities { get { return _entities.AsReadOnly(); } }

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        public IComponentSystemManager SystemManager { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of child entities this state drives.
        /// </summary>
        private List<IEntity> _entities = new List<IEntity>();

        /// <summary>
        /// The counter used to assign ids to added entities. Start at one
        /// to avoid referencing the first added object with invalidly
        /// initialized "pointers".
        /// </summary>
        private long _nextEntityId = 1;

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
        public void AddEntity(IEntity entity)
        {
            if (entity.Manager == this)
            {
                return;
            }
            else if (entity.Manager != null)
            {
                throw new ArgumentException("Entity is already part of an entity manager.", "entity");
            }
            else
            {
                entity.UID = _nextEntityId++;
                AddEntityUnchecked(entity);
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
        public IEntity RemoveEntity(long entityUid)
        {
            if (entityUid > 0)
            {
                int index = _entities.FindIndex(e => e.UID == entityUid);
                if (index >= 0)
                {
                    var entity = _entities[index];
                    foreach (var component in entity.Components)
                    {
                        SystemManager.RemoveComponent(component);
                    }
                    _entities.RemoveAt(index);
                    entity.UID = -1;
                    entity.Manager = null;
                    return entity;
                }
            }
            return null;
        }

        /// <summary>
        /// Get a entity's current representation in this manager by its id.
        /// </summary>
        /// <param name="entityUid">The id of the entity to look up.</param>
        /// <returns>The current representation in this manager.</returns>
        public IEntity GetEntity(long entityUid)
        {
            if (entityUid > 0)
            {
                return _entities.Find(e => e.UID == entityUid);
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
            _entities.Add(entity);
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
            // Write the list of entities we track.
            packet.Write(_entities.Count);
            foreach (var entity in _entities)
            {
                Packetizer.Packetize(entity, packet);
            }

            // Next id we'll distribute.
            packet.Write(_nextEntityId);

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            // Clear component lists. Just do a clone, which preserves the
            // non-entity bound components for us.
            SystemManager = (IComponentSystemManager)SystemManager.Clone();

            // And finally the objects. Remove the one we know before that.
            _entities.Clear();
            int numEntitys = packet.ReadInt32();
            for (int i = 0; i < numEntitys; ++i)
            {
                AddEntityUnchecked(Packetizer.Depacketize<IEntity>(packet));
            }

            // Next id we'll distribute.
            _nextEntityId = packet.ReadInt64();
        }

        public void Hash(Hasher hasher)
        {
            foreach (var entity in _entities)
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
            copy._entities = new List<IEntity>();
            foreach (var entity in _entities)
            {
                copy.AddEntityUnchecked((IEntity)entity.Clone());
            }

            return copy;
        }

        #endregion
    }
}
