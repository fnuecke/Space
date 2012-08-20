using Engine.ComponentSystem.RPG.Components;

namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// Sent by the <c>Equipment</c> component when an item is equipped.
    /// </summary>
    public struct ItemEquipped
    {
        /// <summary>
        /// The item that was equipped.
        /// </summary>
        public int Item;

        /// <summary>
        /// The slot to which the item was added.
        /// </summary>
        public ItemSlot Slot;
    }
}
