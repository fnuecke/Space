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

    /// <summary>
    /// Utility methods for item slot sizes.
    /// </summary>
    public static class ItemSlotSizeExtensions
    {
        /// <summary>
        /// Returns the actual pixel size at which an object in that slot should be rendered.
        /// </summary>
        /// <param name="size">The slot size.</param>
        /// <returns>The pixel size of the slot.</returns>
        public static int ToPixelSize(this ItemSlotSize size)
        {
            switch (size)
            {
                case ItemSlotSize.Small:
                    return 16;
                case ItemSlotSize.Medium:
                    return 24;
                case ItemSlotSize.Large:
                    return 32;
                default:
                    return 48;
            }
        }
    }
}