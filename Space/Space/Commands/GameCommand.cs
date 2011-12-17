using Engine.Commands;
using Space.Model;

namespace Space.Commands
{
    abstract class GameCommand : FrameCommand<PlayerInfo>
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }
    }
}
