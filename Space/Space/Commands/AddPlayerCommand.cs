using Engine.Commands;
using Engine.Session;
using Space.Model;

namespace Space.Commands
{
    class AddPlayerCommand : SimulationCommand<GameCommandType, PlayerInfo>
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        public AddPlayerCommand()
            : base(GameCommandType.AddPlayerShip)
        {
        }

        public AddPlayerCommand(Player<PlayerInfo> player, long frame)
            : base(GameCommandType.AddPlayerShip, frame)
        {
            this.Player = player;
        }
    }
}
