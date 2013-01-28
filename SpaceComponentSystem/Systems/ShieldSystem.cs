using System.Linq;
using Engine.ComponentSystem.Physics;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
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

        public override void OnComponentAdded(Engine.ComponentSystem.Components.IComponent component)
        {
            base.OnComponentAdded(component);

            var shield = component as ShieldEnergyStatusEffect;
            if (shield == null)
            {
                return;
            }

            var body = Manager.GetComponent(shield.Entity, Body.TypeId) as Body;
            if (body == null)
            {
                return;
            }

            var attributes = Manager.GetComponent(shield.Entity, Attributes<AttributeType>.TypeId) as Attributes<AttributeType>;
            if (attributes == null)
            {
                return;
            }

            var existingFixture = (Fixture) body.Fixtures.First();
            System.Diagnostics.Debug.Assert(existingFixture != null);

            var shieldRadius = UnitConversion.ToSimulationUnits(attributes.GetValue(AttributeType.ShieldRadius));
            shield.Fixture = Manager.AttachCircle(
                body,
                shieldRadius,
                collisionCategory: existingFixture.CollisionCategory | Factions.Shields.ToCollisionGroup(),
                collisionMask: existingFixture.CollisionMask).Id;
        }

        public override void OnComponentRemoved(Engine.ComponentSystem.Components.IComponent component)
        {
            base.OnComponentRemoved(component);
            
            var shield = component as ShieldEnergyStatusEffect;
            if (shield != null && shield.Fixture >= 0)
            {
                Manager.RemoveComponent(shield.Fixture);
            }
        }
    }
}