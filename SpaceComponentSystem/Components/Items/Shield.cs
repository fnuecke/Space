using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a shield item, which blocks damage.
    /// </summary>
    public sealed class Shield : SpaceItem
    {
        /// <summary>
        /// Creates a new shield with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public Shield(string name, string iconName, ItemQuality quality)
            : base(name, iconName, quality)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Shield()
        {
        }
    }
}
