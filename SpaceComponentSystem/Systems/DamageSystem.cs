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
            var damage = component.Value;
            var health = ((Health)Manager.GetComponent(component.Entity, Health.TypeId));

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
                            // Got some, apply shield armor rating.
                            damage -= reduction;

                            // Don't allow healing ;)
                            if (damage <= 0)
                            {
                                return;
                            }

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
                            damage -= blockable; // <= 0 test is done below anyway
                        }
                    }
                }

                // Compute physical damage we take by applying armor rating.
                damage -= character.GetValue(AttributeType.DamageReduction, character.GetBaseValue(AttributeType.DamageReduction));

                // Don't allow healing ;)
                if (damage <= 0)
                {
                    return;
                }
            }

            // Apply whatever remains as physical damage.
            health.SetValue(health.Value - damage, component.Owner);
        }
    }
}
