using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Data;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Entities
{
    class EntityFactory
    {
        /// <summary>
        /// Creates a new, player controlled ship.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="playerNumber">The player for whom to create the ship.</param>
        /// <returns>The new ship.</returns>
        public static Entity CreatePlayerShip(ShipData shipData, int playerNumber)
        {
            Entity entity = CreateShip(shipData, playerNumber.ToFaction());

            entity.AddComponent(new Avatar(playerNumber));
            entity.AddComponent(new Respawn(300, new HashSet<Type>()
            {
                typeof(ShipControl),
                typeof(WeaponControl),
                typeof(CollidableSphere),
                typeof(Acceleration),
                typeof(Gravitation),
                typeof(TransformedRenderer),
                typeof(ThrusterEffect)
            }, new Vector2(15500, 15500)));

            return entity;
        }

        /// <summary>
        /// Creates a new, AI controlled ship.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <returns>The new ship.</returns>
        public static Entity CreateAIShip(ShipData shipData, Factions faction)
        {
            Entity entity = CreateShip(shipData, faction);

            entity.AddComponent(new AIComponent());
            entity.AddComponent(new Death());
            
            return entity;
        }

        /// <summary>
        /// Creates a new ship with the specified parameters.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <returns>The new ship.</returns>
        private static Entity CreateShip(ShipData shipData, Factions faction)
        {
            var entity = new Entity();

            var renderer = new TransformedRenderer(shipData.Texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            renderer.DrawOrder = 50; //< Draw ships above everything else.
            var modules = new EntityModules<EntityAttributeType>();
            var health = new Health(120);
            var energy = new Energy();

            entity.AddComponent(new Transform(new Vector2(36000, 38000)));
            entity.AddComponent(new Velocity());
            entity.AddComponent(new Spin());
            entity.AddComponent(new Acceleration());
            entity.AddComponent(new Friction(0.01f, 0.02f));
            // TODO compute based on equipped components
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Atractee, 10));
            entity.AddComponent(new Index(Gravitation.IndexGroup | Detectable.IndexGroup | faction.ToCollisionIndexGroup()));
            entity.AddComponent(new CollidableSphere(shipData.CollisionRadius, faction.ToCollisionGroup()));
            entity.AddComponent(new Faction(faction));
            entity.AddComponent(new ShipControl());
            entity.AddComponent(new WeaponControl());
            entity.AddComponent(new WeaponSound());
            entity.AddComponent(new Detectable("Textures/ship"));
            entity.AddComponent(new ThrusterEffect("Effects/thruster"));
            entity.AddComponent(renderer);
            entity.AddComponent(modules);
            entity.AddComponent(health);
            entity.AddComponent(energy);

            // Add after all components are registered to give them the chance
            // to react to the ModuleAdded messages.
            modules.AddModules(ModuleArrayCopy(shipData.Hulls));
            modules.AddModules(ModuleArrayCopy(shipData.Reactors));
            modules.AddModules(ModuleArrayCopy(shipData.Thrusters));
            modules.AddModules(ModuleArrayCopy(shipData.Shields));
            modules.AddModules(ModuleArrayCopy(shipData.Weapons));
            modules.AddModules(ModuleArrayCopy(shipData.Sensors));

            // Set value to max after equipping.
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

            return entity;
        }

        /// <summary>
        /// Copies modules from module array of a ShipData instance.
        /// </summary>
        /// <typeparam name="T">The type of modules to copy.</typeparam>
        /// <param name="array">The array to copy.</param>
        /// <returns>A copy of the array.</returns>
        private static T[] ModuleArrayCopy<T>(T[] array) where T : AbstractEntityModule<EntityAttributeType>
        {
            if (array == null)
            {
                return null;
            }

            var copy = new T[array.Length];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = (T)array[i].DeepCopy();
            }
            return copy;
        }

        /// <summary>
        /// Creates a new projectile, which is an entity that does damage on
        /// impact. Its physical properties are determined by the specified
        /// data. The emitter is used to determine the starting position and
        /// velocity, if available.
        /// </summary>
        /// <remarks>
        /// The emitter must have a <c>Transform</c> component. If it has a
        /// <c>Velocity</c> component, it will be added to the starting
        /// velocity of the projectile.
        /// </remarks>
        /// <param name="projectile"></param>
        /// <param name="emitter"></param>
        /// <param name="faction"></param>
        /// <returns></returns>
        public static Entity CreateProjectile(ProjectileData projectile, Entity emitter, Factions faction)
        {
            var entity = new Entity();

            var emitterTransform = emitter.GetComponent<Transform>();
            var emitterVelocity = emitter.GetComponent<Velocity>();

            var transform = new Transform(emitterTransform.Translation, emitterTransform.Rotation + projectile.InitialRotation);
            entity.AddComponent(transform);

            var velocity = new Velocity(Rotate(projectile.InitialVelocity, transform.Rotation));
            if (emitterVelocity != null)
            {
                velocity.Value += emitterVelocity.Value;
            }
            entity.AddComponent(velocity);
            if (projectile.AccelerationForce > 0)
            {
                entity.AddComponent(new Acceleration(projectile.AccelerationForce * Rotate(Vector2.UnitX, transform.Rotation)));
            }
            if (projectile.Friction > 0)
            {
                entity.AddComponent(new Friction(projectile.Friction));
            }
            if (projectile.Spin > 0)
            {
                entity.AddComponent(new Spin(projectile.Spin));
            }
            if (projectile.TimeToLive > 0)
            {
                entity.AddComponent(new Expiration(projectile.TimeToLive));
            }
            if (projectile.Damage != 0)
            {
                entity.AddComponent(new CollisionDamage(projectile.Damage));
            }

            if (projectile.Damage >= 0)
            {
                entity.AddComponent(new Index(faction.ToCollisionIndexGroup()));
            }
            else if (projectile.Damage < 0)
            {
                // Negative damage = healing -> collide will all our allies.
                entity.AddComponent(new Index(faction.Inverse().ToCollisionIndexGroup()));
            }
            entity.AddComponent(new CollidableSphere(projectile.CollisionRadius, faction.ToCollisionGroup()));
            if (!string.IsNullOrWhiteSpace(projectile.Texture))
            {
                entity.AddComponent(new TransformedRenderer(projectile.Texture, faction.ToColor(), projectile.Scale));
            }
            if (!string.IsNullOrWhiteSpace(projectile.Effect))
            {
                // TODO
            }

            return entity;
        }

        private static Vector2 Rotate(Vector2 f, float angle)
        {
            Vector2 result;
            var cos = (float)System.Math.Cos(angle);
            var sin = (float)System.Math.Sin(angle);
            result.X = f.X * cos - f.Y * sin;
            result.Y = f.X * sin + f.Y * cos;
            return result;
        }

        public static Entity CreateFixedAstronomicalObject(
            string texture, 
            Vector2 position,
            AstronomicBodyType type,
            float mass)
        {
            var entity = new Entity();

            entity.AddComponent(new Transform(position));
            entity.AddComponent(new Spin());
            entity.AddComponent(new Index(Gravitation.IndexGroup | Detectable.IndexGroup | Factions.None.ToCollisionIndexGroup()));
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Attractor, mass));

            entity.AddComponent(new CollidableSphere(256, Factions.None.ToCollisionGroup()));
            entity.AddComponent(new CollisionDamage(1, float.MaxValue));

            entity.AddComponent(new Detectable("Textures/radar_sun"));
            entity.AddComponent(new AstronomicBody(type));
            
            //entity.AddComponent(new TransformedRenderer(texture));
            entity.AddComponent(new Effect("Effects/sun2"));

            return entity;
        }

        public static Entity CreateOrbitingAstronomicalObject(
            string texture,
            Entity center,
            float majorRadius,
            float minorRadius,
            float angle,
            float period,
            float periodOffset,
            AstronomicBodyType type,
            float mass)
        {
            if (period < 1)
            {
                throw new ArgumentException("Period must be greater than zero.", "period");
            }
            var entity = new Entity();

            entity.AddComponent(new Transform(center.GetComponent<Transform>().Translation));
            entity.AddComponent(new Spin());
            entity.AddComponent(new EllipsePath(center.UID, majorRadius, minorRadius, angle, period, periodOffset));
            entity.AddComponent(new Index(Gravitation.IndexGroup | Detectable.IndexGroup));
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Attractor, mass));

            entity.AddComponent(new AstronomicBody(type));
            entity.AddComponent(new Detectable("Textures/radar_planet"));
            entity.AddComponent(new PlanetRenderer(texture));

            return entity;
        }
    }
}
