using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Sends a serialized game state (complete simulation state!) to a client.
    /// </summary>
    public sealed class GameStateCommand : Command
    {

        /// <summary>
        /// Serialized representation of the game state.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Creates a new command representing a query for open games.
        /// </summary>
        public GameStateCommand(byte[] gameState)
            : base((uint)CommandType.GameState)
        {
            Data = gameState;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        /// <param name="packet">the packet to read data from.</param>
        public GameStateCommand(Packet packet)
            : base(packet)
        {
            Data = packet.ReadByteArray();
        }

        public override void Write(Packet packet)
        {
            base.Write(packet);
            packet.Write(Data);
        }

        public override string ToString()
        {
            return "GameState(" + Data.Length + ")";
        }

    }
}
