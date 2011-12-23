using Space.Data;
namespace Space.ComponentSystem.Components.Messages
{
    public struct WeaponFired
    {
        public WeaponModule Weapon;

        public static WeaponFired Create(WeaponModule weapon)
        {
            WeaponFired result;
            result.Weapon = weapon;
            return result;
        }
    }
}
