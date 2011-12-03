using Engine.Commands;
using Space.Model;

namespace Space.Commands
{
    abstract class GameCommand : FrameCommand<GameCommandType, PlayerInfo, PacketizerContext>
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }
    }
}
