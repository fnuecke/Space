using Engine.ComponentSystem.RPG.Components;

namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    ///     Sent by the <see cref="ItemSlot"/> component when an item is equipped.
    /// </summary>
    public struct ItemEquipped
    {
        /// <summary>The item that was equipped.</summary>
        public int Item;

        /// <summary>The slot to which the item was added.</summary>
        public ItemSlot Slot;
    }
}