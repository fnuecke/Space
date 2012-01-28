using Engine.Serialization;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Base class for item constraints.
    /// </summary>
    public abstract class ItemConstraints : IPacketizable
    {
        #region Fields
        
        /// <summary>
        /// Unique name for this item type.
        /// </summary>
        public string Name;

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet
                .Write(Name);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
        }

        #endregion
    }
}
