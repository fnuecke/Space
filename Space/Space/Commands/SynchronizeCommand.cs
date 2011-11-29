using Engine.Commands;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by the server to send current frame to clients, used to synchronize
    /// run speeds of clients to server.
    /// </summary>
    class SynchronizeCommand : Command<GameCommandType, PlayerInfo>
    {
        /// <summary>
        /// The frame to synchronize to.
        /// </summary>
        public ulong Frame { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public SynchronizeCommand()
            : base(GameCommandType.Synchronize)
        {
        }

        /// <summary>
        /// Construct new synchronize command, telling players to synchronize
        /// to the server, by telling them the servers current frame.
        /// </summary>
        /// <param name="frame">the current leading frame on the server.</param>
        public SynchronizeCommand(ulong frame)
            : base(GameCommandType.Synchronize)
        {
            this.Frame = frame;
        }

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write(Frame);

            base.Packetize(packet);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            Frame = packet.ReadUInt64();

            base.Depacketize(packet);
        }
    }
}
