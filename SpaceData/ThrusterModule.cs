using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public class ThrusterModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Fields
        
        /// <summary>
        /// The acceleration force this thruster provides.
        /// </summary>
        public float AccelerationForce;

        /// <summary>
        /// The energy this thruster consumes per tick, while active.
        /// </summary>
        public float EnergyConsumption;
        
        #endregion

        #region Constructor

        public ThrusterModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.AccelerationForce);
            AddAttributeTypeToInvalidate(EntityAttributeType.ThrusterEnergyConsumption);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(AccelerationForce)
                .Write(EnergyConsumption);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            AccelerationForce = packet.ReadSingle();
            EnergyConsumption = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(AccelerationForce));
            hasher.Put(BitConverter.GetBytes(EnergyConsumption));
        }

        #endregion
    }
}
