using System;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Entity event, used to signal entity changes in the <c>EntityManager</c>.
    /// </summary>
    public sealed class EntityEventArgs : EventArgs
    {
        /// <summary>
        /// The entity that triggered the event.
        /// </summary>
        public readonly Entity Entity;

        /// <summary>
        /// The UID the entity had, prior to what triggered the event.
        /// </summary>
        public readonly int EntityUid;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityEventArgs"/>
        /// class.
        /// </summary>
        /// <param name="entity">The entity that triggered the event.</param>
        /// <param name="entityUid">The UID the entity had, prior to what
        /// triggered the event.</param>
        public EntityEventArgs(Entity entity, int entityUid)
        {
            this.Entity = entity;
            this.EntityUid = entityUid;
        }
    }
}
