namespace Space.Data
{
    /// <summary>
    /// Types of ship attributes. This is used for grouping attributes for
    /// actual game logic, i.e. all attributes of the same type are computed
    /// together, then used somehow.
    /// </summary>
    public enum EntityAttributeType
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
        RadarDistance
    }
}
