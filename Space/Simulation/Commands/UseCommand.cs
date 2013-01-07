using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Makes a player use an item from his inventory.
    /// </summary>
    internal sealed class UseCommand : FrameCommand
    {
        #region Fields

        /// <summary>
        /// The index in the inventory of the item to be used.
        /// </summary>
        public int InventoryIndex;

        #endregion

        #region Constructor

        public UseCommand(int inventoryIndex)
            : base(SpaceCommandType.UseItem)
        {
            InventoryIndex = inventoryIndex;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public UseCommand()
            : this(-1)
        {
        }

        #endregion
    }
}
