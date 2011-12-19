using System;
using Engine.ComponentSystem.Components;
using Engine.Math;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Contains some movement restraint information.
    /// </summary>
    public class MovementProperties : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The maximum acceleration an entity with this component can have.
        /// </summary>
        public Fixed Acceleration { get; set; }

        /// <summary>
        /// The maximum rotation speed an entity with this component can have.
        /// </summary>
        public Fixed RotationSpeed { get; set; }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write(Acceleration);
            packet.Write(RotationSpeed);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            Acceleration = packet.ReadFixed();
            RotationSpeed = packet.ReadFixed();
        }

        public override void Hash(Engine.Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Acceleration.RawValue));
            hasher.Put(BitConverter.GetBytes(RotationSpeed.RawValue));
        }

        #endregion
    }
}
