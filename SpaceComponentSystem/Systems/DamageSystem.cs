using System.Diagnostics;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of applying damaging status effects each tick.
    /// </summary>
    public sealed class DamageSystem : AbstractParallelComponentSystem<DamagingStatusEffect>
    {
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
            else
            {
                // Done waiting through one interval, re-set delay
                // and apply the damage.
                component.Delay = component.Interval;
            }

            var damage = component.Value;
            var health = (Health)Manager.GetComponent(component.Entity, Health.TypeId);

            // Check for character info that could modify damage values.
            var character = (Character<AttributeType>)Manager.GetComponent(component.Entity, Character<AttributeType>.TypeId);
            if (character != null)
            {
                // We got some character! We need to get some more info, e.g. if shields are up.
                var control = (ShipControl)Manager.GetComponent(component.Entity, ShipControl.TypeId);
                if (control.ShieldsActive)
                {
                    // Yes, shields are up, see if we have any energy to operate them.
                    var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
                    if (energy.Value > 0)
                    {
                        // See how much damage we can reduce.
                        var reduction = character.GetValue(AttributeType.ShieldDamageReduction,
                                                           character.GetBaseValue(AttributeType.ShieldDamageReduction));
                        if (reduction > 0)
                        {
                            // Got some, determine coverage to see if we have a multiplicator.
                            var coverage = character.GetValue(AttributeType.ShieldCoverage,
                                                              character.GetBaseValue(AttributeType.ShieldCoverage));
                            if (coverage > 1f)
                            {
                                // Got a surplus, act as damage reduction multiplier.
                                reduction *= coverage;
                            }

                            // Got some, apply shield armor rating, but cap it at 75%.
                            // TODO make a static class with some game-play constants including the shield armor damage reduction cap
                            damage -= System.Math.Min(0.75f * damage, reduction);

                            // How much energy do we need to block one point of damage?
                            var cost = character.GetValue(AttributeType.ShieldBlockCost,
                                                          character.GetBaseValue(AttributeType.ShieldBlockCost));

                            // Compute how much energy we need to block the remaining amount of damage.
                            var actualCost = damage * cost;
                            if (actualCost < energy.Value)
                            {
                                // We can block it all! Just subtract the energy and we're done.
                                energy.SetValue(energy.Value - actualCost, component.Owner);
                                return;
                            }

                            // See how much we *can* block, consume energy required for that and
                            // subtract the blocked amount of damage.
                            var blockable = energy.Value / cost;
                            energy.SetValue(0, component.Owner);

                            // The following assert must hold because from above:
                            // damage * cost >= energy.Value
                            // because actualCost >= energy.Value, otherwise we'd have returned by now.
                            // -> ... / cost
                            // -> damage >= energy.Value / cost
                            // with blockable = energy.Value / cost this gives
                            // -> damage >= blockable
                            // And that tells us we won't "heal" by overshielding.
                            Debug.Assert(damage >= blockable);

                            damage -= blockable;
                        }
                    }
                }

                // Compute physical damage we take by applying armor rating, but cap the
                // damage reduction at 75%.
                // TODO make a static class with some game-play constants including the armor damage reduction cap
                damage -= System.Math.Min(0.75f * damage, character.GetValue(AttributeType.DamageReduction, character.GetBaseValue(AttributeType.DamageReduction)));
            }

            // Apply whatever remains as direct physical damage.
            health.SetValue(health.Value - damage, component.Owner);
        }
    }
}
