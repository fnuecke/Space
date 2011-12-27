using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public sealed class ProjectileData : IPacketizable, IHashable
    {
        /// <summary>
        /// The texture to use to render the projectile.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// The damage this projectile inflicts.
        /// </summary>
        public float Damage;

        /// <summary>
        /// The initial velocity of the projectile.
        /// </summary>
        public float InitialVelocity;

        /// <summary>
        /// Acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float AccelerationForce = 0;

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Friction = 0;

        /// <summary>
        /// The time this projectile will stay alive before disappearing.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int TimeToLive = 5 * 60; // ~5 seconds

        #region Serialization / Hashing

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
            Damage = packet.ReadSingle();
            CollisionRadius = packet.ReadSingle();
            InitialVelocity = packet.ReadSingle();
            Friction = packet.ReadSingle();
            TimeToLive = packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(CollisionRadius));
            hasher.Put(BitConverter.GetBytes(Damage));
            hasher.Put(BitConverter.GetBytes(InitialVelocity));
            hasher.Put(BitConverter.GetBytes(AccelerationForce));
            hasher.Put(BitConverter.GetBytes(Friction));
            hasher.Put(BitConverter.GetBytes(TimeToLive));
        }

        #endregion
    }
}
