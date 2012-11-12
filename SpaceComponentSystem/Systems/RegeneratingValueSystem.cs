using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Triggers refilling regenerating values.
    /// </summary>
    public sealed class RegeneratingValueSystem : AbstractParallelComponentSystem<AbstractRegeneratingValue>, IMessagingSystem
    {
        #region Logic
        
        /// <summary>
        /// Updates the component's current value or timeout.
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, AbstractRegeneratingValue component)
        {
            if (component.TimeToWait > 0)
            {
                --component.TimeToWait;
            }
            else
            {
                component.SetValue(component.Value + component.Regeneration);
            }
        }

        /// <summary>
        /// Receives the specified message.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as CharacterStatsInvalidated?;
            if (cm == null)
            {
                return;
            }

            foreach (var component in Manager.GetComponents(cm.Value.Entity, AbstractRegeneratingValue.TypeId))
            {
                ((AbstractRegeneratingValue)component).RecomputeValues();
            }
        }

        #endregion
    }
}
