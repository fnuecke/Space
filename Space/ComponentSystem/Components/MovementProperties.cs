using System;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

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

        public override Packet Packetize(Packet packet)
        {
            return packet
                .Write(Acceleration)
                .Write(RotationSpeed);
        }

        public override void Depacketize(Packet packet)
        {
            Acceleration = packet.ReadFixed();
            RotationSpeed = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Acceleration.RawValue));
            hasher.Put(BitConverter.GetBytes(RotationSpeed.RawValue));
        }

        #endregion
    }
}
