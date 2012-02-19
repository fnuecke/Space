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

        /// <summary>
        /// Modifies the amount of damage all equipped weapons do.
        /// </summary>
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
            { AttributeType.SensorRange, AttributeStrings.SensorRange },
            {AttributeType.WeaponDamage, AttributeStrings.WeaponDamage}
        };
        private static readonly Dictionary<AttributeType, string> _nameLookup = new Dictionary<AttributeType, string>()
        {
            { AttributeType.Health, ItemNames.Health },
            { AttributeType.Energy, ItemNames.Energy },
            { AttributeType.HealthRegeneration, ItemNames.HealthRegeneration },
            { AttributeType.EnergyRegeneration, ItemNames.EnergyRegeneration },
            { AttributeType.Armor, ItemNames.Armor },
            { AttributeType.Mass, ItemNames.Mass },
            { AttributeType.AccelerationForce, ItemNames.AccelerationForce },
            { AttributeType.RotationForce, ItemNames.RotationForce },
            { AttributeType.ThrusterEnergyConsumption, ItemNames.ThrusterEnergyConsumption },
            { AttributeType.ShieldEnergyConsumption, ItemNames.ShieldEnergyConsumption },
            { AttributeType.WeaponEnergyConsumption, ItemNames.WeaponEnergyConsumption },
            { AttributeType.ShieldEfficiency, ItemNames.ShieldEfficiency },
            { AttributeType.WeaponCooldown, ItemNames.WeaponCooldown },
            { AttributeType.SensorRange, ItemNames.SensorRange }
        };
        private static readonly Dictionary<AttributeType, float> _valueLookup = new Dictionary<AttributeType, float>()
        {
            { AttributeType.Health,1 },
            { AttributeType.Energy, 1 },
            { AttributeType.HealthRegeneration, 50 },
            { AttributeType.EnergyRegeneration, 50 },
            { AttributeType.Armor, 1 },
            { AttributeType.Mass, -0.1f },
            { AttributeType.AccelerationForce, 50 },
            { AttributeType.RotationForce, 50 },
            { AttributeType.ThrusterEnergyConsumption, -50 },
            { AttributeType.ShieldEnergyConsumption, -60 },
            { AttributeType.WeaponEnergyConsumption, -70 },
            { AttributeType.ShieldEfficiency, 5 },
            { AttributeType.WeaponCooldown, -20 },
            { AttributeType.SensorRange, 0.1f}
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

        /// <summary>
        /// Get the value for this Attribute calculated by the value of the Attribute, can be negative.
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <param name="value">The value of the Attribute</param>
        /// <returns>The calculated vale</returns>
        public static float GetValue(this AttributeType attributeType,float value)//,AttributeComputationType modifier )
        {
            return _valueLookup[attributeType] * value;
        }

        /// <summary>
        /// Get the localized Prefix for the given attribute type
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The localized Prefix string</returns>
        public static string ToNameString(this AttributeType attributeType)
        {
            return _nameLookup[attributeType];
        }
    }
}
