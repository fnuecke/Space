using System.ComponentModel;

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
        [Browsable(false)]
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
        /// Returns the scale at which an object in that slot should be rendered.
        /// </summary>
        /// <param name="size">The slot size.</param>
        /// <returns>
        /// The scaling.
        /// </returns>
        public static float Scale(this ItemSlotSize size)
        {
            switch (size)
            {
                case ItemSlotSize.Small:
                    return 0.5f;
                case ItemSlotSize.Medium:
                    return 0.7f;
                case ItemSlotSize.Large:
                    return 0.85f;
                default:
                    return 1f;
            }
        }
    }
}