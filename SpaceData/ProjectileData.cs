using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public sealed class ProjectileData : ICloneable, IPacketizable, IHashable
    {
        public string Texture;

        public int Damage;

        public Fixed CollisionRadius;

        public Fixed InitialVelocity;

        [ContentSerializer(Optional = true)]
        public Fixed Friction = (Fixed)0;

        [ContentSerializer(Optional = true)]
        public int TimeToLive = 5 * 60; // ~5 seconds

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
            Damage = packet.ReadInt32();
            CollisionRadius = packet.ReadFixed();
            InitialVelocity = packet.ReadFixed();
            Friction = packet.ReadFixed();
            TimeToLive = packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {
            
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
