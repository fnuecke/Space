using Engine.Commands;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a new player spawn.
    /// </summary>
    class GameStateRequestCommand : Command<GameCommandType, PlayerInfo>
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        public GameStateRequestCommand()
            : base(GameCommandType.GameStateRequest)
        {
        }
    }
}
