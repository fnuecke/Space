﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
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
    public sealed class CollisionDamage : Component
    {
        #region Fields

        /// <summary>
        /// Determines how many frames (updates) to wait between dealing our
        /// damage. This is per other entity we collide with.
        /// 
        /// <para>
        /// A special case is zero, which means we only do our damage once,
        /// then die.
        /// </para>
        /// </summary>
        public int Cooldown;

        /// <summary>
        /// The amount of damage to deal upon collision.
        /// </summary>
        public float Damage;

        /// <summary>
        /// Cooldown before doing damage to an entity (maps entity id to cd).
        /// </summary>
        internal Dictionary<int, int> Cooldowns = new Dictionary<int, int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            var otherCollisionDamage = (CollisionDamage)other;
            Cooldown = otherCollisionDamage.Cooldown;
            Damage = otherCollisionDamage.Damage;
            foreach (var item in otherCollisionDamage.Cooldowns)
            {
                Cooldowns.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="cooldown">The cooldown.</param>
        /// <param name="damage">The damage.</param>
        public void Initialize(int cooldown, float damage)
        {
            this.Cooldown = cooldown;
            this.Damage = damage;
        }

        /// <summary>
        /// Initialize with the specified damage.
        /// </summary>
        /// <param name="damage">The damage.</param>
        public void Initialize(float damage)
        {
            Initialize(0, damage);
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Cooldown = 0;
            Damage = 0;
            Cooldowns.Clear();
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Cooldown);
            packet.Write(Damage);
            if (Cooldowns == null)
            {
                packet.Write(0);
            }
            else
            {
                packet.Write(Cooldowns.Count);
                foreach (var item in Cooldowns)
                {
                    packet.Write(item.Key);
                    packet.Write(item.Value);
                }
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Cooldown = packet.ReadInt32();
            Damage = packet.ReadSingle();

            int numCooldowns = packet.ReadInt32();
            if (numCooldowns > 0)
            {
                if (Cooldowns != null)
                {
                    Cooldowns.Clear();
                }
                else
                {
                    Cooldowns = new Dictionary<int, int>();
                }
                for (int i = 0; i < numCooldowns; i++)
                {
                    var key = packet.ReadInt32();
                    var value = packet.ReadInt32();
                    Cooldowns.Add(key, value);
                }
            }
            else
            {
                Cooldowns = null;
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Cooldown));
            hasher.Put(BitConverter.GetBytes(Damage));
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Cooldown = " + Cooldown + ", Damage = " + Damage;
        }

        #endregion
    }
}
