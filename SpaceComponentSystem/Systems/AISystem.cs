using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Handles AI logic updates.</summary>
    public sealed class AISystem : AbstractUpdatingComponentSystem<ArtificialIntelligence>
    {
        #region Logic

        /// <summary>Updates AI component behaviors.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ArtificialIntelligence component)
        {
            component.Update();
        }

        /// <summary>Called by the manager when an entity was removed.</summary>
        /// <param name="message"></param>
        [MessageCallback]
        public void OnEntityRemoved(EntityRemoved message)
        {
            // If an entity was removed from the system we want to make sure
            // that no AI is targeting it anymore. Otherwise the id may be
            // re-used before the AI is updated, leading to bad references.
            foreach (var ai in Components)
            {
                ai.OnEntityInvalidated(message.Entity);
            }
        }

        /// <summary>Stop shooting if it's dead.</summary>
        [MessageCallback]
        public void OnEntityDied(EntityDied message)
        {
            foreach (var ai in Components)
            {
                ai.OnEntityInvalidated(message.KilledEntity);
            }
        }

        /// <summary>
        /// When an AI receives damage and isn't already attacking something
        /// we want to make sure it tries to defend itself.
        /// </summary>
        /// <param name="message"></param>
        [MessageCallback]
        public void OnDamageReceived(DamageReceived message)
        {
            var ai = (ArtificialIntelligence) Manager.GetComponent(message.Damagee, ArtificialIntelligence.TypeId);
            if (ai != null && ai.CurrentBehavior != ArtificialIntelligence.BehaviorType.Attack &&
                // Make sure it makes sense to fire back. For example, it doesn't make sense
                // to start shooting a radiating body / nebula.
                Manager.GetComponent(message.Owner, Health.TypeId) != null)
            {
                ai.Attack(message.Owner);
            }
        }

        #endregion
    }
}