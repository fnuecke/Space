using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Constraints for generating sensors.
    /// </summary>
    public sealed class SensorConstraints : ItemConstraints
    {
        #region Fields

        /// <summary>
        /// The minimum range this sensor has.
        /// </summary>
        public float MinRange;

        /// <summary>
        /// The maximum range this sensor has.
        /// </summary>
        public float MaxRange;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the range this sensor provides.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sensor range.</returns>
        public float SampleRange(IUniformRandom random)
        {
            return MinRange + (float)random.NextDouble() * (MaxRange - MinRange);
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
                .Write(MinRange)
                .Write(MaxRange);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            MinRange = packet.ReadSingle();
            MaxRange = packet.ReadSingle();
        }

        #endregion
    }
}
