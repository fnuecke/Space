using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system takes care of cleaning up "dangling pointers" to owners that are removed from the system.</summary>
    public sealed class OwnerSystem : AbstractComponentSystem<Owner>
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Logic

        /// <summary>Gets the root owner of an ownership chain, starting with the specified entity.</summary>
        /// <param name="entity">The entity to start at.</param>
        /// <returns>The root entry of the owner chain the entity is in.</returns>
        public int GetRootOwner(int entity)
        {
            while (entity != 0)
            {
                var owner = (Owner) Manager.GetComponent(entity, Owner.TypeId);
                if (owner != null && owner.Value != 0)
                {
                    entity = owner.Value;
                }
                else
                {
                    break;
                }
            }
            return entity;
        }

        /// <summary>Called by the manager when an entity was removed.</summary>
        /// <param name="entity">The entity that was removed.</param>
        public override void OnEntityRemoved(int entity)
        {
            base.OnEntityRemoved(entity);

            // Unset owner for all components where the removed entity
            // was the owner.
            foreach (var component in Components)
            {
                if (component.Value == entity)
                {
                    component.Value = 0;
                }
            }
        }

        #endregion
    }
}