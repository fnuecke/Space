using Engine.Commands;
using Space.Model;

namespace Space.Commands
{
    abstract class GameCommand : FrameCommand<PlayerInfo, PacketizerContext>
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }
    }
}
