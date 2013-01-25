using System;
using System.Collections.Generic;

namespace Space.Data
{
    /// <summary>
    ///     Types of ship attributes. This is used for grouping attributes for actual game logic, i.e. all attributes of
    ///     the same type are computed together, then used somehow.
    /// </summary>
    public enum AttributeType
    {
        /// <summary>Invalid attribute.</summary>
        None,

        #region Movement

        /// <summary>Mass of an entity, determines how well it accelerates / rotates.</summary>
        Mass,

        /// <summary>Modifier for force applied for acceleration for an entity.</summary>
        AccelerationForce,

        /// <summary>Modifier for force applied for rotation for an entity.</summary>
        RotationForce,

        /// <summary>
        ///     The maximum velocity of a ship. This allows relatively quick acceleration without getting ridiculously fast,
        ///     making the game feel less lethargic.
        /// </summary>
        MaximumVelocity,

        /// <summary>Energy consumption of all thrusters on the entity.</summary>
        ThrusterEnergyConsumption,

        #endregion

        #region Health / Energy

        /// <summary>Health of a entity.</summary>
        Health,

        /// <summary>Speed in units per tick with which an entities health replenishes.</summary>
        HealthRegeneration,

        /// <summary>Energy of an entity, used for movement, shields and shooting.</summary>
        Energy,

        /// <summary>Speed in units per tick with which an entities energy replenishes.</summary>
        EnergyRegeneration,

        #endregion

        #region Defense

        /// <summary>Armor rating of a entity, i.e. how much physical damage gets blocked.</summary>
        PhysicalDamageReduction,

        /// <summary>Shield armor rating, i.e. how much physical damage gets blocked by the shield.</summary>
        ShieldDamageReduction,

        /// <summary>Fire resistance rating, i.e. how much fire damage gets absorbed.</summary>
        FireDamageReduction,

        /// <summary>Ice resistance rating, i.e. how much ice damage gets absorbed.</summary>
        IceDamageReduction,

        /// <summary>Acid resistance rating, i.e. how much acid damage gets absorbed.</summary>
        AcidDamageReduction,

        /// <summary>Electric resistance rating, i.e. how much electricity damage gets absorbed.</summary>
        ElectricDamageReduction,

        /// <summary>Time reduction of burn damage debuffs in seconds.</summary>
        BurnDurationReduction,

        /// <summary>Time reduction of cold/freeze debuffs in seconds.</summary>
        ColdAndFreezeDurationReduction,

        /// <summary>Time reduction of corrosion damage debuffs in seconds.</summary>
        CorrosionDurationReduction,

        /// <summary>Time reduction of radiation damage debuffs in seconds.</summary>
        RadiationDurationReduction,

        #endregion

        #region Shields

        /// <summary>Shield coverage angle, i.e. the percentage of the ship covered by the shield.</summary>
        ShieldCoverage,

        /// <summary>Shield radius, i.e. how large an area our shields will span.</summary>
        ShieldRadius,

        /// <summary>Energy consumption of all active shields on the entity. The unit is energy per second.</summary>
        ShieldActiveEnergyConsumption,

        /// <summary>
        ///     Shield block chance, i.e. how likely it is damage gets blocked completely, free of additional energy cost.
        ///     Let's pretend this means the shot ricocheted or something :P This is applied upon collision detection.
        /// </summary>
        ShieldBlockChance,

        /// <summary>
        ///     Shield absorbtion cost, i.e. the energy it costs to absorb one damage point, in case the damage wasn't
        ///     blocked. This is handled each time damage is applied (including debuffs).
        /// </summary>
        ShieldEnergyConsumptionPerAbsorbedDamage,

        #endregion

        #region Offense

        /// <summary>Weapon cooldown modifier for an entity.</summary>
        WeaponCooldown,

        /// <summary>Energy consumption of all weapons on the entity.</summary>
        WeaponEnergyConsumption,

        /// <summary>Chance that an attack deals critical damage.</summary>
        AttackCriticalDamageChance,

        /// <summary>Damage modifier when dealing critical damage.</summary>
        AttackCriticalDamageMultiplier,

        /// <summary>Modifies the amount of physical damage of a weapon.</summary>
        AttackPhysicalMinDamage,
        AttackPhysicalMaxDamage,

        /// <summary>Modifies the amount of fire damage of a weapon.</summary>
        AttackFireMinDamage,
        AttackFireMaxDamage,

        /// <summary>Modifies the amount of ice damage of a weapon.</summary>
        AttackIceMinDamage,
        AttackIceMaxDamage,

        /// <summary>Modifies the amount of acid damage of a weapon.</summary>
        AttackAcidMinDamage,
        AttackAcidMaxDamage,

        /// <summary>Modifies the amount of electric damage of a weapon.</summary>
        AttackElectricMinDamage,
        AttackElectricMaxDamage,

        /// <summary>Chance that a hit will trigger a burn dot on the target.</summary>
        AttackBurnChance,

        /// <summary>Modifies the amount of fire damage of a weapon.</summary>
        AttackBurnMinDamage,
        AttackBurnMaxDamage,

        /// <summary>The time in seconds for how long the target should burn.</summary>
        AttackBurnMinDuration,
        AttackBurnMaxDuration,

        /// <summary>Chance that a hit will freeze the target.</summary>
        AttackFreezeChance,

        /// <summary>The time in seconds for how long the target should be frozen.</summary>
        AttackFreezeMinDuration,
        AttackFreezeMaxDuration,

        /// <summary>
        ///     The time in seconds for how long the target should be slowed from cold damage. This debuff is *always* applied
        ///     on cold damage.
        /// </summary>
        AttackColdMinDuration,
        AttackColdMaxDuration,

        /// <summary>Chance that a hit will trigger a corrosion damage debuff on the target.</summary>
        AttackCorrosionChance,

        /// <summary>Modifies the amount of corrosion damage of a corrosion debuff.</summary>
        AttackCorrosionMinDamage,
        AttackCorrosionMaxDamage,

        /// <summary>Time in ticks the corrosion damage debuff should last.</summary>
        AttackCorrosionMinDuration,
        AttackCorrosionMaxDuration,

        /// <summary>Chance that a hit will trigger a corrosion dot on the target.</summary>
        AttackRadiationChance,

        /// <summary>Modifies the amount of radiation damage of a weapon.</summary>
        AttackRadiationMinDamage,
        AttackRadiationMaxDamage,

        /// <summary>Time in ticks the acid damage should last.</summary>
        AttackRadiationMinDuration,
        AttackRadiationMaxDuration,

        /// <summary>Armor reduction debuff on attack.</summary>
        AttackReduceArmorDamageReduction,

        /// <summary>Shield armor debuff on attack.</summary>
        AttackReduceShieldDamageReduction,

        /// <summary>Fire resistance debuff on attack.</summary>
        AttackReduceFireDamageReduction,

        /// <summary>Ice resistance debuff on attack.</summary>
        AttackReduceIceDamageReduction,

        /// <summary>Acid resistance debuff on attack.</summary>
        AttackReduceAcidDamageReduction,

        /// <summary>Electric resistance debuff on attack.</summary>
        AttackReduceElectricDamageReduction,

        #endregion

        #region Sensors

        /// <summary>Radar Distance for an entity.</summary>
        SensorRange,

        #endregion
    }

    public static class AttributeTypeExtension
    {
        #region Constants

        private static readonly Dictionary<AttributeType, float> ValueLookup = new Dictionary<AttributeType, float>
        {
            {AttributeType.Health, 1},
            {AttributeType.Energy, 1},
            {AttributeType.HealthRegeneration, 50},
            {AttributeType.EnergyRegeneration, 50},
            {AttributeType.PhysicalDamageReduction, 1},
            {AttributeType.Mass, -0.1f},
            {AttributeType.AccelerationForce, 50},
            {AttributeType.RotationForce, 50},
            {AttributeType.ThrusterEnergyConsumption, -50},
            {AttributeType.ShieldActiveEnergyConsumption, -60},
            {AttributeType.WeaponEnergyConsumption, -70},
            {AttributeType.ShieldEnergyConsumptionPerAbsorbedDamage, -5},
            {AttributeType.WeaponCooldown, -20},
            {AttributeType.SensorRange, 0.1f},
        };

        #endregion

        /// <summary>Get the localized display name for a specified attribute type.</summary>
        /// <param name="attributeType">The attribute type.</param>
        /// <returns>The localized display string.</returns>
        public static string ToLocalizedString(this AttributeType attributeType)
        {
            var name = Enum.GetName(typeof (AttributeType), attributeType);
            return (name != null ? AttributeNames.ResourceManager.GetString(name) : null)
                   ?? "!!AttributeNames:" + name + "!!";
        }

        /// <summary>Get the localized prefix for the given attribute type.</summary>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The localized prefix string.</returns>
        public static string ToLocalizedPrefixString(this AttributeType attributeType)
        {
            var name = Enum.GetName(typeof (AttributeType), attributeType);
            return (name != null ? AttributePrefixes.ResourceManager.GetString(name) : null)
                   ?? "!!AttributePrefixes:" + name + "!!";
        }

        /// <summary>Get the value for this Attribute calculated by the value of the Attribute, can be negative.</summary>
        /// <param name="attributeType">The attribute type</param>
        /// <param name="value">The value of the Attribute</param>
        /// <returns>The calculated vale</returns>
        public static float GetValue(this AttributeType attributeType, float value)
        {
            return ValueLookup[attributeType] * value;
        }
    }
}