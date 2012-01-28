using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public sealed class Thruster : Item
    {
        #region Fields
        
        /// <summary>
        /// The acceleration force this thruster provides.
        /// </summary>
        public float AccelerationForce;

        /// <summary>
        /// The rotation force this thruster provides.
        /// </summary>
        public float RotationForce;

        /// <summary>
        /// The energy this thruster consumes per tick, while active.
        /// </summary>
        public float EnergyConsumption;
        
        #endregion

        #region Constructor

        public Thruster(float accelerationForce, float rotationForce, float energyConsumption)
        {
            this.AccelerationForce = accelerationForce;
            this.RotationForce = rotationForce;
            this.EnergyConsumption = energyConsumption;
        }

        public Thruster()
        {
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(AccelerationForce)
                .Write(RotationForce)
                .Write(EnergyConsumption);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            AccelerationForce = packet.ReadSingle();
            RotationForce = packet.ReadSingle();
            EnergyConsumption = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(AccelerationForce));
            hasher.Put(BitConverter.GetBytes(RotationForce));
            hasher.Put(BitConverter.GetBytes(EnergyConsumption));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Thruster)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.AccelerationForce = AccelerationForce;
                copy.RotationForce = RotationForce;
                copy.EnergyConsumption = EnergyConsumption;
            }

            return copy;
        }

        #endregion
    }
}
