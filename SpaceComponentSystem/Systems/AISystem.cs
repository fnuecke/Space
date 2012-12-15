using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles AI logic updates.
    /// </summary>
    public sealed class AISystem : AbstractUpdatingComponentSystem<ArtificialIntelligence>, IMessagingSystem
    {
        #region Logic

        /// <summary>
        /// Updates AI component behaviors.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ArtificialIntelligence component)
        {
            component.Update();
        }

        /// <summary>
        /// Called by the manager when an entity was removed.
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        public override void OnEntityRemoved(int entity)
        {
            base.OnEntityRemoved(entity);

            foreach (var ai in Components)
            {
                ai.OnEntityInvalidated(entity);
            }
        }

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            // If an entity was removed from the system we want to make sure
            // that no AI is targeting it anymore. Otherwise the id may be
            // re-used before the AI is updated, leading to bad references.
            {
                var cm = message as EntityDied?;
                if (cm != null)
                {
                    foreach (var ai in Components)
                    {
                        ai.OnEntityInvalidated(cm.Value.KilledEntity);
                    }
                }
            }
            // When an AI receives damage and isn't already attacking something
            // we want to make sure it tries to defend itself.
            {
                var cm = message as DamageReceived?;
                if (cm != null && cm.Value.Owner != 0)
                {
                    var ai = (ArtificialIntelligence)Manager.GetComponent(cm.Value.Damagee, ArtificialIntelligence.TypeId);
                    if (ai != null && ai.CurrentBehavior != ArtificialIntelligence.BehaviorType.Attack)
                    {
                        ai.Attack(cm.Value.Owner);
                    }
                }
            }
        }

        #endregion
    }
}
