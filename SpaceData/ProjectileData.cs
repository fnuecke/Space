using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public sealed class ProjectileData : IPacketizable, IHashable
    {
        /// <summary>
        /// The texture to use to render the projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Texture = string.Empty;

        /// <summary>
        /// Name of the particle effect to use.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Effect = string.Empty;

        /// <summary>
        /// The scale at which to render the texture and effects.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Scale = 1;

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// The damage this projectile inflicts.
        /// </summary>
        public float Damage;

        /// <summary>
        /// Whether this projectile can be hit by other projectiles (e.g.
        /// missiles may be shot down, but normal projectiles should not
        /// interact).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public bool CanBeShot = false;

        /// <summary>
        /// The initial directed velocity of the projectile. This is rotated
        /// according to the emitters rotation. The set value applies directly
        /// if the emitter is facing to the right (i.e. is at zero rotation).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 InitialVelocity = Vector2.Zero;

        /// <summary>
        /// Initial orientation of the projectile. As with the initial
        /// velocity, this is rotated by the emitters rotation, and the
        /// rotation applies directly if the emitter is facing to the right,
        /// i.e. its own rotation is zero.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float InitialRotation = 0;

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
        /// The spin of the projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Spin = 0;

        /// <summary>
        /// The time this projectile will stay alive before disappearing.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int TimeToLive = 5 * 60; // ~5 seconds

        #region Serialization / Hashing

        public Packet Packetize(Packet packet)
        {
            packet.Write(Texture)
                .Write(Effect)
                .Write(Scale)
                .Write(CollisionRadius)
                .Write(Damage)
                .Write(InitialVelocity)
                .Write(InitialRotation)
                .Write(AccelerationForce)
                .Write(Friction)
                .Write(Spin)
                .Write(TimeToLive);

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            Texture = packet.ReadString();
            Effect = packet.ReadString();
            Scale = packet.ReadSingle();
            CollisionRadius = packet.ReadSingle();
            Damage = packet.ReadSingle();
            InitialVelocity = packet.ReadVector2();
            InitialRotation = packet.ReadSingle();
            AccelerationForce = packet.ReadSingle();
            Friction = packet.ReadSingle();
            Spin = packet.ReadSingle();
            TimeToLive = packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(CollisionRadius));
            hasher.Put(BitConverter.GetBytes(Damage));
            hasher.Put(BitConverter.GetBytes(InitialVelocity.X));
            hasher.Put(BitConverter.GetBytes(InitialVelocity.Y));
            hasher.Put(BitConverter.GetBytes(InitialRotation));
            hasher.Put(BitConverter.GetBytes(AccelerationForce));
            hasher.Put(BitConverter.GetBytes(Friction));
            hasher.Put(BitConverter.GetBytes(Spin));
            hasher.Put(BitConverter.GetBytes(TimeToLive));
        }

        #endregion
    }
}
