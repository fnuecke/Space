using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Constraints for generating thrusters.
    /// </summary>
    public sealed class ThrusterConstraints : ItemConstraints
    {
        #region Fields

        /// <summary>
        /// The minimum acceleration force this thruster class provides.
        /// </summary>
        public float MinAccelerationForce;

        /// <summary>
        /// The maximum acceleration force this thruster class provides.
        /// </summary>
        public float MaxAccelerationForce;

        /// <summary>
        /// The minimum rotation force this thruster class provides.
        /// </summary>
        public float MinRotationForce;

        /// <summary>
        /// The maximum rotation force this thruster class provides.
        /// </summary>
        public float MaxRotationForce;

        /// <summary>
        /// The minimum energy this thruster class consumes per tick, while active.
        /// </summary>
        public float MinEnergyConsumption;

        /// <summary>
        /// The maximum energy this thruster class consumes per tick, while active.
        /// </summary>
        public float MaxEnergyConsumption;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the acceleration force provided by this thruster.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled acceleration force.</returns>
        public float SampleAccelerationForce(IUniformRandom random)
        {
            return MinAccelerationForce + (float)random.NextDouble() * (MaxAccelerationForce - MinAccelerationForce);
        }

        /// <summary>
        /// Samples the rotation force provided by this thruster.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled rotation force.</returns>
        public float SampleRotationForce(IUniformRandom random)
        {
            return MinRotationForce + (float)random.NextDouble() * (MaxRotationForce - MinRotationForce);
        }

        /// <summary>
        /// Samples the energy consumption provided by this thruster.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy consumption.</returns>
        public float SampleEnergyConsumption(IUniformRandom random)
        {
            return MinEnergyConsumption + (float)random.NextDouble() * (MaxEnergyConsumption - MinEnergyConsumption);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(MinAccelerationForce)
                .Write(MaxAccelerationForce)
                .Write(MinRotationForce)
                .Write(MaxRotationForce)
                .Write(MinEnergyConsumption)
                .Write(MaxEnergyConsumption);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            MinAccelerationForce = packet.ReadSingle();
            MaxAccelerationForce = packet.ReadSingle();
            MinRotationForce = packet.ReadSingle();
            MaxRotationForce = packet.ReadSingle();
            MinEnergyConsumption = packet.ReadSingle();
            MaxEnergyConsumption = packet.ReadSingle();
        }

        #endregion
    }
}
