using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public sealed class Thruster : SpaceItem
    {
        /// <summary>
        /// Creates a new thruster with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public Thruster(string name, string iconName, ItemQuality quality)
            : base(name, iconName, quality)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Thruster()
        {
        }
    }
}
