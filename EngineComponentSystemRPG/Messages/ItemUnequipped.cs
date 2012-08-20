using Engine.ComponentSystem.RPG.Components;

namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// Sent by the <c>Equipment</c> component when an item is unequipped.
    /// </summary>
    public struct ItemUnequipped
    {
        /// <summary>
        /// The item that was unequipped.
        /// </summary>
        public int Item;

        /// <summary>
        /// The slot from which the item was removed.
        /// </summary>
        public ItemSlot Slot;
    }
}
