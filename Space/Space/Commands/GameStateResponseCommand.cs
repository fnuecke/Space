using Engine.Commands;
using Engine.Serialization;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a new player spawn.
    /// </summary>
    class GameStateResponseCommand : Command<GameCommandType, PlayerInfo>
    {
        #region Properties
        
        /// <summary>
        /// The complete, serialized TSS on the server.
        /// </summary>
        public Packet GameState { get; private set; }

        #endregion

        #region Constructor
        
        /// <summary>
        /// For deserialization.
        /// </summary>
        public GameStateResponseCommand()
            : base(GameCommandType.GameStateResponse)
        {
            this.GameState = new Packet();
        }

        /// <summary>
        /// Creates a new response by serializing the given gamestate.
        /// </summary>
        /// <param name="gameState">the game state to serialize.</param>
        public GameStateResponseCommand(IPacketizable gameState)
            : base(GameCommandType.GameStateResponse)
        {
            this.GameState = new Packet();
            gameState.Packetize(this.GameState);
        }

        #endregion

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(GameState);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            GameState = packet.ReadPacket();

            base.Depacketize(packet);
        }

        #endregion
    }
}
