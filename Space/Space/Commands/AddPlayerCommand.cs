using Engine.Session;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a new player spawn.
    /// </summary>
    class AddPlayerCommand : GameCommand
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        public AddPlayerCommand()
            : base(GameCommandType.AddPlayerShip)
        {
        }

        /// <summary>
        /// Construct new player add command, spawning a ship for the given player
        /// at the given time.
        /// </summary>
        /// <param name="player">the player for whom to spawn a ship.</param>
        /// <param name="frame">the frame in which to spawn the ship.</param>
        public AddPlayerCommand(Player<PlayerInfo> player, long frame)
            : base(GameCommandType.AddPlayerShip, player, frame)
        {
        }
    }
}
