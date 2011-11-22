using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Client indicates he needs the current game state due to desync.
    /// </summary>
    public sealed class GameStateQueryCommand : Command
    {

        /// <summary>
        /// Creates a new command representing a query for open games.
        /// </summary>
        public GameStateQueryCommand()
            : base((uint)CommandType.GameStateQuery)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        /// <param name="packet">the packet to read data from.</param>
        public GameStateQueryCommand(Packet packet)
            : base(packet)
        {
        }

        public override string ToString()
        {
            return "GameStateQuery()";
        }

    }
}
