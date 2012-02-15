namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// Sent by the <c>Equipment<c> component when an item is equipped.
    /// </summary>
    public struct ItemAdded
    {
        /// <summary>
        /// The item that was equipped.
        /// </summary>
        public Entity Item;

        /// <summary>
        /// The slot to which the item was added.
        /// </summary>
        public int Slot;
    }
}
