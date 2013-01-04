using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    internal sealed class DropCommand : FrameCommand
    {
        #region Fields

        /// <summary>
        /// The position of the item to equip in the player's inventory.
        /// </summary>
        public int InventoryIndex;

        /// <summary>
        /// The Source of the command
        /// </summary>
        public Source Source;

        #endregion

        #region Constructor

        public DropCommand(int slot, Source source)
            : base(SpaceCommandType.DropItem)
        {
            InventoryIndex = slot;
            Source = source;
        }

        public DropCommand()
            : this(-1, Source.None)
        {
        }

        #endregion
    }
}
