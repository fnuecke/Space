using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Consumes energy for active shields and disables them if there is no more energy or no shield equipped.</summary>
    public sealed class ShieldSystem : AbstractUpdatingComponentSystem<ShieldEnergyStatusEffect>
    {
        /// <summary>Updates the component.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ShieldEnergyStatusEffect component)
        {
            // See if we even have an equipped shield.
            if (!HasShield(component.Entity))
            {
                var control = (ShipControl) Manager.GetComponent(component.Entity, ShipControl.TypeId);
                control.ShieldsActive = false;
                Manager.RemoveComponent(component);
                return;
            }

            // Got a shield, consume constant energy drain.
            var energy = (Energy) Manager.GetComponent(component.Entity, Energy.TypeId);
            var attributes =
                (Attributes<AttributeType>) Manager.GetComponent(component.Entity, Attributes<AttributeType>.TypeId);
            var drain = attributes.GetValue(AttributeType.ShieldActiveEnergyConsumption) / Settings.TicksPerSecond;
            energy.SetValue(energy.Value - drain);
        }

        /// <summary>Determines whether the specified entity has a shield.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///     <c>true</c> if the specified entity has a shield; otherwise, <c>false</c>.
        /// </returns>
        private bool HasShield(int entity)
        {
            var equipment = (SpaceItemSlot) Manager.GetComponent(entity, ItemSlot.TypeId);
            foreach (var item in equipment.AllItems)
            {
                if (Manager.GetComponent(item, Shield.TypeId) != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}