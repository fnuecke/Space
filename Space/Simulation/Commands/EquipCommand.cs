using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>Used to signal a player should equip a specific item from his inventory.</summary>
    internal sealed class EquipCommand : FrameCommand
    {
        #region Fields

        /// <summary>The position of the item to equip in the player's inventory.</summary>
        public readonly int InventoryIndex;

        /// <summary>The slot to equip the item to.</summary>
        public readonly int Slot;

        #endregion

        #region Constructor

        public EquipCommand(int inventoryPosition, int slot)
            : base(SpaceCommandType.Equip)
        {
            InventoryIndex = inventoryPosition;
            Slot = slot;
        }

        public EquipCommand()
            : this(-1, -1) {}

        #endregion
    }
}