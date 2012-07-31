using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles applying damage when two entities collide.
    /// </summary>
    public sealed class CollisionDamageSystem : AbstractParallelComponentSystem<CollisionDamage>
    {
        #region Logic

        /// <summary>
        /// Decrements cooldowns, clears entries for expired cooldown.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, CollisionDamage component)
        {
            // Decrement, remove if run out.
            foreach (var entityId in new List<int>(component.Cooldowns.Keys))
            {
                if (component.Cooldowns[entityId] > 0)
                {
                    component.Cooldowns[entityId]--;
                }
                else
                {
                    component.Cooldowns.Remove(entityId);
                }
            }
        }

        /// <summary>
        /// Handle collision messages by applying damage, if possible.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is Collision)
            {
                // We only explicitly handle the first entity here, because we'll get the
                // same message in reverse for the other entity anyway. We still need it
                // for reference, though (collision damage).
                var damageMessage = (Collision)(ValueType)message;
                var firstEntity = damageMessage.FirstEntity;
                var secondEntity = damageMessage.SecondEntity;

                // Get damage we might take.
                var secondDamage = ((CollisionDamage)Manager.GetComponent(secondEntity, CollisionDamage.TypeId));

                // Checking if the cooldowns contain this entry can be done without
                // locking, because we're the only thread that would set the entry for
                // that value.
                if (secondDamage != null && !secondDamage.Cooldowns.ContainsKey(firstEntity))
                {
                    // Apply damage to second entity.
                    var firstHealth = ((Health)Manager.GetComponent(firstEntity, Health.TypeId));
                    if (firstHealth != null)
                    {
                        firstHealth.SetValue(firstHealth.Value - secondDamage.Damage);
                    }

                    // One-shot?
                    if (secondDamage.Cooldown == 0)
                    {
                        // Yes, kill it.
                        ((DeathSystem)Manager.GetSystem(DeathSystem.TypeId)).Kill(secondEntity);
                    }
                    else
                    {
                        // No, keep cooldown for this one, if it is still alive.
                        lock (secondDamage.Cooldowns)
                        {
                            secondDamage.Cooldowns.Add(firstEntity, secondDamage.Cooldown);
                        }
                    }   
                }
            }
        }

        #endregion
    }
}
