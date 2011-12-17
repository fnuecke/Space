using System;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    public class PhysicsComponent : IComponent
    {
        /// <summary>
        /// Current position of the object.
        /// </summary>
        public FPoint Position { get; set; }

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public Fixed Rotation { get; set; }

        /// <summary>
        /// The directed speed of the object.
        /// </summary>
        public FPoint Velocity { get; set; }

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public Fixed Spin { get; set; }

        /// <summary>
        /// The directed acceleration of the object.
        /// </summary>
        public FPoint Acceleration { get; set; }

        public PhysicsComponent()
        {
        }

        public void Update(object parameterization)
        {
            Rotation += Spin;
            if (Rotation < -Fixed.PI)
            {
                Rotation += Fixed.PI * 2;
            }
            else if (Rotation > Fixed.PI)
            {
                Rotation -= Fixed.PI * 2;
            }
            //previousPosition = position;
            Velocity += Acceleration;
            Position += Velocity;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Packetize(Serialization.Packet packet)
        {
            packet.Write(Position);
            packet.Write(Rotation);
            packet.Write(Velocity);
            packet.Write(Spin);
            packet.Write(Acceleration);
        }

        public void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            Position = packet.ReadFPoint();
            Rotation = packet.ReadFixed();
            Velocity = packet.ReadFPoint();
            Spin = packet.ReadFixed();
            Acceleration = packet.ReadFPoint();
        }

        public void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Position.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Position.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Rotation.RawValue));
            hasher.Put(BitConverter.GetBytes(Velocity.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Velocity.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Spin.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.Y.RawValue));
        }
    }
}
