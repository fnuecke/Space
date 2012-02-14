using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a sensor item, which is used to detect stuff.
    /// </summary>
    public sealed class Sensor : SpaceItem
    {
        /// <summary>
        /// Creates a new sensor with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public Sensor(string name, string iconName, ItemQuality quality)
            : base(name, iconName, quality)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Sensor()
        {

        }
    }
}
