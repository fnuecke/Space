using Engine.Commands;
using Engine.Session;
using Space.Model;

namespace Space.Commands
{
    abstract class GameCommand : SimulationCommand<GameCommandType, PlayerInfo>
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }

        public GameCommand(GameCommandType type, Player<PlayerInfo> player, long frame)
            : base(type, player, frame)
        {
        }
    }
}
