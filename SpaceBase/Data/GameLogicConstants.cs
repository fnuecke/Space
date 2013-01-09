using System.Collections.Generic;
using Space.Util;

namespace Space.Data
{
    public static class GameLogicConstants
    {
        #region Caps

        /// <summary>
        ///     Maximum percentage of incoming damage that can be blocked via damage reduction (all types). Note that the
        ///     effective cap differs based on armor and damage type (see effectiveness table), because the cap is applied before
        ///     the effectiveness computation.
        /// </summary>
        public static readonly float DamageReductionCap = 0.75f;

        /// <summary>
        ///     Lower cap for damage reduction. Damage reduction may go negative due to debuffs (reduce armor), which will
        ///     actually lead to the entity taking increased damage. We want to cap that.
        /// </summary>
        public static readonly float NegativeDamageReductionCap = -1f;

        #endregion

        #region Status effects

        /// <summary>Interval in ticks that damage debuffs should be applied at.</summary>
        public static readonly int DamageDebuffInterval = (int) Settings.TicksPerSecond;

        /// <summary>Duration in ticks that damage reduction debuffs (i.e. less damage reduction) last.</summary>
        public static readonly int ArmorDebuffDuration = (int) (5 * Settings.TicksPerSecond);

        /// <summary>Amount by which a unit is slowed when under a cold slow effect.</summary>
        public static readonly float ColdSlowMultiplier = 0.5f;

        #endregion

        #region Resistance vs. Damage ratings

        /// <summary>
        ///     Effectiveness of armor/resistance types against specific damage types. The order goes like so: - Damage is
        ///     registered. - Resistances are applied to damage, reducing the overall value. - If shield is hit, it absorbs as much
        ///     damage as it can based on damage reduction and effectiveness against the damage type. - Remaining damage is reduced
        ///     based on armor rating and effectiveness of physical armor vs damage type. - Remaining damage is applied to health.
        /// </summary>
        /// <remarks>
        ///     This maps damage type to armor type with that armor type's effectiveness, primarily because this way a single
        ///     type lookup suffices in the damage system.
        /// </remarks>
        public static readonly Dictionary<DamageType, Dictionary<AttributeType, float>> DamageReductionEffectiveness =
            new Dictionary<DamageType, Dictionary<AttributeType, float>>
            {
                {
                    DamageType.Physical, new Dictionary<AttributeType, float>
                    {
                        {AttributeType.PhysicalDamageReduction, 1f},
                        {AttributeType.ShieldDamageReduction, 0.95f},
                        {AttributeType.FireDamageReduction, 0f},
                        {AttributeType.IceDamageReduction, 0f},
                        {AttributeType.AcidDamageReduction, 0f},
                        {AttributeType.ElectricDamageReduction, 0f}
                    }
                },
                {
                    DamageType.Fire, new Dictionary<AttributeType, float>
                    {
                        {AttributeType.PhysicalDamageReduction, 0.5f},
                        {AttributeType.ShieldDamageReduction, 0.5f},
                        {AttributeType.FireDamageReduction, 1f},
                        {AttributeType.IceDamageReduction, 0f},
                        {AttributeType.AcidDamageReduction, 0f},
                        {AttributeType.ElectricDamageReduction, 0f}
                    }
                },
                {
                    DamageType.Ice, new Dictionary<AttributeType, float>
                    {
                        {AttributeType.PhysicalDamageReduction, 0.75f},
                        {AttributeType.ShieldDamageReduction, 0.5f},
                        {AttributeType.FireDamageReduction, 0f},
                        {AttributeType.IceDamageReduction, 1f},
                        {AttributeType.AcidDamageReduction, 0f},
                        {AttributeType.ElectricDamageReduction, 0f}
                    }
                },
                {
                    DamageType.Acid, new Dictionary<AttributeType, float>
                    {
                        {AttributeType.PhysicalDamageReduction, 0.25f},
                        {AttributeType.ShieldDamageReduction, 0.75f},
                        {AttributeType.FireDamageReduction, 0f},
                        {AttributeType.IceDamageReduction, 0f},
                        {AttributeType.AcidDamageReduction, 1f},
                        {AttributeType.ElectricDamageReduction, 0f}
                    }
                },
                {
                    DamageType.Electric, new Dictionary<AttributeType, float>
                    {
                        {AttributeType.PhysicalDamageReduction, 1f},
                        {AttributeType.ShieldDamageReduction, 0.25f},
                        {AttributeType.FireDamageReduction, 0f},
                        {AttributeType.IceDamageReduction, 0f},
                        {AttributeType.AcidDamageReduction, 0f},
                        {AttributeType.ElectricDamageReduction, 1f}
                    }
                }
            };

        #endregion
    }
}