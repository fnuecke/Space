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
        /// Acceleration modifier for an entity.
        /// </summary>
        Acceleration,

        /// <summary>
        /// Rotation speed modifier for an entity.
        /// </summary>
        RotationSpeed,

        /// <summary>
        /// Energy consumption of all engines on the entity.
        /// </summary>
        EngineEnergyConsumption,

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
        WeaponCooldown
    }
}
