using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Messages
{
    /// <summary>
    /// Dispatched when a single weapon is fired.
    /// </summary>
    public struct WeaponFired
    {
        /// <summary>
        /// The weapon that was fired.
        /// </summary>
        public Weapon Weapon;

        /// <summary>
        /// The ship to which the fired weapon is attached.
        /// </summary>
        public int ShipEntity;
    }
}
