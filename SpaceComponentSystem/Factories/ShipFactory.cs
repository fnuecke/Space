﻿using System;
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
        /// <param name="faction">The faction the ship belongs to.</param>
        /// <param name="position">The position at which to spawn the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public Entity SampleShip(Factions faction, Vector2 position, IUniformRandom random)
        {
            var entity = CreateShip(faction, position);

            // Add our attributes.
            var character = entity.GetComponent<Character<AttributeType>>();
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
        /// <param name="faction"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private Entity CreateShip(Factions faction, Vector2 position)
        {
            var entity = new Entity();

            // Draw ships above everything else.
            var renderer = new TransformedRenderer(Texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            renderer.DrawOrder = 50;

            // Friction has to be updated before acceleration is, to allow
            // maximum speed to be reached.
            var friction = new Friction(0.01f, 0.02f);
            friction.UpdateOrder = 10;

            // These components have to be updated in a specific order to
            // function as intended.
            // Ship control must come first, but after stuff like gravitation,
            // to be able to compute the stabilizer acceleration.
            var shipControl = new ShipControl();
            shipControl.UpdateOrder = 11;

            // Acceleration must come after ship control, due to it setting
            // its value.
            var acceleration = new Acceleration();
            acceleration.UpdateOrder = 12;

            // Velocity must come after acceleration, so that all other forces
            // already have been applied (gravitation).
            var velocity = new Velocity();
            velocity.UpdateOrder = 13;

            // Run weapon control after velocity, to spawn projectiles at the
            // correct position.
            var weaponControl = new WeaponControl();
            weaponControl.UpdateOrder = 14;

            // Energy should be update after it was used, to give it a chance
            // to regenerate (e.g. if we're using less than we produce this
            // avoids always displaying slightly less than max).
            var energy = new Energy();
            energy.UpdateOrder = 15;

            // Same for health.
            var health = new Health(120);
            health.UpdateOrder = 15;

            // Update effects last, because they are spawned based on the
            // position.
            var thruster = new ThrusterEffect("Effects/thruster");
            thruster.UpdateOrder = 16;

            // Physics related components.
            entity.AddComponent(new Transform(position));
            entity.AddComponent(velocity);
            entity.AddComponent(new Spin());
            entity.AddComponent(acceleration);
            entity.AddComponent(friction);
            entity.AddComponent(new ShipGravitation());

            // Index component, to register with indexes used for other
            // components.
            entity.AddComponent(new Index(Gravitation.IndexGroup | Detectable.IndexGroup | faction.ToCollisionIndexGroup()));

            // Collision component, to allow colliding with other entities.
            entity.AddComponent(new CollidableSphere(CollisionRadius, faction.ToCollisionGroup()));

            // Faction component, which allows checking which group the ship
            // belongs to.
            entity.AddComponent(new Faction(faction));

            // Controllers for maneuvering and shooting.
            entity.AddComponent(shipControl);
            entity.AddComponent(weaponControl);
            entity.AddComponent(new ShipInfo());

            // Audio and display components.
            entity.AddComponent(new WeaponSound());
            entity.AddComponent(new Detectable("Textures/ship"));
            entity.AddComponent(thruster);
            entity.AddComponent(renderer);

            // Other game logic related components.
            entity.AddComponent(health);
            entity.AddComponent(energy);

            // Create equipment slots.
            var equipment = new Equipment();
            equipment.SetSlotCount<Sensor>(SensorSlots);
            equipment.SetSlotCount<Armor>(ArmorSlots);
            equipment.SetSlotCount<Reactor>(ReactorSlots);
            equipment.SetSlotCount<Shield>(ShieldSlots);
            equipment.SetSlotCount<Thruster>(ThrusterSlots);
            equipment.SetSlotCount<Weapon>(WeaponSlots);
            entity.AddComponent(equipment);

            // Give it an inventory as well.
            entity.AddComponent(new Inventory(10));

            // Add some character!
            entity.AddComponent(new Character<AttributeType>());
            
            // Fill up our values.
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

            if (ItemPool != null)
            {
               
                entity.AddComponent(new Drops {ItemPool = ItemPool});
            }
                
            return entity;
        }

        #endregion
    }
}
