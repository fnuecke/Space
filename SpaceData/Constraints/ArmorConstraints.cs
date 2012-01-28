using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Constraints for generating armor.
    /// </summary>
    public sealed class ArmorConstraints : ItemConstraints
    {
        #region Fields
        
        /// <summary>
        /// The minimum armor rating this armor provides.
        /// </summary>
        public float MinArmor;

        /// <summary>
        /// The maximum armor rating this armor provides.
        /// </summary>
        public float MaxArmor;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the armor rating this armor provides.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled armor rating.</returns>
        public float SampleArmor(IUniformRandom random)
        {
            return MinArmor + (float)random.NextDouble() * (MaxArmor - MinArmor);
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
                .Write(MinArmor)
                .Write(MaxArmor);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            MinArmor = packet.ReadSingle();
            MaxArmor = packet.ReadSingle();
        }

        #endregion
    }
}
