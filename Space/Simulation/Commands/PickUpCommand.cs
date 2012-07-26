using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Command sent when a player want to pick up all items in his vicinity.
    /// </summary>
    internal sealed class PickUpCommand : FrameCommand
    {
        public PickUpCommand()
            : base(SpaceCommandType.PickUp)
        {
        }
    }
}
