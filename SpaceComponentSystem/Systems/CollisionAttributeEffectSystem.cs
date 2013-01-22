using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Physics.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Handles applying effects defined through attributes when two entities collide.</summary>
    public sealed class CollisionAttributeEffectSystem : AbstractComponentSystem<DamagingStatusEffect>
    {
        #region Fields

        /// <summary>List of current collisions, mapping to their damage effect.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, int> _collisions = new Dictionary<ulong, int>();

        /// <summary>Randomizer used for determining whether certain effects should be applied (e.g. dots, blocking, ...).</summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<BeginContact>(OnBeginContact);
            Manager.AddMessageListener<EndContact>(OnEndContact);
        }

        private void OnBeginContact(BeginContact message)
        {
            // We only get one message for a collision pair, so we apply damage to both parties.
            var contact = message.Contact;
            Vector2 normal;
            IList<FarPosition> points;
            contact.ComputeWorldManifold(out normal, out points);
            var disableContact = BeginContact(contact.FixtureA.Entity, contact.FixtureB.Entity, normal);
            disableContact = BeginContact(contact.FixtureB.Entity, contact.FixtureA.Entity, -normal) || disableContact;
            if (disableContact)
            {
                contact.Disable();
            }
        }

        private void OnEndContact(EndContact message)
        {
            // Stop damage that is being applied because of this collision.
            var contact = message.Contact;
            EndContact(contact.FixtureA.Entity, contact.FixtureB.Entity);
            EndContact(contact.FixtureB.Entity, contact.FixtureA.Entity);
        }

        private bool BeginContact(int damagee, int damager, Vector2 normal)
        {
            // Our return value is used to tell whether to disable the contact or not. We want
            // to disable contacts involving bodies that are removed on collision, to avoid
            // knock-back from those.
            var disableContact = false;

            // Do we do any damage at all?
            var damage = (CollisionDamage) Manager.GetComponent(damager, CollisionDamage.TypeId);
            if (damage == null)
            {
                // Damager does not apply any damage, forget about it.
                return disableContact;
            }

            // Should the damager be removed when colliding (self-destructs)?
            if (damage.RemoveOnCollision)
            {
                // Yep, mark it for removal.
                ((DeathSystem) Manager.GetSystem(DeathSystem.TypeId)).MarkForRemoval(damager);
                disableContact = true;
            }

            // Apply damage to second entity if it has health, otherwise stop.
            // We do this after the removal, because sometimes the damagee may
            // be invulnerable/environmental (suns for example).
            if (Manager.GetComponent(damagee, Health.TypeId) == null)
            {
                return disableContact;
            }

            // See if the damagee blocks.
            if (!TryBlock(damagee, normal))
            {
                // Not blocked. Build message to send to trigger systems actually applying damage.
                DamageReceived message;

                // Figure out the root owner of the damager. We need to keep track of
                // this to allow us to eventually attribute kills properly.
                message.Owner = ((OwnerSystem) Manager.GetSystem(OwnerSystem.TypeId)).GetRootOwner(damager);

                // Apply damages, debuffs, ... get the damager attributes for actual values.
                message.Attributes = (Attributes<AttributeType>) Manager.GetComponent(damager, Attributes<AttributeType>.TypeId);

                // Pass on the entity that's being damaged.
                message.Damagee = damagee;

                // Aaaand send the message.
                Manager.SendMessage(message);
            }

            //var effect = Manager.AddComponent<DamagingStatusEffect>(damagee).Initialize(DamagingStatusEffect.InfiniteDamageDuration, damage.Damage, damage.Cooldown, damage.Type, damager);
            //_collisions.Add(BitwiseMagic.Pack(damagee, damager), effect.Id);

            return disableContact;
        }
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        /// <summary>Checks if the damagee can and will block damage coming in from the specified direction.</summary>
        /// <param name="damagee">The damagee to check for.</param>
        /// <param name="normal">The normal from which the damage is coming.</param>
        /// <returns>
        ///     <c>true</c> if the damage was blocked; <c>false</c> otherwise.
        /// </returns>
        private bool TryBlock(int damagee, Vector2 normal)
        {
            var attributes = (Attributes<AttributeType>) Manager.GetComponent(damagee, Attributes<AttributeType>.TypeId);
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
            if (!((ShipControl) Manager.GetComponent(damagee, ShipControl.TypeId)).ShieldsActive)
            {
                // Shields are not active, so we cannot block.
                return false;
            }

            // Check if shields are oriented properly to intercept the damage.
            var rotation = (((ITransform) Manager.GetComponent(damagee, TransformTypeId)).Angle + MathHelper.TwoPi) %
                           MathHelper.TwoPi;
            var normalAngle = ((float) Math.Atan2(normal.Y, normal.X) + MathHelper.TwoPi) % MathHelper.TwoPi;
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

        private void EndContact(int damagee, int damager)
        {
            int effectId;
            if (_collisions.TryGetValue(BitwiseMagic.Pack(damagee, damager), out effectId))
            {
                Manager.RemoveComponent(effectId);
            }
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (CollisionAttributeEffectSystem) base.NewInstance();

            copy._collisions = new Dictionary<ulong, int>();
            copy._random = new MersenneTwister(0);

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CollisionAttributeEffectSystem) into;

            copy._collisions.Clear();
            foreach (var collision in _collisions)
            {
                copy._collisions.Add(collision.Key, collision.Value);
            }
        }

        #endregion
    }
}