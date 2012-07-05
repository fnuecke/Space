using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Space.Data
{
    /// <summary>
    /// List of possible item quality levels.
    /// </summary>
    public enum ItemQuality
    {
        /// <summary>
        /// Poor quality.
        /// </summary>
        Poor,

        /// <summary>
        /// Common quality.
        /// </summary>
        Common,

        /// <summary>
        /// Uncommon quality.
        /// </summary>
        Uncommon,

        /// <summary>
        /// Rare quality.
        /// </summary>
        Rare,

        /// <summary>
        /// Epic quality.
        /// </summary>
        Epic,

        /// <summary>
        /// Legendary quality.
        /// </summary>
        Legendary,

        /// <summary>
        /// Unique quality.
        /// </summary>
        Unique
    }

    /// <summary>
    /// Utility methods for item quality levels.
    /// </summary>
    public static class ItemQualityExtensions
    {
        #region Constants
        
        /// <summary>
        /// Lookup table for colors.
        /// </summary>
        private static readonly Dictionary<ItemQuality, Color> ColorLookup = new Dictionary<ItemQuality, Color>
                                                                             {
            { ItemQuality.Poor, new Color(0x9D, 0x9D, 0x9D) },
            { ItemQuality.Common, new Color(0xFF, 0xFF, 0xFF) },
            { ItemQuality.Uncommon, new Color(0x1E, 0xFF, 0x00) },
            { ItemQuality.Rare, new Color(0x00, 0x70, 0xFF) },
            { ItemQuality.Epic, new Color(0xA3, 0x35, 0xEE) },
            { ItemQuality.Legendary, new Color(0xFF, 0x80, 0x00) },
            { ItemQuality.Unique, new Color(0xE6, 0xCC, 0x80) }
        };

        #endregion

        #region Methods
        
        /// <summary>
        /// Get the display color for the specified item quality.
        /// </summary>
        /// <param name="quality">The item quality.</param>
        /// <returns>The color for that quality.</returns>
        public static Color ToColor(this ItemQuality quality)
        {
            return ColorLookup[quality];
        }

        #endregion
    }
}
