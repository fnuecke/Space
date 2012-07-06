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

                // Get damage component of involved components, and all
                // required info for the damage checks first, because one
                // of the two components may die midway through.
                var firstDamage = Manager.GetComponent<CollisionDamage>(firstEntity);
                var secondDamage = Manager.GetComponent<CollisionDamage>(secondEntity);

                // Get the actual values, if possible, and only if we're not
                // on cooldown. If it's zero we simply won't try to do any
                // damage.
                var firstDamageValue = 0f;
                // This is used to keep track of whether we already removed the
                // entity because it self-destructs on impact.
                var firstAlreadyDead = false;
                if (firstDamage != null && !firstDamage.Cooldowns.ContainsKey(secondEntity))
                {
                    firstDamageValue = firstDamage.Damage;
                    // One-shot?
                    if (firstDamage.Cooldown == 0)
                    {
                        // Yes, remove from system.
                        Manager.RemoveEntity(firstEntity);
                        // Remember we don't need to apply damage to it.
                        firstAlreadyDead = true;
                    }
                    else
                    {
                        // No, keep cooldown for this one, if it is still alive.
                        firstDamage.Cooldowns.Add(secondEntity, firstDamage.Cooldown);
                    }   
                }

                // Same as for first entity.
                var secondDamageValue = 0f;
                var secondAlreadyDead = false;
                if (secondDamage != null && !secondDamage.Cooldowns.ContainsKey(firstEntity))
                {
                    secondDamageValue = secondDamage.Damage;
                    // One-shot?
                    if (secondDamage.Cooldown == 0)
                    {
                        // Yes, remove from system.
                        Manager.RemoveEntity(secondEntity);
                        // Remember we don't need to apply damage to it.
                        secondAlreadyDead = true;
                    }
                    else
                    {
                        // No, keep cooldown for this one, if it is still alive.
                        secondDamage.Cooldowns.Add(firstEntity, secondDamage.Cooldown);
                    }   
                }

                // Apply damage where necessary.
                if (!secondAlreadyDead && firstDamageValue > 0)
                {
                    // Apply damage to second entity.
                    var secondHealth = Manager.GetComponent<Health>(secondEntity);
                    if (secondHealth != null)
                    {
                        secondHealth.SetValue(secondHealth.Value - firstDamageValue);
                    }
                }
                
                if (!firstAlreadyDead && secondDamageValue > 0)
                {
                    // Apply damage to second entity.
                    var firstHealth = Manager.GetComponent<Health>(firstEntity);
                    if (firstHealth != null)
                    {
                        firstHealth.SetValue(firstHealth.Value - secondDamageValue);
                    }
                }
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem DeepCopy()
        {
            var copy = (CollisionDamageSystem)base.DeepCopy();

            copy._reusableCooldownList = new List<int>();

            return copy;
        }

        #endregion
    }
}
