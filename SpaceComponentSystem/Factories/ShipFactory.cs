using System;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
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
            var character = ((Character<AttributeType>)manager.GetComponent(entity, Character<AttributeType>.TypeId));
            foreach (var attribute in Attributes)
            {
                var modifier = attribute.SampleAttributeModifier(random);
                if (modifier.ComputationType == AttributeComputationType.Multiplicative)
                {
                    throw new InvalidOperationException("Base attributes must be additive.");
                }
                character.SetBaseValue(modifier.Type, modifier.Value);
            }

            // Fill up our values.
            var health = ((Health)manager.GetComponent(entity, Health.TypeId));
            var energy = ((Energy)manager.GetComponent(entity, Energy.TypeId));
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

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

            manager.AddComponent<Transform>(entity).Initialize(position);
            manager.AddComponent<Friction>(entity).Initialize(0.01f, 0.02f);
            manager.AddComponent<Acceleration>(entity);
            manager.AddComponent<Gravitation>(entity).Initialize();
            manager.AddComponent<Velocity>(entity);
            manager.AddComponent<Spin>(entity);
            manager.AddComponent<ShipControl>(entity);
            manager.AddComponent<WeaponControl>(entity);
            manager.AddComponent<Energy>(entity);
            manager.AddComponent<Health>(entity).Initialize(120);
            manager.AddComponent<TextureRenderer>(entity).Initialize(Texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            manager.AddComponent<ParticleEffects>(entity);

            // Collision component, to allow colliding with other entities.
            manager.AddComponent<CollidableSphere>(entity)
                .Initialize(CollisionRadius, faction.ToCollisionGroup());

            // Faction component, which allows checking which group the ship
            // belongs to.
            manager.AddComponent<Faction>(entity).Initialize(faction);

            // Make it detectable by AI and show up on the radar.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/ship", true);

            // Controllers for maneuvering and shooting.
            manager.AddComponent<ShipInfo>(entity);

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

            // Do we drop stuff?
            if (ItemPool != null)
            {
                manager.AddComponent<Drops>(entity).Initialize(ItemPool);
            }

            // The the sound component for the thruster sound.
            manager.AddComponent<Sound>(entity).Initialize("Thruster");

            // Index component, to register with indexes used for other
            // components.
            manager.AddComponent<Index>(entity).Initialize(
                CollisionSystem.IndexGroupMask | // Can bump into other stuff.
                DetectableSystem.IndexGroupMask | // Can be detected.
                GravitationSystem.IndexGroupMask | // Can be attracted.
                SoundSystem.IndexGroupMask | // Can make noise.
                CullingTextureRenderSystem.IndexGroupMask,
                (int)(CollisionRadius + CollisionRadius));

            return entity;
        }

        #endregion
    }
}
