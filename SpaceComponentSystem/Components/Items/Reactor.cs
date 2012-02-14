using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a reactor item, which is used to store and produce energy.
    /// </summary>
    public sealed class Reactor : SpaceItem
    {
        /// <summary>
        /// Creates a new reactor with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public Reactor(string name, string iconName, ItemQuality quality)
            : base(name, iconName, quality)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Reactor()
        {
        }
    }
}
