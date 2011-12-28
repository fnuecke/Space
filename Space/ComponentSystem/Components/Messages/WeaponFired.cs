using Space.Data.Modules;

namespace Space.ComponentSystem.Components.Messages
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

        public static WeaponFired Create(WeaponModule weapon)
        {
            WeaponFired result;
            result.Weapon = weapon;
            return result;
        }
    }
}
