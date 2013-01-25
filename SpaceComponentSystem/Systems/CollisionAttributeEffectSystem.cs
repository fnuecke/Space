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
    public sealed class CollisionAttributeEffectSystem : AbstractComponentSystem<DamagingStatusEffect>, IUpdatingSystem
    {
        #region Fields

        /// <summary>Tracks new collisions that occurred since the last update.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, BeginCollisionInfo> _newCollision = new Dictionary<ulong, BeginCollisionInfo>();

        /// <summary>List of current collisions.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, ActiveCollisionInfo> _activeCollisions = new Dictionary<ulong, ActiveCollisionInfo>();

        /// <summary>Randomizer used for determining whether certain effects should be applied (e.g. dots, blocking, ...).</summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic
        
        public void Update(long frame)
        {
            foreach (var info in _newCollision)
            {
                // Check if we already have contact between the two entities.
                ActiveCollisionInfo active;
                if (_activeCollisions.TryGetValue(info.Key, out active))
                {
                    // Yes, just update the number of fixture contacts.
                    active.Count += info.Value.Count;
                }
                else
                {
                    // Nothing yet, save the contact and apply effects.
                    if (info.Value.Count > 0)
                    {
                        active = new ActiveCollisionInfo {Count = info.Value.Count};
                        _activeCollisions.Add(info.Key, active);
                    }

                    int entityA, entityB;
                    BitwiseMagic.Unpack(info.Key, out entityA, out entityB);

                    OnEntityContact(entityA, entityB, info.Value.IsShieldA, info.Value.Normal);
                    OnEntityContact(entityB, entityA, info.Value.IsShieldB, -info.Value.Normal);
                }
            }
            _newCollision.Clear();
        }

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<BeginContact>(OnBeginContact);
            Manager.AddMessageListener<EndContact>(OnEndContact);
        }

        private void OnBeginContact(BeginContact message)
        {
            // We only get one message for a collision pair, so we handle it for both parties.
            var contact = message.Contact;

            // Get the two involved entities. We only handle one collision between
            // two bodies per tick, to avoid damage being applied multiple times.
            var entityA = Math.Min(contact.FixtureA.Entity, contact.FixtureB.Entity);
            var entityB = Math.Max(contact.FixtureA.Entity, contact.FixtureB.Entity);
            
            // See if either one does some damage and/or should be removed.
            var damageA = (CollisionDamage) Manager.GetComponent(entityA, CollisionDamage.TypeId);
            var damageB = (CollisionDamage) Manager.GetComponent(entityB, CollisionDamage.TypeId);

            var healthA = (Health) Manager.GetComponent(entityA, Health.TypeId);
            var healthB = (Health) Manager.GetComponent(entityB, Health.TypeId);

            // Ignore contacts where no damage is involved at all.
            if ((damageA == null || healthB == null) &&
                (damageB == null || healthA == null))
            {
                return;
            }

            // See if the hit entity should be removed on collision.
            var persistent = true;
            if (damageA != null && damageA.RemoveOnCollision)
            {
                // Entity gets removed, so forget about handling the collision physically.
                ((DeathSystem) Manager.GetSystem(DeathSystem.TypeId)).MarkForRemoval(entityA);
                contact.Disable();
                persistent = false;
            }
            if (damageB != null && damageB.RemoveOnCollision)
            {
                // Entity gets removed, so forget about handling the collision physically.
                ((DeathSystem) Manager.GetSystem(DeathSystem.TypeId)).MarkForRemoval(entityB);
                contact.Disable();
                persistent = false;
            }

            // See if we already know something about this collision.
            var key = BitwiseMagic.Pack(entityA, entityB);
            BeginCollisionInfo info;
            if (!_newCollision.TryGetValue(key, out info))
            {
                info = new BeginCollisionInfo();
                _newCollision.Add(key, info);
            }

            // Track the number of persistent contacts. This is necessary for damage "fields",
            // such as radiation in a certain area.
            if (persistent)
            {
                ++info.Count;
            }

            // See if the shield was hit.
            var shielded = false;
            var shieldA = Manager.GetComponent(entityA, ShieldEnergyStatusEffect.TypeId) as ShieldEnergyStatusEffect;
            if (shieldA != null && contact.FixtureA.Id == shieldA.Fixture)
            {
                info.IsShieldA = true;
                shielded = true;
            }
            var shieldB = Manager.GetComponent(entityB, ShieldEnergyStatusEffect.TypeId) as ShieldEnergyStatusEffect;
            if (shieldB != null && contact.FixtureA.Id == shieldB.Fixture)
            {
                info.IsShieldB = true;
                shielded = true;
            }

            // For at least one of the involved entities a shield was hit, so get the normal.
            // Note that this can potentially lead to 'wrong' results, if an entity is hit by
            // multiple fixtures in the same frame. This is a problematic case, logically speaking:
            // what to do if this happens?
            if (shielded)
            {
                IList<FarPosition> points;
                contact.ComputeWorldManifold(out info.Normal, out points);
            }
        }

        private void OnEndContact(EndContact message)
        {
            // Stop damage that is being applied because of this collision.
            var contact = message.Contact;

            // Get the two related entities.
            var entityA = Math.Min(contact.FixtureA.Entity, contact.FixtureB.Entity);
            var entityB = Math.Max(contact.FixtureB.Entity, contact.FixtureB.Entity);

            // See if we're tracking such a collision.
            var key = BitwiseMagic.Pack(entityA, entityB);
            ActiveCollisionInfo active;
            if (_activeCollisions.TryGetValue(key, out active))
            {
                // One less fixture contact.
                --active.Count;
                if (active.Count == 0)
                {
                    // That was the last one, stop tracking this and remove any "during contact" effects.
                    _activeCollisions.Remove(key);

                    // TODO figure out a nice way to let contact based debuffs know they can stop (radiation, ...)
                    //int effectId;
                    //if (_collisions.TryGetValue(BitwiseMagic.Pack(damagee, damager), out effectId))
                    //{
                    //    Manager.RemoveComponent(effectId);
                    //}
                }
            }
        }

        private void OnEntityContact(int damagee, int damager, bool shieldHit, Vector2 normal)
        {
            // Do we do any damage at all?
            if (Manager.GetComponent(damager, CollisionDamage.TypeId) == null)
            {
                return;
            }

            // Otherwise we should not have accepted this contact!
            System.Diagnostics.Debug.Assert(Manager.GetComponent(damagee, Health.TypeId) != null);

            // See if the damagee blocks.
            if (!shieldHit || !TryBlock(damagee, normal))
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

            // Otherwise we should not have flagged this contact as shielded!
            System.Diagnostics.Debug.Assert(((ShipControl) Manager.GetComponent(damagee, ShipControl.TypeId)).ShieldsActive);

            // Check if shields are oriented properly to intercept the damage.
            var angle = MathHelper.WrapAngle(((ITransform) Manager.GetComponent(damagee, TransformTypeId)).Angle);
            var normalAngle = (float) Math.Atan2(normal.Y, normal.X);
            var coverage = attributes.GetValue(AttributeType.ShieldCoverage) * MathHelper.Pi;
            if (Math.Abs(Angle.MinAngle(angle, normalAngle)) > coverage)
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

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (CollisionAttributeEffectSystem) base.NewInstance();

            copy._newCollision = new Dictionary<ulong, BeginCollisionInfo>();
            copy._activeCollisions = new Dictionary<ulong, ActiveCollisionInfo>();
            copy._random = new MersenneTwister(0);

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para/>
        ///     This clones any contained data types to return an instance that represents a complete copy of the one passed in.
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CollisionAttributeEffectSystem) into;

            copy._newCollision.Clear();
            foreach (var info in _newCollision)
            {
                copy._newCollision.Add(
                    info.Key,
                    new BeginCollisionInfo
                    {
                        Count = info.Value.Count,
                        IsShieldA = info.Value.IsShieldA,
                        IsShieldB = info.Value.IsShieldB,
                        Normal = info.Value.Normal
                    });
            }
            copy._activeCollisions.Clear();
            foreach (var collision in _activeCollisions)
            {
                copy._activeCollisions.Add(
                    collision.Key,
                    new ActiveCollisionInfo
                    {
                        Count = collision.Value.Count
                    });
            }
        }

        #endregion

        #region Serialization

        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_newCollision.Count);
            foreach (var info in _newCollision)
            {
                packet.Write(info.Key);
                packet.Write(info.Value);
            }

            packet.Write(_activeCollisions.Count);
            foreach (var info in _activeCollisions)
            {
                packet.Write(info.Key);
                packet.Write(info.Value);
            }

            return packet;
        }

        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _newCollision.Clear();
            var newCollisionCount = packet.ReadInt32();
            for (var i = 0; i < newCollisionCount; ++i)
            {
                var key = packet.ReadUInt64();
                var value = packet.ReadPacketizable<BeginCollisionInfo>();
                _newCollision.Add(key, value);
            }

            _activeCollisions.Clear();
            var activeCollisionCount = packet.ReadInt32();
            for (var i = 0; i < activeCollisionCount; ++i)
            {
                var key = packet.ReadUInt64();
                var value = packet.ReadPacketizable<ActiveCollisionInfo>();
                _activeCollisions.Add(key, value);
            }
        }

        #endregion

        #region Types

        private sealed class BeginCollisionInfo : IPacketizable
        {
            /// <summary>Number of fixture collisions between the two entities.</summary>
            public int Count;

            /// <summary>Whether the first entity's shield was hit, potentially blocking damage.</summary>
            public bool IsShieldA;

            /// <summary>Whether the second entity's shield was hit, potentially blocking damage.</summary>
            public bool IsShieldB;

            /// <summary>The surface normal at the intersection, directed from the first towards the second entity.</summary>
            /// <remarks>This is only set if a shield was hit.</remarks>
            public Vector2 Normal;
        }

        private sealed class ActiveCollisionInfo : IPacketizable
        {
            /// <summary>The number of active fixture collisions. The collision becomes inactive when this reaches zero.</summary>
            public int Count;
        }

        #endregion
    }
}