using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public sealed class ProjectileData : ICloneable, IPacketizable, IHashable
    {
        /// <summary>
        /// The texture to use to render the projectile.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        public Fixed CollisionRadius;

        /// <summary>
        /// The damage this projectile inflicts.
        /// </summary>
        public Fixed Damage;

        /// <summary>
        /// The initial velocity of the projectile.
        /// </summary>
        public Fixed InitialVelocity;

        /// <summary>
        /// Acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Fixed AccelerationForce = (Fixed)0;

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Fixed Friction = (Fixed)0;

        /// <summary>
        /// The time this projectile will stay alive before disappearing.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int TimeToLive = 5 * 60; // ~5 seconds

        #region Serialization / Hashing / Cloning

        public Packet Packetize(Packet packet)
        {
            packet.Write(Texture)
                .Write(Damage)
                .Write(CollisionRadius)
                .Write(InitialVelocity)
                .Write(Friction)
                .Write(TimeToLive);

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            Texture = packet.ReadString();
            Damage = packet.ReadFixed();
            CollisionRadius = packet.ReadFixed();
            InitialVelocity = packet.ReadFixed();
            Friction = packet.ReadFixed();
            TimeToLive = packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(CollisionRadius.RawValue));
            hasher.Put(BitConverter.GetBytes(Damage.RawValue));
            hasher.Put(BitConverter.GetBytes(InitialVelocity.RawValue));
            hasher.Put(BitConverter.GetBytes(Friction.RawValue));
            hasher.Put(BitConverter.GetBytes(TimeToLive));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}
