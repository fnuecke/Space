namespace Engine.Serialization
{
    /// <summary>
    /// Custom serialization using packets, with the intent of making
    /// it easier to send and receive data between network participants,
    /// or to store game data.
    /// </summary>
    public interface IPacketizable
    {
        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        Packet Packetize(Packet packet);

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// before automatic depacketization is performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        void PreDepacketize(Packet packet);

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        void PostDepacketize(Packet packet);
    }
}
