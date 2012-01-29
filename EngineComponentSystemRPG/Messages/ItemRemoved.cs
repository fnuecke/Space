using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// Sent by the <c>Equipment<c> component when an item is unequipped.
    /// </summary>
    public struct ItemRemoved
    {
        /// <summary>
        /// The item that was unequipped.
        /// </summary>
        public Entity Item;

        /// <summary>
        /// The slot from which the item was removed.
        /// </summary>
        public int Slot;
    }
}
