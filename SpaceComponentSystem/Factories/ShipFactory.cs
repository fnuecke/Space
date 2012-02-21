using System;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Basic descriptor for a single ship class.
    /// </summary>
    public sealed class ShipFactory : IFactory
    {
        #region General

        /// <summary>
        /// The name of the ship class, which serves as a unique type
        /// identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The base texture to use for rendering the ship class.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The base collision radius of the ship class.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// The Item Pool which is used if the Entity is destroyed
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string ItemPool;

        /// <summary>
        /// List of basic stats for this ship class.
        /// </summary>
        public AttributeModifierConstraint<AttributeType>[] Attributes;

        #endregion

        #region Equipment slots

        /// <summary>
        /// The number of sensor slots available for this ship class.
        /// </summary>
        public int SensorSlots;

        /// <summary>
        /// The number of armor slots available for this ship class.
        /// </summary>
        public int ArmorSlots;

        /// <summary>
        /// The number of reactor slots available for this ship class.
        /// </summary>
        public int ReactorSlots;

        /// <summary>
        /// The number of shield slots available for this ship class.
        /// </summary>
        public int ShieldSlots;

        /// <summary>
        /// The number of thruster slots available for this ship class.
        /// </summary>
        public int ThrusterSlots;

        /// <summary>
        /// The number of weapon slots available for this ship class.
        /// </summary>
        public int WeaponSlots;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager"> </param>
        /// <param name="faction">The faction the ship belongs to.</param>
        /// <param name="position">The position at which to spawn the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int SampleShip(IManager manager, Factions faction, Vector2 position, IUniformRandom random)
        {
            var entity = CreateShip(manager, faction, position);

            // Add our attributes.
            var character = manager.GetComponent<Character<AttributeType>>(entity);
            foreach (var attribute in Attributes)
            {
                var modifier = attribute.SampleAttributeModifier(random);
                if (modifier.ComputationType == AttributeComputationType.Multiplicative)
                {
                    throw new InvalidOperationException("Base attributes must be additive.");
                }
                character.SetBaseValue(modifier.Type, modifier.Value);
            }

            return entity;
        }

        /// <summary>
        /// Sets up the basic ship data that is deterministic.
        /// </summary>
        /// <param name="manager"> </param>
        /// <param name="faction"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private int CreateShip(IManager manager, Factions faction, Vector2 position)
        {
            var entity = manager.AddEntity();

            // Draw ships above everything else.
            var renderer = manager.AddComponent<TextureRenderer>(entity).Initialize(Texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            renderer.UpdateOrder = 50;

            // Friction has to be updated before acceleration is, to allow
            // maximum speed to be reached.
            var friction = manager.AddComponent<Friction>(entity).Initialize(0.01f, 0.02f);
            friction.UpdateOrder = 10;

            // These components have to be updated in a specific order to
            // function as intended.
            // Ship control must come first, but after stuff like gravitation,
            // to be able to compute the stabilizer acceleration.
            var shipControl = manager.AddComponent<ShipControl>(entity);
            shipControl.UpdateOrder = 11;

            // Acceleration must come after ship control, due to it setting
            // its value.
            var acceleration = manager.AddComponent<Acceleration>(entity);
            acceleration.UpdateOrder = 12;

            // Velocity must come after acceleration, so that all other forces
            // already have been applied (gravitation).
            var velocity = manager.AddComponent<Velocity>(entity);
            velocity.UpdateOrder = 13;

            // Run weapon control after velocity, to spawn projectiles at the
            // correct position.
            var weaponControl = manager.AddComponent<WeaponControl>(entity);
            weaponControl.UpdateOrder = 14;

            // Energy should be update after it was used, to give it a chance
            // to regenerate (e.g. if we're using less than we produce this
            // avoids always displaying slightly less than max).
            var energy = manager.AddComponent<Energy>(entity);
            energy.UpdateOrder = 15;

            // Same for health.
            var health = manager.AddComponent<Health>(entity).Initialize(120);
            health.UpdateOrder = 15;

            // Update effects last, because they are spawned based on the
            // position.
            var thruster = manager.AddComponent<ThrusterEffect>(entity).Initialize("Effects/thruster");
            thruster.UpdateOrder = 16;

            // Physics related components.
            manager.AddComponent<Transform>(entity).Initialize(position);
            manager.AddComponent<Spin>(entity);

            // Index component, to register with indexes used for other
            // components.
            manager.AddComponent<Index>(entity).Initialize(Gravitation.IndexGroup | Detectable.IndexGroup | faction.ToCollisionIndexGroup());

            // Collision component, to allow colliding with other entities.
            manager.AddComponent<CollidableSphere>(entity).Initialize(CollisionRadius, faction.ToCollisionGroup());

            // Faction component, which allows checking which group the ship
            // belongs to.
            manager.AddComponent<Faction>(entity).Initialize(faction);

            // Controllers for maneuvering and shooting.
            manager.AddComponent<ShipInfo>(entity);

            // Audio and display components.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/ship");

            // Create equipment slots.
            var equipment = manager.AddComponent<Equipment>(entity);
            equipment.SetSlotCount<Sensor>(SensorSlots);
            equipment.SetSlotCount<Armor>(ArmorSlots);
            equipment.SetSlotCount<Reactor>(ReactorSlots);
            equipment.SetSlotCount<Shield>(ShieldSlots);
            equipment.SetSlotCount<Thruster>(ThrusterSlots);
            equipment.SetSlotCount<Weapon>(WeaponSlots);

            // Give it an inventory as well.
            manager.AddComponent<Inventory>(entity).Initialize(10);

            // Add some character!
            manager.AddComponent<Character<AttributeType>>(entity);
            
            // Fill up our values.
            health.SetValue(health.MaxValue);
            energy.SetValue(energy.MaxValue);

            // Do we drop stuff?
            if (ItemPool != null)
            {
                manager.AddComponent<Drops>(entity).Initialize(ItemPool);
            }
                
            return entity;
        }

        #endregion
    }
}
