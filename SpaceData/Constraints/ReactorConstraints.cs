using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Constraints for generating reactors.
    /// </summary>
    public sealed class ReactorConstraints : ItemConstraints
    {
        #region Fields

        /// <summary>
        /// The minimum amount of energy storage this reactor class supplies.
        /// </summary>
        public float MinEnergy;

        /// <summary>
        /// The maximum amount of energy storage this reactor class supplies.
        /// </summary>
        public float MaxEnergy;

        /// <summary>
        /// The minimum amount of energy this reactor class produces per tick.
        /// </summary>
        public float MinEnergyRegeneration;

        /// <summary>
        /// The maximum amount of energy this reactor class produces per tick.
        /// </summary>
        public float MaxEnergyRegeneration;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the energy this reactor provides.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy.</returns>
        public float SampleEnergy(IUniformRandom random)
        {
            return MinEnergy + (float)random.NextDouble() * (MaxEnergy - MinEnergy);
        }

        /// <summary>
        /// Samples the energy regeneration this reactor provides.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy regeneration.</returns>
        public float SampleEnergyRegeneration(IUniformRandom random)
        {
            return MinEnergyRegeneration + (float)random.NextDouble() * (MaxEnergyRegeneration - MinEnergyRegeneration);
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
                .Write(MinEnergy)
                .Write(MaxEnergy)
                .Write(MinEnergyRegeneration)
                .Write(MaxEnergyRegeneration);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            MinEnergy = packet.ReadSingle();
            MaxEnergy = packet.ReadSingle();
            MinEnergyRegeneration = packet.ReadSingle();
            MaxEnergyRegeneration = packet.ReadSingle();
        }

        #endregion
    }
}
