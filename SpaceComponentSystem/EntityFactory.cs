using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.ComponentSystem.Util;
using Space.Data;

namespace Space.ComponentSystem
{
    public static class EntityFactory
    {
        /// <summary>
        /// Creates a new, player controlled ship.
        /// </summary>
        /// <param name="playerClass">The player's class, which determines the blueprint.</param>
        /// <param name="playerNumber">The player for whom to create the ship.</param>
        /// <param name="position">The position at which to spawn and respawn the ship.</param>
        /// <returns>The new ship.</returns>
        public static Entity CreatePlayerShip(PlayerClassType playerClass, int playerNumber, Vector2 position)
        {
            // Player ships must be 'static', i.e. not have random attributes, so we don't need a randomizer.
            Entity entity = FactoryLibrary.SampleShip(playerClass.GetShipFactoryName(), playerNumber.ToFaction(), position, null);

            // Remember the class.
            entity.AddComponent(new PlayerClass(playerClass));

            // Mark it as the player's avatar.
            entity.AddComponent(new Avatar(playerNumber));

            // Make it respawn.
            entity.AddComponent(new Respawn(300, new HashSet<Type>()
            {
                // Make ship uncontrollable.
                typeof(ShipControl),
                typeof(WeaponControl),
                // Disable collisions.
                typeof(CollidableSphere),
                // And movement.
                typeof(Acceleration),
                typeof(Gravitation),
                // Hide it.
                typeof(TransformedRenderer),
                typeof(ThrusterEffect),
                // And don't regenerate.
                typeof(Health),
                typeof(Energy)
            }, position));

            return entity;
        }

        /// <summary>
        /// Creates a new, AI controlled ship.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <returns>The new ship.</returns>
        public static Entity CreateAIShip(string blueprint, Factions faction, Vector2 position, IEntityManager manager, IUniformRandom random, AiComponent.AiCommand command)
        {
            Entity entity = FactoryLibrary.SampleShip(blueprint, faction, position, random);

            // Add to the index from which entities will automatically removed on cell death.
            entity.GetComponent<Index>().IndexGroups |=  CellSystem.CellDeathAutoRemoveIndex;

            var input = entity.GetComponent<ShipControl>();
            input.Stabilizing = true;
            entity.AddComponent(new AiComponent(command));
            entity.AddComponent(new Death());
            entity.AddComponent(new VoidDeath());

            var equipment = entity.GetComponent<Equipment>();

            var item = FactoryLibrary.SampleItem("L1_AI_Thruster", random);
            manager.AddEntity(item);
            equipment.Equip(item, 0);

            item = FactoryLibrary.SampleItem("L1_AI_Reactor", random);
            manager.AddEntity(item);
            equipment.Equip(item, 0);

            item = FactoryLibrary.SampleItem("L1_AI_Armor", random);
            manager.AddEntity(item);
            equipment.Equip(item, 0);

            item = FactoryLibrary.SampleItem("L1_AI_Weapon", random);
            manager.AddEntity(item);
            equipment.Equip(item, 0);

            return entity;
        }

        /// <summary>
        /// Creates a new explosion effect at the specified position.
        /// </summary>
        /// <param name="position">The position at which to show the explosion.</param>
        /// <param name="scale">The Diameter of the Explosion</param>
        /// <returns>The entity representing the explosion.</returns>
        public static Entity CreateExplosion(Vector2 position,float scale = 0)
        {
            Entity entity = new Entity();

            entity.AddComponent(new Transform(position));
            entity.AddComponent(new ExplosionEffect(scale));

            return entity;
        }

        /// <summary>
        /// Creates a new sun.
        /// </summary>
        /// <param name="radius">The radius of the sun, for rendering and
        /// collision detection.</param>
        /// <param name="position">The position of the sun.</param>
        /// <param name="mass">The mass of this sun.</param>
        /// <returns>A new sun.</returns>
        public static Entity CreateSun(
            float radius,
            Vector2 position,
            float mass)
        {
            var entity = new Entity();

            entity.AddComponent(new Transform(position));
            entity.AddComponent(new Spin());
            entity.AddComponent(new Index(Detectable.IndexGroup | CellSystem.CellDeathAutoRemoveIndex
                | Factions.Nature.ToCollisionIndexGroup()));
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Attractor, mass));

            entity.AddComponent(new CollidableSphere(radius, Factions.Nature.ToCollisionGroup()));
            entity.AddComponent(new CollisionDamage(1, float.MaxValue));

            entity.AddComponent(new Detectable("Textures/Radar/Icons/radar_sun"));
            
            entity.AddComponent(new SunRenderer(radius));

            return entity;
        }

        /// <summary>
        /// Creates a new orbiting astronomical object, such as a planet or
        /// moon.
        /// </summary>
        /// <param name="texture">The texture to use for the object.</param>
        /// <param name="planetTint">The color tint for the texture.</param>
        /// <param name="radius">The radius for rendering and collision detection.</param>
        /// <param name="rotationDirection">The rotation direction.</param>
        /// <param name="atmosphereTint">The atmosphere tint.</param>
        /// <param name="center">The center entity we're orbiting.</param>
        /// <param name="majorRadius">The major radius of the orbit ellipse.</param>
        /// <param name="minorRadius">The minor radius of the orbit ellipse.</param>
        /// <param name="angle">The angle of the orbit ellipse.</param>
        /// <param name="period">The orbiting period.</param>
        /// <param name="periodOffset">The period offset.</param>
        /// <param name="mass">The mass of the entity, for gravitation.</param>
        /// <returns></returns>
        public static Entity CreateOrbitingAstronomicalObject(
            string texture,
            Color planetTint,
            float radius,
            Color atmosphereTint,
            float rotationSpeed,
            Entity center,
            float majorRadius,
            float minorRadius,
            float angle,
            float period,
            float periodOffset,
            float mass)
        {
            if (period < 1)
            {
                throw new ArgumentException("Period must be greater than zero.", "period");
            }

            var entity = new Entity();

            entity.AddComponent(new Transform(center.GetComponent<Transform>().Translation));
            entity.AddComponent(new Spin(rotationSpeed));
            entity.AddComponent(new EllipsePath(center.UID, majorRadius, minorRadius, angle, period, periodOffset));
            entity.AddComponent(new Index(Detectable.IndexGroup | CellSystem.CellDeathAutoRemoveIndex));
            if (mass > 0)
            {
                entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Attractor, mass));
            }

            entity.AddComponent(new Detectable("Textures/Radar/Icons/radar_planet"));
            entity.AddComponent(new PlanetRenderer(texture, planetTint, radius, atmosphereTint));

            return entity;
        }

        /// <summary>
        /// Creates a new space station.
        /// </summary>
        /// <param name="texture">The texture to use for rending the station.</param>
        /// <param name="center">The center entity we're orbiting.</param>
        /// <param name="orbitRadius">The orbit radius of the station.</param>
        /// <param name="period">The orbiting period.</param>
        /// <param name="periodOffset">The periodoffset.</param>
        /// <param name="faction">The faction the station belongs to.</param>
        /// <returns></returns>
        public static Entity CreateStation(
            String texture,
            Entity center,
            float orbitRadius,
            float period,
            Factions faction)
        {
            var entity = new Entity();

            var renderer = new TransformedRenderer(texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            //var health = new Health(120);
            //entity.AddComponent(health);
            //entity.AddComponent(new Death());
            entity.AddComponent(new Faction(faction));
            entity.AddComponent(new Transform(center.GetComponent<Transform>().Translation));
            entity.AddComponent(new Spin(((float)Math.PI) / period));
            entity.AddComponent(new EllipsePath(center.UID, orbitRadius, orbitRadius, 0, period, 0));
            entity.AddComponent(new Index(Detectable.IndexGroup | CellSystem.CellDeathAutoRemoveIndex));
            entity.AddComponent(new Detectable("Textures/Stolen/Ships/sensor_array_dish"));
            entity.AddComponent(new ShipSpawner());
            entity.AddComponent(renderer);

            return entity;
        }
    }
}
