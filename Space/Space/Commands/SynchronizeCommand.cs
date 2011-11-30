using Engine.Commands;
using Engine.Serialization;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used to synchronize game clocks (leading simulation frames).
    /// </summary>
    /// <seealso cref="http://www.mine-control.com/zack/timesync/timesync.html"/>
    class SynchronizeCommand : Command<GameCommandType, PlayerInfo>
    {
        #region Properties
        
        /// <summary>
        /// The frame to synchronize to.
        /// </summary>
        public long ClientFrame { get; private set; }

        /// <summary>
        /// The frame to synchronize to.
        /// </summary>
        public long ServerFrame { get; private set; }

        #endregion

        #region Constructor

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
        /// <param name="clientFrame">the current frame on the client when the sync was initialized.</param>
        /// <param name="serverFrame">the current frame on the server when it responded.</param>
        public SynchronizeCommand(long clientFrame, long serverFrame = 0)
            : base(GameCommandType.Synchronize)
        {
            this.ClientFrame = clientFrame;
            this.ServerFrame = serverFrame;
        }

        #endregion

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(ClientFrame);
            packet.Write(ServerFrame);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            ClientFrame = packet.ReadInt64();
            ServerFrame = packet.ReadInt64();

            base.Depacketize(packet);
        }

        #endregion
    }
}
