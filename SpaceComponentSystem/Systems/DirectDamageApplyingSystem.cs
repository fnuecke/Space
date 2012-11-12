using System;
using System.Collections.Generic;
using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to apply 'direct' damage, i.e. damage that is applied once for
    /// its full amount. This covers all possible damage types.
    /// </summary>
    public sealed class DirectDamageApplyingSystem : AbstractDamageApplyingSystem
    {
        private static readonly List<Tuple<AttributeType, AttributeType, DamageType>> DirectDamages =
            new List<Tuple<AttributeType, AttributeType, DamageType>>
        {
            Tuple.Create(AttributeType.AttackPhysicalMinDamage, AttributeType.AttackPhysicalMaxDamage, DamageType.Physical),
            Tuple.Create(AttributeType.AttackFireMinDamage, AttributeType.AttackFireMaxDamage, DamageType.Fire),
            Tuple.Create(AttributeType.AttackIceMinDamage, AttributeType.AttackIceMaxDamage, DamageType.Ice),
            Tuple.Create(AttributeType.AttackAcidMinDamage, AttributeType.AttackAcidMaxDamage, DamageType.Acid),
            Tuple.Create(AttributeType.AttackElectricMinDamage, AttributeType.AttackElectricMaxDamage, DamageType.Electric)
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

            foreach (var value in DirectDamages)
            {
                // Get min and max damage, make sure it's larger or equal to zero.
                var minDamage = attributes.GetValue(value.Item1);
                var maxDamage = attributes.GetValue(value.Item2);
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

                // Get the damage type and apply a damage debuff.
                var type = value.Item3;
                Manager.AddComponent<DamagingStatusEffect>(damagee).
                    Initialize(minDamage, maxDamage,
                               critChance, critMultiplier,
                               type, owner);

                // Special case: slow debuff on ice damage.
                if (type == DamageType.Ice)
                {
                    // See how long we last.
                    var minDuration = attributes.GetValue(AttributeType.AttackColdMinDuration);
                    var maxDuration = attributes.GetValue(AttributeType.AttackColdMaxDuration);
                    if (minDuration < 0)
                    {
                        minDuration = 0;
                    }
                    if (maxDuration < minDuration)
                    {
                        maxDuration = minDuration;
                    }
                    var reduction = damageeAttributes != null ? damageeAttributes.GetValue(AttributeType.ColdAndFreezeDurationReduction) : 0;
                    var duration = (int)Math.Round((Random.NextDouble(minDuration, maxDuration) - reduction) * Settings.TicksPerSecond);
                    if (duration <= 0)
                    {
                        continue;
                    }

                    // See if the target is already cold-slowed, if so just prolong
                    // the effect (if our duration is longer).
                    var effect = (ColdSlowStatusEffect)Manager.GetComponent(damagee, ColdSlowStatusEffect.TypeId);
                    if (effect != null)
                    {
                        effect.Remaining = Math.Max(effect.Remaining, duration);
                    }
                    else
                    {
                        Manager.AddComponent<ColdSlowStatusEffect>(damagee).Initialize(duration);
                    }
                }
            }
        }
    }
}
