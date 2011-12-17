using System;
using Engine.ComponentSystem.Components;
using Engine.Math;

namespace Space.ComponentSystem.Components
{
    public class MovementProperties : AbstractComponent
    {
        public Fixed Acceleration { get; set; }

        public Fixed RotationSpeed { get; set; }

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write(Acceleration);
            packet.Write(RotationSpeed);
        }

        public override void Depacketize(Engine.Serialization.Packet packet, Engine.Serialization.IPacketizerContext context)
        {
            Acceleration = packet.ReadFixed();
            RotationSpeed = packet.ReadFixed();
        }

        public override void Hash(Engine.Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Acceleration.RawValue));
            hasher.Put(BitConverter.GetBytes(RotationSpeed.RawValue));
        }
    }
}
