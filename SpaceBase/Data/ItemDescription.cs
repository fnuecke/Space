using System.Collections.Generic;
using Engine.ComponentSystem.RPG.Components;

namespace Space.Data
{
    /// <summary>
    /// Utility class used to query item information.
    /// </summary>
    public struct ItemDescription
    {
        /// <summary>
        /// A list of attributes that the item applies when equipped.
        /// </summary>
        /// <remarks>May be empty.</remarks>
        public List<AttributeModifier<AttributeType>> Attributes;

        #region Weapons
        
        /// <summary>
        /// Whether the item is a weapon. The following fields are only
        /// relevant if the item is indeed a weapon.
        /// </summary>
        public bool IsWeapon;

        /// <summary>
        /// The unmodified weapon damage.
        /// </summary>
        public float WeaponDamage;

        /// <summary>
        /// The weapon's unmodified cooldown, in seconds.
        /// </summary>
        public float WeaponCooldown;

        /// <summary>
        /// The unmodified amount of energy the weapon consumes per shot.
        /// </summary>
        public float WeaponEnergyConsumption;

        /// <summary>
        /// The number of projectiles the weapon fires.
        /// </summary>
        public int WeaponProjectileCount;

        #endregion
    }
}
