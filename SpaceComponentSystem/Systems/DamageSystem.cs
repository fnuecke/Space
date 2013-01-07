using System.Diagnostics;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of applying damaging status effects each tick.
    /// </summary>
    public sealed class DamageSystem : AbstractParallelComponentSystem<DamagingStatusEffect>
    {
        #region Fields

        /// <summary>
        /// Randomizer used for determining actual damage (from [min,max] interval).
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic
        
        /// <summary>
        /// Updates the component.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, DamagingStatusEffect component)
        {
            // We trigger in certain intervals, as defined by the component's
            // inverval field. We do this via keeping track of the delay before
            // the next damage is dealt.
            if (component.Delay > 0)
            {
                // Still waiting.
                --component.Delay;
                return;
            }

            // Done waiting through one interval, re-set delay
            // and apply the damage.
            component.Delay = component.Interval;

            // Check if it's a crit.
            var isCriticalHit = false;
            var damageMultiplier = 1f;
            {
                if (_random.NextDouble() < component.ChanceToCrit)
                {
                    isCriticalHit = true;
                    damageMultiplier = component.CriticalDamageMultiplier;
                }
            }

            var damage = damageMultiplier * (float)_random.NextDouble(component.MinValue, component.MaxValue);
            var typeEffectiveness = GameLogicConstants.DamageReductionEffectiveness[component.Type];

            // We fill in some data over the course of the next checks.
            DamageApplied message;
            message.Entity = component.Entity;
            message.Type = component.Type;
            message.IsCriticalHit = isCriticalHit;
            message.Amount = 0;
            message.ShieldedAmount = 0;

            // Check for attribute info that could modify damage values.
            var attributes = (Attributes<AttributeType>)Manager.GetComponent(component.Entity, Attributes<AttributeType>.TypeId);
            if (attributes != null)
            {
                // Apply our resistances.
                foreach (var entry in typeEffectiveness)
                {
                    var reductionType = entry.Key;
                    var effectiveness = entry.Value;

                    // Special handling for shield reduction follows below. Unlike all other
                    // resistances shields consume energy when absorbing damage and must be
                    // active...
                    if (reductionType == AttributeType.ShieldDamageReduction)
                    {
                        continue;
                    }

                    // Skip if base effectiveness is already zero (cannot block this damage type).
                    if (effectiveness <= 0f)
                    {
                        continue;
                    }

                    var reduction = effectiveness * attributes.GetValue(reductionType);
                    if (reduction > 0f)
                    {
                        // Positive damage reduction means we take less damage. Cap it to avoid
                        // complete immunity.
                        damage -= System.Math.Min(GameLogicConstants.DamageReductionCap * damage, reduction);
                    }
                    else
                    {
                        // Negative reduction means increased damage. This is possible from debuffs
                        // which may reduce the armor rating.
                        damage -= System.Math.Max(GameLogicConstants.NegativeDamageReductionCap * damage, reduction);
                    }
                }

                // Apply shield damage reduction and absorption.
                {
                    // Get our effectiveness against that damage type.
                    var effectiveness = typeEffectiveness[AttributeType.ShieldDamageReduction];
                    if (effectiveness > 0)
                    {
                        // Are the shields up?
                        var control = (ShipControl)Manager.GetComponent(component.Entity, ShipControl.TypeId);
                        if (control != null && control.ShieldsActive)
                        {
                            // Yes, shields are up, see if we have any energy to operate them.
                            var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
                            if (energy.Value > 0f)
                            {
                                // See how much damage we can reduce, and apply our effectiveness.
                                var reduction = effectiveness * attributes.GetValue(AttributeType.ShieldDamageReduction);
                                if (reduction > 0f)
                                {
                                    // Got some, determine coverage to see if we have a multiplicator.
                                    var coverage = attributes.GetValue(AttributeType.ShieldCoverage);
                                    if (coverage > 1f)
                                    {
                                        // Got a surplus, act as damage reduction multiplier.
                                        reduction *= coverage;
                                    }

                                    // Cap the reduction and apply it.
                                    damage -= System.Math.Min(GameLogicConstants.DamageReductionCap * damage, reduction);

                                    // How much energy do we need to block one point of damage?
                                    var cost = attributes.GetValue(
                                        AttributeType.ShieldEnergyConsumptionPerAbsorbedDamage);

                                    // Compute how much energy we need to block the remaining amount of
                                    // damage. We apply the effectiveness again, to make it more expensive
                                    // to absorb damage of types that shields are not so good against.
                                    var actualCost = damage * cost / effectiveness;
                                    if (actualCost < energy.Value)
                                    {
                                        // We can block it all! Just subtract the energy and we're done.
                                        energy.SetValue(energy.Value - actualCost, component.Owner);

                                        // Notify some other systems (floating text, effects, ...)
                                        message.ShieldedAmount = damage;
                                        damage = 0;
                                    }
                                    else
                                    {
                                        // See how much we *can* block, consume energy required for that and
                                        // subtract the blocked amount of damage. Again, we need to take into
                                        // account the effectiveness of the shield against that type of damage.
                                        var blockable = energy.Value / cost * effectiveness;
                                        energy.SetValue(0, component.Owner);

                                        // The following assert must hold because from above:
                                        // damage * cost / effectiveness >= energy.Value
                                        // because actualCost >= energy.Value, otherwise we'd have returned by now.
                                        // -> ... / cost * effectiveness
                                        // -> damage >= energy.Value / cost * effectiveness
                                        // with blockable = energy.Value / cost * effectiveness this gives
                                        // -> damage >= blockable
                                        // And that tells us we won't "heal" by overshielding.
                                        Debug.Assert(damage >= blockable);

                                        message.ShieldedAmount = blockable;
                                        damage -= blockable;
                                    }
                                }
                                else if (reduction < 0f)
                                {
                                    // Negative reduction means increased damage. This is possible from debuffs
                                    // which may reduce the armor rating. We don't want to absorb anything
                                    // in that case, just cap the damage increase.
                                    damage -= System.Math.Max(GameLogicConstants.NegativeDamageReductionCap * damage, reduction);
                                }
                            }
                        }
                    }
                }
            }

            // If we don't do any damage (all absorbed via resistances) we can skip the rest.
            if (damage > 0f)
            {
                // Apply whatever remains as direct physical damage.
                var health = (Health)Manager.GetComponent(component.Entity, Health.TypeId);
                health.SetValue(health.Value - damage, component.Owner);
            }

            // Notify some other systems (floating text, effects, ...)
            message.Amount = damage;
            Manager.SendMessage(message);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>
        /// The copy.
        /// </returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (DamageSystem)base.NewInstance();

            copy._random = new MersenneTwister(0);

            return copy;
        }

        #endregion
    }
}
