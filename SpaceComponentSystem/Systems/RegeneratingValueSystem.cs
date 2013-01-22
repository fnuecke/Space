using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Triggers refilling regenerating values.</summary>
    public sealed class RegeneratingValueSystem
        : AbstractParallelComponentSystem<AbstractRegeneratingValue>
    {
        #region Logic

        /// <summary>Updates the component's current value or timeout.</summary>
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

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<CharacterStatsInvalidated>(OnCharacterStatsInvalidated);
        }

        private void OnCharacterStatsInvalidated(CharacterStatsInvalidated message)
        {
            foreach (var component in Manager.GetComponents(message.Entity, AbstractRegeneratingValue.TypeId))
            {
                ((AbstractRegeneratingValue) component).RecomputeValues();
            }
        }

        #endregion
    }
}