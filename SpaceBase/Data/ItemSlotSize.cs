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
        /// <param name="pixelSize">The original size to scale.</param>
        /// <returns>
        /// The pixel size of the slot.
        /// </returns>
        public static float Scale(this ItemSlotSize size, float pixelSize)
        {
            switch (size)
            {
                case ItemSlotSize.Small:
                    return pixelSize;
                case ItemSlotSize.Medium:
                    return pixelSize * 1.5f;
                case ItemSlotSize.Large:
                    return pixelSize * 2f;
                default:
                    return pixelSize * 3f;
            }
        }
    }
}