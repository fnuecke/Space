using Engine.Commands;

namespace Space.Commands
{
    abstract class GameCommand : FrameCommand
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }
    }
}
