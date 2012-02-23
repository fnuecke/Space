using System;
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
        
        private static readonly Dictionary<AttributeType, float> ValueLookup = new Dictionary<AttributeType, float> {
            { AttributeType.Health, 1 },
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
            { AttributeType.SensorRange, 0.1f},
            { AttributeType.WeaponDamage, 80 }
        };

        #endregion

        /// <summary>
        /// Get the localized display name for a specified attribute type.
        /// </summary>
        /// <param name="attributeType">The attribute type.</param>
        /// <returns>The localized display string.</returns>
        public static string ToLocalizedString(this AttributeType attributeType)
        {
            var name = Enum.GetName(typeof(AttributeType), attributeType);
            return AttributeNames.ResourceManager.GetString(name)
                ?? "!!AttributeNames:" + name + "!!";
        }

        /// <summary>
        /// Get the localized prefix for the given attribute type.
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The localized prefix string.</returns>
        public static string ToLocalizedPrefixString(this AttributeType attributeType)
        {
            var name = Enum.GetName(typeof(AttributeType), attributeType);
            return AttributePrefixes.ResourceManager.GetString(name)
                ?? "!!AttributePrefixes:" + name + "!!";
        }

        /// <summary>
        /// Get the value for this Attribute calculated by the value of the Attribute, can be negative.
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <param name="value">The value of the Attribute</param>
        /// <returns>The calculated vale</returns>
        public static float GetValue(this AttributeType attributeType,float value)//,AttributeComputationType modifier )
        {
            return ValueLookup[attributeType] * value;
        }
    }
}
