using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    public abstract class GameCommand : FrameCommand
    {
        public GameCommand(GameCommandType type)
            : base(type)
        {
        }
    }
}
