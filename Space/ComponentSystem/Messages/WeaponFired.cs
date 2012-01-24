using Space.ComponentSystem.Modules;

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
        public WeaponModule Weapon;
    }
}
