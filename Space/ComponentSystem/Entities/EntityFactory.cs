using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Entities
{
    class EntityFactory
    {
        /// <summary>
        /// Creates a new ship, player controlled or otherwise, with the
        /// specified parameters.
        /// </summary>
        /// <param name="shipData">The ship info to use.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <returns></returns>
        public static Entity CreateShip(ShipData shipData, Factions faction)
        {
            var entity = new Entity();

            var renderer = new TransformedRenderer(shipData.Texture, faction.ToColor());
            renderer.DrawOrder = 50; //< Draw ships above everything else.
            var modules = new EntityModules<EntityAttributeType>();
            var health = new Health(120);
            var energy = new Energy();

            entity.AddComponent(new Transform(new Vector2(15500, 15500)));
            entity.AddComponent(new Velocity());
            entity.AddComponent(new Spin());
            entity.AddComponent(new Acceleration());
            entity.AddComponent(new Friction(0.01f, 0.02f));
            // TODO compute based on equipped components
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Atractee, 1));
            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup | (ulong)faction << CollisionSystem.FirstIndexGroup));
            entity.AddComponent(new CollidableSphere(shipData.CollisionRadius, (uint)faction));
            entity.AddComponent(new Faction(faction));
            entity.AddComponent(new Avatar(faction.ToPlayerNumber()));
            entity.AddComponent(new ShipControl());
            entity.AddComponent(new WeaponControl());
            entity.AddComponent(new WeaponSound());
            entity.AddComponent(new Radar());
            entity.AddComponent(new ThrusterEffect("Effects/thruster"));
            entity.AddComponent(new Respawn(300, new List<Type>()
            {
                typeof(ShipControl),
                typeof(WeaponControl),
                typeof(CollidableSphere),
                typeof(Acceleration),
                typeof(Gravitation),
                typeof(TransformedRenderer)
            }, new Vector2(15500, 15500)));
            entity.AddComponent(renderer);
            entity.AddComponent(modules);
            entity.AddComponent(health);
            entity.AddComponent(energy);


            // Add after all components are registered to give them the chance
            // to react to the ModuleAdded messages.
            modules.AddModules(shipData.Hulls);
            modules.AddModules(shipData.Reactors);
            modules.AddModules(shipData.Thrusters);
            modules.AddModules(shipData.Shields);
            modules.AddModules(shipData.Weapons);
            modules.AddModules(shipData.Sensors);

            // Set value to max after equipping.
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

            return entity;
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

            if (projectile.Damage > 0)
            {
                entity.AddComponent(new Index((ulong)faction << CollisionSystem.FirstIndexGroup));
            }
            else if (projectile.Damage < 0)
            {
                entity.AddComponent(new Index((ulong)~(uint)faction << CollisionSystem.FirstIndexGroup));
            }
            entity.AddComponent(new CollidableSphere(projectile.CollisionRadius, (uint)faction));
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

        public static Entity CreateAstronomicBody(string texture, Vector2 position, AstronomicBodyType type, float mass)
        {
            var entity = new Entity();

            entity.AddComponent(new Transform(position));
            entity.AddComponent(new Spin());
            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup));
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Atractor, mass));

            entity.AddComponent(new AstronomicBody(type));
            
            //entity.AddComponent(new TransformedRenderer(texture));
            entity.AddComponent(new Effect("Effects/sun2"));

            return entity;
        }

        public static Entity CreateAstronomicBody(string texture, Entity center, float majorRadius, float minorRadius, float angle, int period, AstronomicBodyType type, float mass)
        {
            var entity = new Entity();

            entity.AddComponent(new Transform(center.GetComponent<Transform>().Translation));
            entity.AddComponent(new Spin());
            entity.AddComponent(new EllipsePath(center.UID, majorRadius, minorRadius, angle, period));
            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup));
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Atractor, mass));

            entity.AddComponent(new AstronomicBody(type));

            entity.AddComponent(new PlanetRenderer(texture));

            return entity;
        }
    }
}
