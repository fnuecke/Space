using System.Collections.Generic;
using System.Xml.Serialization;

namespace Space.Data
{
    /// <summary>
    /// Possible item types for ships.
    /// </summary>
    public enum ShipModuleType
    {
        /// <summary>
        /// Hull, mainly determines ship health (and regeneration) and armor
        /// rating.
        /// </summary>
        [XmlEnum(Name = "Hull")]
        Hull,

        /// <summary>
        /// Reactor, mainly determines ship energy (and regeneration).
        /// </summary>
        [XmlEnum(Name = "Reactor")]
        Reactor,

        /// <summary>
        /// Thruster, mainly determines ship acceleration and turn speed.
        /// </summary>
        [XmlEnum(Name = "Thruster")]
        Thruster,

        /// <summary>
        /// Shield, mainly determines ship shield efficiency.
        /// </summary>
        [XmlEnum(Name = "Shield")]
        Shield,

        /// <summary>
        /// Weapon, special component that has attributes only relevant to
        /// itself (cooldown, projectile properties).
        /// </summary>
        [XmlEnum(Name = "Weapon")]
        Weapon
    }

    /// <summary>
    /// Extension method to check if a module is required or not.
    /// </summary>
    public static class ShipModuleTypeExtension
    {
        private static Dictionary<ShipModuleType, bool> _required =
            new Dictionary<ShipModuleType, bool>()
            {
                { ShipModuleType.Hull, true },
                { ShipModuleType.Reactor, true },
                { ShipModuleType.Thruster, true },
                { ShipModuleType.Shield, false },
                { ShipModuleType.Weapon, false }
            };

        /// <summary>
        /// Determines if a module of a certain type is required, i.e. may not
        /// be unequipped (only replaced).
        /// </summary>
        /// <param name="type">The type of module to check.</param>
        /// <returns>Whether that type is required or not.</returns>
        public static bool IsRequired(this ShipModuleType type)
        {
            return _required[type];
        }
    }
}
