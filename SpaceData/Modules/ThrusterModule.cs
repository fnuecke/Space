using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Modules
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

        #region Serialization / Hashing

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

        #region Copying

        public override AbstractEntityModule<EntityAttributeType> DeepCopy(AbstractEntityModule<EntityAttributeType> into)
        {
            var copy = (ThrusterModule)base.DeepCopy(into is ThrusterModule ? into : null);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.AccelerationForce = AccelerationForce;
                copy.EnergyConsumption = EnergyConsumption;
            }

            return copy;
        }

        #endregion
    }
}
