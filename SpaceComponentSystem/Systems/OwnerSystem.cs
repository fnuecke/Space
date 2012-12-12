using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of cleaning up "dangling pointers" to owners
    /// that are removed from the system.
    /// </summary>
    public sealed class OwnerSystem : AbstractComponentSystem<Owner>
    {
        /// <summary>
        /// Called by the manager when an entity was removed.
        /// </summary>
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
    }
}
