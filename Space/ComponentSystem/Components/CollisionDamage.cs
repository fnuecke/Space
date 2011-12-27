using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component can be used to apply damage to another entity upon
    /// collision with the entity this component belongs to.
    /// 
    /// <para>
    /// Used for making bullets damage ships, and causing ships to damage each
    /// other. In the latter case, the damage is applied with a certain
    /// frequency (controlled via the <c>Cooldown</c>).
    /// </para>
    /// </summary>
    public sealed class CollisionDamage : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// Determines how many frames (updates) to wait between dealing our
        /// damage. This is per other entity we collide with.
        /// 
        /// <para>
        /// A special case is zero, which means we only do our damage once,
        /// then die.
        /// </para>
        /// </summary>
        public int Cooldown { get; set; }

        /// <summary>
        /// The amount of damage to deal upon collision.
        /// </summary>
        public float Damage { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Cooldown before doing damage to an entity (maps entity id to cd).
        /// </summary>
        /// <remarks>
        /// Kept <c>null</c> as long as possible, because the most frequent use
        /// of this class will be projectiles, which don't need this.
        /// </remarks>
        private Dictionary<int, int> _cooldowns;

        #endregion

        #region Logic

        /// <summary>
        /// Reduces damage cooldowns, if there are any..
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            if (_cooldowns != null)
            {
                // Decrement, remove if run out.
                foreach (var entityId in new List<int>(_cooldowns.Keys))
                {
                    if (--_cooldowns[entityId] <= 0)
                    {
                        _cooldowns.Remove(entityId);
                    }
                }
            }
        }

        /// <summary>
        /// Supports <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        /// <summary>
        /// Handles collision messages to apply damage.
        /// </summary>
        /// <param name="message"></param>
        public override void HandleMessage(ValueType message)
        {
            // Only handle collisions, and only if we weren't removed yet.
            // This can happen if we collide with two things in one frame
            // but are a oneshot.
            if (message is Collision && Entity.Manager != null)
            {
                var entity = ((Collision)message).OtherEntity;

                // On cooldown?
                if (_cooldowns != null && _cooldowns.ContainsKey(entity.UID))
                {
                    // Yes.
                    return;
                }

                // Apply damage if we can.
                var health = entity.GetComponent<Health>();
                if (health != null)
                {
                    health.Value -= Damage;
                }

                // Oneshot?
                if (Cooldown == 0)
                {
                    Entity.Manager.RemoveEntity(Entity);
                }
                // No, keep cooldown for this one - if it had any health.
                else if (health != null)
                {
                    if (_cooldowns == null)
                    {
                        _cooldowns = new Dictionary<int, int>();
                    }
                    _cooldowns.Add(entity.UID, Cooldown);
                }
            }
        }

        #endregion

        #region Serialization / Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Cooldown);
            packet.Write(Damage);
            if (_cooldowns == null)
            {
                packet.Write((int)0);
            }
            else
            {
                packet.Write(_cooldowns.Count);
                foreach (var item in _cooldowns)
                {
                    packet.Write(item.Key);
                    packet.Write(item.Value);
                }
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Cooldown = packet.ReadInt32();
            Damage = packet.ReadSingle();

            int numCooldowns = packet.ReadInt32();
            if (numCooldowns > 0)
            {
                if (_cooldowns != null)
                {
                    _cooldowns.Clear();
                }
                else
                {
                    _cooldowns = new Dictionary<int, int>();
                }
                for (int i = 0; i < numCooldowns; i++)
                {
                    var key = packet.ReadInt32();
                    var value = packet.ReadInt32();
                    _cooldowns.Add(key, value);
                }
            }
            else
            {
                _cooldowns = null;
            }
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Cooldown));
            hasher.Put(BitConverter.GetBytes(Damage));
        }

        public override object Clone()
        {
            var copy = (CollisionDamage)base.Clone();

            if (_cooldowns != null)
            {
                copy._cooldowns = new Dictionary<int, int>(_cooldowns);
            }

            return copy;
        }

        #endregion
    }
}
