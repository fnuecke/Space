using System;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Triggers refilling regenerating values.
    /// </summary>
    public sealed class RegeneratingValueSystem : AbstractComponentSystem<AbstractRegeneratingValue>
    {
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

        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is CharacterStatsInvalidated)
            {
                var entity = ((CharacterStatsInvalidated)(ValueType)message).Entity;
                foreach (var component in Manager.GetComponents<AbstractRegeneratingValue>(entity))
                {
                    component.RecomputeValues();
                }
            }
        }
    }
}
