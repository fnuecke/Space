using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles AI logic updates.
    /// </summary>
    public sealed class AISystem : AbstractParallelComponentSystem<ArtificialIntelligence>, IMessagingSystem
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
            var cm = message as EntityDied?;
            if (cm != null)
            {
                foreach (var ai in Components)
                {
                    ai.OnEntityInvalidated(cm.Value.KilledEntity);
                }
            }
        }

        #endregion
    }
}
