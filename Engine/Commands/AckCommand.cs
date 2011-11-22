using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// This command signals the sender successfully received a package.
    /// </summary>
    sealed class AckCommand : Command
    {

        /// <summary>
        /// The packet number acknowledged via this ack.
        /// </summary>
        public long PacketNumber { get; private set; }

        /// <summary>
        /// Creates a new ack command, acknowledging the given packet.
        /// </summary>
        /// <param name="packetNumber">the number of the acknowledged packet.</param>
        public AckCommand(long packetNumber)
            : base((uint)InternalCommandType.Ack)
        {
            this.PacketNumber = packetNumber;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        /// <param name="packet">the packet to read data from.</param>
        public AckCommand(Packet packet)
            : base(packet)
        {
            PacketNumber = packet.ReadInt64();
        }

        public override void Write(Packet packet)
        {
            base.Write(packet);
            packet.Write(PacketNumber);
        }
        
        public override string ToString()
        {
            return "Ack(" + PacketNumber + ")";
        }

    }
}
