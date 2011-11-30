using Engine.Session;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a player despawn.
    /// </summary>
    class RemovePlayerCommand : GameCommand
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        public RemovePlayerCommand()
            : base(GameCommandType.RemovePlayerShip)
        {
        }

        /// <summary>
        /// Construct new player removal command, removing a ship of the given player
        /// at the given time.
        /// </summary>
        /// <param name="player">the player for whom to spawn a ship.</param>
        /// <param name="frame">the frame in which to spawn the ship.</param>
        public RemovePlayerCommand(Player<PlayerInfo> player, long frame)
            : base(GameCommandType.RemovePlayerShip, player, frame)
        {
        }
    }
}
