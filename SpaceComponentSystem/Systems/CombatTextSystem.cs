using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    public sealed class CombatTextSystem : AbstractSystem, IMessagingSystem
    {
        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as DamageApplied?;
                if (cm != null)
                {
                    var value = (int)System.Math.Round(cm.Value.Amount);
                    if (value < 1)
                    {
                        return;
                    }
                    var position = ((Transform)Manager.GetComponent(cm.Value.Entity, Transform.TypeId)).Translation;
                    var color = cm.Value.IsCriticalHit ? Color.Yellow : Color.White;
                    ((FloatingTextSystem)Manager.GetSystem(FloatingTextSystem.TypeId)).Display(value, position, color);
                }
            }
            {
                var cm = message as DamageBlocked?;
                if (cm != null)
                {
                    var position = ((Transform)Manager.GetComponent(cm.Value.Entity, Transform.TypeId)).Translation;
                    ((FloatingTextSystem)Manager.GetSystem(FloatingTextSystem.TypeId)).Display("Blocked!", position, Color.White);
                }
            }
        }
    }
}
