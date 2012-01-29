using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Modules;
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
        public static Entity CreatePlayerShip(ShipData shipData, int playerNumber, Vector2 position)
        {
            Entity entity = CreateShip(shipData, playerNumber.ToFaction(), ref position);

            entity.AddComponent(new Avatar(playerNumber));
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
        public static Entity CreateAIShip(ShipData shipData, Factions faction, Vector2 position, AiComponent.AiCommand command)
        {
            Entity entity = CreateShip(shipData, faction, ref position);

            var input = entity.GetComponent<ShipControl>();
            input.Stabilizing = true;
            entity.AddComponent(new AiComponent(command));
            entity.AddComponent(new Death());
            entity.AddComponent(new CellChangedComponent());
            return entity;
        }

        /// <summary>
        /// Creates a new explosion effect at the specified position.
        /// </summary>
        /// <param name="position">The position at which to show the explosion.</param>
        /// <param name="scale">The Scale of the Explosion</param>
        /// <returns>The entity representing the explosion.</returns>
        public static Entity CreateExplosion(Vector2 position,float scale = 0)
        {
            Entity entity = new Entity();

            entity.AddComponent(new Transform(position));
            entity.AddComponent(new ExplosionEffect(scale));

            return entity;
        }

        /// <summary>
        /// Creates a new ship with the specified parameters.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <returns>The new ship.</returns>
        private static Entity CreateShip(ShipData shipData, Factions faction, ref Vector2 position)
        {
            var entity = new Entity();

            var renderer = new TransformedRenderer(shipData.Texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            renderer.DrawOrder = 50; //< Draw ships above everything else.
            var modules = new ModuleManager<SpaceModifier>();
            var health = new Health(120);

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
            entity.AddComponent(new CollidableSphere(shipData.CollisionRadius, faction.ToCollisionGroup()));

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
        private static T[] ModuleArrayCopy<T>(T[] array) where T : AbstractModule<SpaceModifier>
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

            ulong collisionIndexGroup = 0;
            if (!projectile.CanBeShot)
            {
                collisionIndexGroup = Factions.Projectiles.ToCollisionIndexGroup();
            }
            if (projectile.Damage >= 0)
            {
                collisionIndexGroup |= faction.ToCollisionIndexGroup();
            }
            else if (projectile.Damage < 0)
            {
                // Negative damage = healing -> collide will all our allies.
                collisionIndexGroup |= faction.Inverse().ToCollisionIndexGroup();
            }
            entity.AddComponent(new Index(collisionIndexGroup));
            uint collisionGroup = faction.ToCollisionGroup();
            if (!projectile.CanBeShot)
            {
                collisionGroup |= Factions.Projectiles.ToCollisionGroup();
            }
            entity.AddComponent(new CollidableSphere(projectile.CollisionRadius, collisionGroup));
            if (!string.IsNullOrWhiteSpace(projectile.Texture))
            {
                entity.AddComponent(new TransformedRenderer(projectile.Texture, projectile.Scale));
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
            entity.AddComponent(new Index(Detectable.IndexGroup | Factions.Nature.ToCollisionIndexGroup()));
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
            entity.AddComponent(new Index(Detectable.IndexGroup));
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
            entity.AddComponent(new Index(Detectable.IndexGroup));
            entity.AddComponent(new Detectable("Textures/Stolen/Ships/sensor_array_dish"));
            entity.AddComponent(new SpawnComponent());
            entity.AddComponent(renderer);

            return entity;
        }
    }
}
