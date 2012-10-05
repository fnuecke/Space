using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles applying damage when two entities collide.
    /// </summary>
    public sealed class CollisionDamageSystem : AbstractSystem, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// List of current collisions, mapping to their damage effect.
        /// </summary>
        private Dictionary<ulong, int> _collisions = new Dictionary<ulong, int>();

        #endregion

        #region Logic

        /// <summary>
        /// Handle collision messages by applying damage, if possible.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
        {
            if (message is BeginCollision)
            {
                // We only get one message for a collision pair, so we apply damage
                // to both parties.
                var damageMessage = (BeginCollision)(ValueType)message;
                BeginDamage(damageMessage.EntityA, damageMessage.EntityB);
                BeginDamage(damageMessage.EntityB, damageMessage.EntityA);

            }
            else if (message is EndCollision)
            {
                // Stop damage that is being applied because of this collision.
                var damageMessage = (EndCollision)(ValueType)message;
                EndDamage(damageMessage.EntityA, damageMessage.EntityB);
                EndDamage(damageMessage.EntityB, damageMessage.EntityA);
            }
        }

        private void BeginDamage(int damagee, int damager)
        {
            // Get damage we might take.
            var damage = (CollisionDamage)Manager.GetComponent(damager, CollisionDamage.TypeId);
            if (damage == null)
            {
                return;
            }

            // Apply damage to second entity if it has health.
            var hasHealth = Manager.GetComponent(damagee, Health.TypeId) != null;

            // One-shot?
            if (damage.Cooldown == 0)
            {
                // Yes, kill the damager after applying our damage.
                ((DeathSystem)Manager.GetSystem(DeathSystem.TypeId)).MarkForRemoval(damager);
                if (hasHealth)
                {
                    Manager.AddComponent<DamagingStatusEffect>(damagee).Initialize(damage.Damage, damager);
                }
            }
            else if (hasHealth)
            {
                // No, keep cooldown for this one, if it is still alive.
                var effect = Manager.AddComponent<DamagingStatusEffect>(damagee).Initialize(DamagingStatusEffect.InfiniteDamageDuration, damage.Damage, damage.Cooldown, damager);
                _collisions.Add(BitwiseMagic.Pack(damagee, damager), effect.Id);
            }
        }

        private void EndDamage(int damagee, int damager)
        {
            int effectId;
            if (_collisions.TryGetValue(BitwiseMagic.Pack(damagee, damager), out effectId))
            {
                Manager.RemoveComponent(effectId);
            }
        }

        #endregion
    }
}
