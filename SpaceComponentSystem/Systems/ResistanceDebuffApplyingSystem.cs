using System;
using System.Collections.Generic;
using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to apply armor and resistance debuffs on hit.
    /// </summary>
    public sealed class ResistanceDebuffApplyingSystem : AbstractDamageApplyingSystem
    {
        private static readonly List<Tuple<AttributeType, AttributeType>> DamageReductionDebuffs =
            new List<Tuple<AttributeType, AttributeType>>
            {
                Tuple.Create(AttributeType.AttackReduceArmorDamageReduction, AttributeType.PhysicalDamageReduction),
                Tuple.Create(AttributeType.AttackReduceShieldDamageReduction, AttributeType.ShieldDamageReduction),
                Tuple.Create(AttributeType.AttackReduceFireDamageReduction, AttributeType.FireDamageReduction),
                Tuple.Create(AttributeType.AttackReduceIceDamageReduction, AttributeType.IceDamageReduction),
                Tuple.Create(AttributeType.AttackReduceAcidDamageReduction, AttributeType.AcidDamageReduction),
                Tuple.Create(AttributeType.AttackReduceElectricDamageReduction, AttributeType.AttackReduceElectricDamageReduction)
            };

        /// <summary>
        /// Applies the damage for this system.
        /// </summary>
        /// <param name="owner">The entity that caused the damage.</param>
        /// <param name="attributes">The attributes of the entity doing the damage.</param>
        /// <param name="damagee">The entity being damage.</param>
        protected override void ApplyDamage(int owner, Attributes<AttributeType> attributes, int damagee)
        {
            foreach (var value in DamageReductionDebuffs)
            {
                var amount = attributes.GetValue(value.Item1);
                if (amount <= 0)
                {
                    // Only allow reduction via these attribute types.
                    continue;
                }

                // See if we already have such a buff, if so reset duration, else create it.
                var handled = false;
                foreach (ArmorReductionStatusEffect effect in Manager.GetComponents(damagee, ArmorReductionStatusEffect.TypeId))
                {
                    if (effect.Modifiers[0].Type == value.Item1)
                    {
                        effect.Remaining = Math.Max(effect.Remaining, GameLogicConstants.ArmorDebuffDuration);
                        handled = true;
                        break;
                    }
                }
                if (!handled)
                {
                    // None yet, create it.
                    Manager.AddComponent<ArmorReductionStatusEffect>(damagee).Initialize(-amount, value.Item2);
                }
            }
        }
    }
}
