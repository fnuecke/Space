using System.Collections.Generic;
namespace Space.Data
{
    /// <summary>
    /// Types of ship attributes. This is used for grouping attributes for
    /// actual game logic, i.e. all attributes of the same type are computed
    /// together, then used somehow.
    /// </summary>
    public enum AttributeType
    {
        /// <summary>
        /// Invalid attribute.
        /// </summary>
        None,

        /// <summary>
        /// Health of a entity.
        /// </summary>
        Health,

        /// <summary>
        /// Energy of an entity, used for movement, shields and shooting.
        /// </summary>
        Energy,

        /// <summary>
        /// Speed in units per tick with which an entities health replenishes.
        /// </summary>
        HealthRegeneration,
        
        /// <summary>
        /// Speed in units per tick with which an entities energy replenishes.
        /// </summary>
        EnergyRegeneration,

        /// <summary>
        /// Armor rating of a entity.
        /// </summary>
        Armor,

        /// <summary>
        /// Mass of an entity, determines how well it accelerates / rotates.
        /// </summary>
        Mass,

        /// <summary>
        /// Modifier for force applied for acceleration for an entity.
        /// </summary>
        AccelerationForce,

        /// <summary>
        /// Modifier for force applied for rotation for an entity.
        /// </summary>
        RotationForce,

        /// <summary>
        /// Energy consumption of all thrusters on the entity.
        /// </summary>
        ThrusterEnergyConsumption,

        /// <summary>
        /// Energy consumption of all active shields on the entity.
        /// </summary>
        ShieldEnergyConsumption,

        /// <summary>
        /// Energy consumption of all weapons on the entity.
        /// </summary>
        WeaponEnergyConsumption,

        /// <summary>
        /// Shield efficiency (damage to energy rate) for an entity.
        /// </summary>
        ShieldEfficiency,

        /// <summary>
        /// Weapon cooldown modifier for an entity.
        /// </summary>
        WeaponCooldown,

        /// <summary>
        /// Radar Distance for an entity.
        /// </summary>
        SensorRange,

        WeaponDamage
    }

    public static class AttributeTypeExtension
    {
        #region Constants
        
        private static readonly Dictionary<AttributeType, string> _stringLookup = new Dictionary<AttributeType, string>()
        {
            { AttributeType.Health, AttributeStrings.Health },
            { AttributeType.Energy, AttributeStrings.Energy },
            { AttributeType.HealthRegeneration, AttributeStrings.HealthRegeneration },
            { AttributeType.EnergyRegeneration, AttributeStrings.EnergyRegeneration },
            { AttributeType.Armor, AttributeStrings.Armor },
            { AttributeType.Mass, AttributeStrings.Mass },
            { AttributeType.AccelerationForce, AttributeStrings.AccelerationForce },
            { AttributeType.RotationForce, AttributeStrings.RotationForce },
            { AttributeType.ThrusterEnergyConsumption, AttributeStrings.ThrusterEnergyConsumption },
            { AttributeType.ShieldEnergyConsumption, AttributeStrings.ShieldEnergyConsumption },
            { AttributeType.WeaponEnergyConsumption, AttributeStrings.WeaponEnergyConsumption },
            { AttributeType.ShieldEfficiency, AttributeStrings.ShieldEfficiency },
            { AttributeType.WeaponCooldown, AttributeStrings.WeaponCooldown },
            { AttributeType.SensorRange, AttributeStrings.SensorRange }
        };

        #endregion

        /// <summary>
        /// Get the localized display name for a specified attribute type.
        /// </summary>
        /// <param name="attributeType">The attribute type.</param>
        /// <returns>The localized display string.</returns>
        public static string ToLocalizedString(this AttributeType attributeType)
        {
            return _stringLookup[attributeType];
        }
    }
}
