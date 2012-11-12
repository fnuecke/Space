using System;
using System.Collections.Generic;
using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to apply DOTs, i.e. damage that is applied over
    /// a certain period of time.
    /// </summary>
    public sealed class OverTimeDamageApplyingSystem : AbstractDamageApplyingSystem
    {
        private static readonly List<Tuple<AttributeType, AttributeType, AttributeType, AttributeType, AttributeType, AttributeType, DamageType>> OverTimeDamages =
            new List<Tuple<AttributeType, AttributeType, AttributeType, AttributeType, AttributeType, AttributeType, DamageType>>
            {
                Tuple.Create(AttributeType.AttackBurnChance,  AttributeType.BurnDurationReduction,
                             AttributeType.AttackBurnMinDamage, AttributeType.AttackBurnMaxDamage,
                             AttributeType.AttackBurnMinDuration, AttributeType.AttackBurnMaxDuration,
                             DamageType.Fire),
                Tuple.Create(AttributeType.AttackCorrosionChance,  AttributeType.CorrosionDurationReduction,
                             AttributeType.AttackCorrosionMinDamage, AttributeType.AttackCorrosionMaxDamage,
                             AttributeType.AttackCorrosionMinDuration, AttributeType.AttackCorrosionMaxDuration,
                             DamageType.Acid),
                Tuple.Create(AttributeType.AttackRadiationChance,  AttributeType.RadiationDurationReduction,
                             AttributeType.AttackRadiationMinDamage, AttributeType.AttackRadiationMaxDamage,
                             AttributeType.AttackRadiationMinDuration, AttributeType.AttackRadiationMaxDuration,
                             DamageType.Electric),
            };

        /// <summary>
        /// Applies the damage for this system.
        /// </summary>
        /// <param name="owner">The entity that caused the damage.</param>
        /// <param name="attributes">The attributes of the entity doing the damage.</param>
        /// <param name="damagee">The entity being damage.</param>
        protected override void ApplyDamage(int owner, Attributes<AttributeType> attributes, int damagee)
        {
            // Critical hit values. These have to be precomputed and stored in the
            // the status effect, because the owner of the damage effect may die in
            // the meantime (i.e. damage it caused may outlive it), in which case
            // we'd have nowhere to look up these values anymore.
            var critChance = attributes.GetValue(AttributeType.AttackCriticalDamageChance);
            var critMultiplier = attributes.GetValue(AttributeType.AttackCriticalDamageMultiplier);

            // Get damagee attributes for debuff duration reduction.
            var damageeAttributes = (Attributes<AttributeType>)Manager.GetComponent(damagee, Attributes<AttributeType>.TypeId);

            foreach (var value in OverTimeDamages)
            {
                // See if we should proc.
                var chance = attributes.GetValue(value.Item1);
                if (Random.NextDouble() >= chance)
                {
                    continue;
                }

                // Get min and max damage, make sure it's larger or equal to zero.
                var minDamage = attributes.GetValue(value.Item3);
                var maxDamage = attributes.GetValue(value.Item4);
                if (minDamage < 0)
                {
                    minDamage = 0;
                }
                if (maxDamage < minDamage)
                {
                    maxDamage = minDamage;
                }
                if (maxDamage <= 0)
                {
                    continue;
                }

                // See how long we last.
                var minDuration = attributes.GetValue(value.Item5);
                var maxDuration = attributes.GetValue(value.Item6);
                if (minDuration < 0)
                {
                    minDuration = 0;
                }
                if (maxDuration < minDuration)
                {
                    maxDuration = minDuration;
                }

                // Apply duration reduction based on damagee 
                var reduction = damageeAttributes != null ? damageeAttributes.GetValue(value.Item2) : 0;
                var duration = (int)Math.Round((Random.NextDouble(minDuration, maxDuration) - reduction) * Settings.TicksPerSecond);
                if (duration <= 0)
                {
                    continue;
                }

                // Get the damage type and apply a damage debuff.
                var type = value.Item7;
                Manager.AddComponent<DamagingStatusEffect>(damagee).
                    Initialize(duration, GameLogicConstants.DamageDebuffInterval,
                               minDamage, maxDamage,
                               critChance, critMultiplier,
                               type, owner);
            }
        }
    }
}
