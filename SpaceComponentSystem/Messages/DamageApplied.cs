using Space.Data;

namespace Space.ComponentSystem.Messages
{
    /// <summary>
    /// This message is fired when damage is actually applied to an entity,
    /// meaning their health value is altered.
    /// </summary>
    public struct DamageApplied
    {
        /// <summary>
        /// The entity that was damaged.
        /// </summary>
        public int Entity;

        /// <summary>
        /// The amount of damage applied.
        /// </summary>
        public float Amount;

        /// <summary>
        /// The type of damage taken.
        /// </summary>
        public DamageType Type;

        /// <summary>
        /// Whether the damage was caused by a critical hit.
        /// </summary>
        public bool IsCriticalHit;
    }
}
