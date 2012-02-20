using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles applying damage when two entities collide.
    /// </summary>
    public sealed class CollisionDamageSystem : AbstractComponentSystem<CollisionDamage>
    {
        #region Single allocation
        
        /// <summary>
        /// Reused for iterating cooldown entries.
        /// </summary>
        private List<int> _reusableCooldownList = new List<int>();

        #endregion

        #region Logic
        
        /// <summary>
        /// Decrements cooldowns, clears entries for expired cooldown.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(GameTime gameTime, long frame, CollisionDamage component)
        {
            // Decrement, remove if run out.
            _reusableCooldownList.AddRange(component.Cooldowns.Keys);
            foreach (var entityId in _reusableCooldownList)
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
            _reusableCooldownList.Clear();
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
                var damageMessage = (Collision)(ValueType)message;
                var firstEntity = damageMessage.FirstEntity;
                var secondEntity = damageMessage.SecondEntity;

                ApplyDamage(firstEntity, secondEntity);
                ApplyDamage(secondEntity, firstEntity);
            }
        }

        #endregion

        #region Utility methods
        
        /// <summary>
        /// Applies the damage of the first entity to the second.
        /// </summary>
        /// <param name="damager">The damager.</param>
        /// <param name="damagee">The damagee.</param>
        private void ApplyDamage(int damager, int damagee)
        {
            // Get damage component of first involved component.
            var damage = Manager.GetComponent<CollisionDamage>(damager);

            // If we don't do any damage, return.
            if (damage == null)
            {
                return;
            }

            // On cooldown?
            if (damage.Cooldowns.ContainsKey(damagee))
            {
                // Yes.
                return;
            }

            // Apply damage if we can.
            var health = Manager.GetComponent<Health>(damagee);
            if (health != null)
            {
                health.SetValue(health.Value - damage.Damage);
            }

            // One-shot?
            if (damage.Cooldown == 0)
            {
                // Yes, remove from system.
                Manager.RemoveEntity(damager);
            }
            else if (health != null)
            {
                // No, keep cooldown for this one - if it had any health.
                damage.Cooldowns.Add(damagee, damage.Cooldown);
            }
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (CollisionDamageSystem)base.DeepCopy(into);

            if (copy != into)
            {
                copy._reusableCooldownList = new List<int>();
            }

            return copy;
        }

        #endregion
    }
}
