using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Interface for classes managing a list of entities.
    /// </summary>
    public interface IEntityManager : IPacketizable, IHashable, ICopyable<IEntityManager>
    {
        #region Properties
        
        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        ISystemManager SystemManager { get; set; }

#if DEBUG && GAMELOG
        /// <summary>
        /// Whether to log any game state changes in detail, for debugging.
        /// </summary>
        bool GameLogEnabled { get; set; }
#endif

        #endregion

        #region Accessors

        /// <summary>
        /// Add an entity object to this manager. This will add all the
        /// entity's components to the associated component system manager.
        /// 
        /// <para>
        /// This will assign an entity a unique id, by which it can be
        /// referenced in this manager, and clones of it.
        /// </para>
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The id that was assigned to the entity.</returns>
        int AddEntity(Entity entity);

        /// <summary>
        /// Remove an entity from this manager. This will remove all the
        /// entity's components from the associated component system manager.
        /// 
        /// <para>
        /// This will unset the entities id, so it can be reused.
        /// </para>
        /// </summary>
        /// <param name="updateable">The entity to remove.</param>
        void RemoveEntity(Entity entity);

        /// <summary>
        /// Remove a entity by its id from this manager. This will remove all the
        /// entity's components from the associated component system manager.
        /// 
        /// <para>
        /// This will unset the entities id, so it can be reused.
        /// </para>
        /// </summary>
        /// <param name="entityUid">The id of the entity to remove.</param>
        /// <returns>The removed entity, or <c>null</c> if this manager has
        /// no entity with the specified id.</returns>
        Entity RemoveEntity(int entityUid);

        /// <summary>
        /// Get a entity's current representation in this manager by its id.
        /// </summary>
        /// <param name="entityUid">The id of the entity to look up.</param>
        /// <returns>The current representation in this manager.</returns>
        Entity GetEntity(int entityUid);
        
        /// <summary>
        /// Test whether the specified entity is in this manager.
        /// </summary>
        /// <param name="entityUid">The id of the entity to check for.</param>
        /// <returns>Whether the manager contains the entity or not.</returns>
        bool Contains(int entityUid);

        #endregion

        #region Messaging

        /// <summary>
        /// Inform all entities in this system of a message.
        /// </summary>
        /// <param name="message">The sent message.</param>
        void SendMessage<T>(ref T message) where T : struct;

        #endregion
    }
}
