using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single armor item, which determines an entity's armor
    /// rating.
    /// </summary>
    public sealed class Armor : SpaceItem
    {
        /// <summary>
        /// Creates a new armor with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public Armor(string name, string iconName, ItemQuality quality)
            : base(name, iconName, quality)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Armor()
        {
        }
    }
}
