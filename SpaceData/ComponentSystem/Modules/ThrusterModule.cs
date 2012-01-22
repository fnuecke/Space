using System;
using Engine.ComponentSystem.Modules;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public class ThrusterModule : AbstractModule<SpaceModifier>
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
            AddAttributeTypeToInvalidate(SpaceModifier.AccelerationForce);
            AddAttributeTypeToInvalidate(SpaceModifier.ThrusterEnergyConsumption);
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

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (ThrusterModule)base.DeepCopy(into);

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
