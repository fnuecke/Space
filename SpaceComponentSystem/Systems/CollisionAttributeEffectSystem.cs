using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles applying effects defined through attributes when two entities collide.
    /// </summary>
    public sealed class CollisionAttributeEffectSystem : AbstractComponentSystem<DamagingStatusEffect>, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// List of current collisions, mapping to their damage effect.
        /// </summary>
        private Dictionary<ulong, int> _collisions = new Dictionary<ulong, int>();

        /// <summary>
        /// Randomizer used for determining whether certain effects should be applied
        /// (e.g. dots, blocking, ...).
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic

        /// <summary>
        /// Handle collision messages by applying damage, if possible.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as BeginCollision?;
                if (cm != null)
                {
                    // We only get one message for a collision pair, so we apply damage
                    // to both parties.
                    var m = cm.Value;
                    BeginCollision(m.EntityA, m.EntityB, m.Normal);
                    BeginCollision(m.EntityB, m.EntityA, -m.Normal);
                    return;
                }
            }
            {
                var cm = message as EndCollision?;
                if (cm != null)
                {
                    // Stop damage that is being applied because of this collision.
                    var m = cm.Value;
                    EndCollision(m.EntityA, m.EntityB);
                    EndCollision(m.EntityB, m.EntityA);
                }
            }
        }

        private void BeginCollision(int damagee, int damager, Vector2 normal)
        {
            // Do we do any damage at all?
            var damage = (CollisionDamage)Manager.GetComponent(damager, CollisionDamage.TypeId);
            if (damage == null)
            {
                // Damager does not apply any damage, forget about it.
                return;
            }

            // Should the damager be removed when colliding (self-destructs)?
            if (damage.RemoveOnCollision)
            {
                // Yep, mark it for removal.
                ((DeathSystem)Manager.GetSystem(DeathSystem.TypeId)).MarkForRemoval(damager);
            }

            // Apply damage to second entity if it has health, otherwise stop.
            // We do this after the removal, because sometimes the damagee may
            // be invulnerable/environmental (suns for example).
            if (Manager.GetComponent(damagee, Health.TypeId) == null)
            {
                return;
            }

            // See if the damagee blocks.
            if (!TryBlock(damagee, normal))
            {
                // Not blocked. Build message to send to trigger systems actually applying damage.
                DamageReceived message;

                // Figure out the root owner of the damager. We need to keep track of
                // this to allow us to eventually attribute kills properly.
                message.Owner = ((OwnerSystem)Manager.GetSystem(OwnerSystem.TypeId)).GetRootOwner(damager);

                // Apply damages, debuffs, ... get the damager attributes for actual values.
                message.Attributes = (Attributes<AttributeType>)Manager.GetComponent(damager, Attributes<AttributeType>.TypeId);

                // Pass on the entity that's being damaged.
                message.Damagee = damagee;

                // Aaaand send the message.
                Manager.SendMessage(message);
            }

            //var effect = Manager.AddComponent<DamagingStatusEffect>(damagee).Initialize(DamagingStatusEffect.InfiniteDamageDuration, damage.Damage, damage.Cooldown, damage.Type, damager);
            //_collisions.Add(BitwiseMagic.Pack(damagee, damager), effect.Id);
        }

        /// <summary>
        /// Checks if the damagee can and will block damage coming in from the specified direction.
        /// </summary>
        /// <param name="damagee">The damagee to check for.</param>
        /// <param name="normal">The normal from which the damage is coming.</param>
        /// <returns>
        ///   <c>true</c> if the damage was blocked; <c>false</c> otherwise.
        /// </returns>
        private bool TryBlock(int damagee, Vector2 normal)
        {
            var attributes = (Attributes<AttributeType>)Manager.GetComponent(damagee, Attributes<AttributeType>.TypeId);
            if (attributes == null)
            {
                // No attributes, so we can't block.
                return false;
            }

            // See if we even have a chance to block.
            var blockChance = attributes.GetValue(AttributeType.ShieldBlockChance);
            if (blockChance <= 0)
            {
                return false;
            }

            // Check if our shields are up.
            if (!((ShipControl)Manager.GetComponent(damagee, ShipControl.TypeId)).ShieldsActive)
            {
                // Shields are not active, so we cannot block.
                return false;
            }

            // Check if shields are oriented properly to intercept the damage.
            var rotation = (((Transform)Manager.GetComponent(damagee, Transform.TypeId)).Rotation + MathHelper.TwoPi) % MathHelper.TwoPi;
            var normalAngle = ((float)Math.Atan2(normal.Y, normal.X) + MathHelper.TwoPi) % MathHelper.TwoPi;
            var coverage = attributes.GetValue(AttributeType.ShieldCoverage) * MathHelper.Pi;
            if (Math.Abs(rotation - normalAngle) > coverage)
            {
                // Rotated the wrong way, damage hits where there is no shield coverage.
                return false;
            }

            // See if our block chance procs.
            if (_random.NextDouble() >= attributes.GetValue(AttributeType.ShieldBlockChance))
            {
                // Nope.
                return false;
            }

            // Damage is completely blocked! Send message to allow other systems to
            // react (particle effects, floating text, ...)
            DamageBlocked message;
            message.Entity = damagee;
            Manager.SendMessage(message);

            return true;
        }

        private void EndCollision(int damagee, int damager)
        {
            int effectId;
            if (_collisions.TryGetValue(BitwiseMagic.Pack(damagee, damager), out effectId))
            {
                Manager.RemoveComponent(effectId);
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>
        /// The copy.
        /// </returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (CollisionAttributeEffectSystem)base.NewInstance();

            copy._collisions = new Dictionary<ulong, int>();
            copy._random = new MersenneTwister(0);

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CollisionAttributeEffectSystem)into;

            copy._collisions.Clear();
            foreach (var collision in _collisions)
            {
                copy._collisions.Add(collision.Key, collision.Value);
            }
            _random.CopyInto(copy._random);
        }

        #endregion
    }
}
