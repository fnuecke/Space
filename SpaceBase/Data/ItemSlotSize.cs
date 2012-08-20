namespace Space.Data
{
    /// <summary>
    /// Different possible item sizes, which is used to determine which items
    /// fit into which slot, in addition to the item type.
    /// </summary>
    public enum ItemSlotSize
    {
        /// <summary>
        /// Invalid value.
        /// </summary>
        None,

        /// <summary>
        /// Small items.
        /// </summary>
        Small,
        
        /// <summary>
        /// Medium sized items.
        /// </summary>
        Medium,

        /// <summary>
        /// Large sized items.
        /// </summary>
        Large,

        /// <summary>
        /// Huge sized items.
        /// </summary>
        Huge
    }
}