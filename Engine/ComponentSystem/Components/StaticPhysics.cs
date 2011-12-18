using System;
using Engine.ComponentSystem.Entities;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    public class StaticPhysics : AbstractComponent
    {
        /// <summary>
        /// Current position of the object.
        /// </summary>
        public FPoint Position { get; set; }

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public Fixed Rotation { get; set; }

        public StaticPhysics(IEntity entity)
            : base(entity)
        {
        }

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Position);
            packet.Write(Rotation);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Position = packet.ReadFPoint();
            Rotation = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Position.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Position.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Rotation.RawValue));
        }
    }
}
