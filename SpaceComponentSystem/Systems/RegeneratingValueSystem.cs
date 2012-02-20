using System;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Triggers refilling regenerating values.
    /// </summary>
    public sealed class RegeneratingValueSystem : AbstractComponentSystem<AbstractRegeneratingValue>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, AbstractRegeneratingValue component)
        {
            if (component.TimeToWait > 0)
            {
                --component.TimeToWait;
            }
            else
            {
                component.Value = Math.Min(component.MaxValue, component.Value + component.Regeneration);
            }
        }

        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is CharacterStatsInvalidated)
            {
                foreach (var component in Manager.GetComponents(((CharacterStatsInvalidated)(ValueType)message).Entity))
                {
                    if (component is AbstractRegeneratingValue)
                    {
                        ((AbstractRegeneratingValue)component).RecomputeValues();
                    }
                }
            }
        }
    }
}
