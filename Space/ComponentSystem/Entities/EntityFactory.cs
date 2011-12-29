﻿using Engine.ComponentSystem.Components;
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
        /// <param name="fraction">The faction the ship will belong to.</param>
        /// <returns></returns>
        public static IEntity CreateShip(ShipData shipData, Fractions fraction)
        {
            var entity = new Entity();

            var modules = new EntityModules<EntityAttributeType>();
            var health = new Health();
            var energy = new Energy();

            entity.AddComponent(new Transform(new Vector2(16000, 16000)));
            entity.AddComponent(new Velocity());
            entity.AddComponent(new Spin());
            entity.AddComponent(new Acceleration());
            entity.AddComponent(new Friction(0.01f, 0.02f));
            // TODO compute based on equipped components
            entity.AddComponent(new Gravitation(Gravitation.GravitationTypes.Atractee, 1));
            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup | (ulong)fraction << CollisionSystem.FirstIndexGroup));
            entity.AddComponent(new CollidableSphere(shipData.CollisionRadius, (uint)fraction));
            entity.AddComponent(new Fraction(fraction));
            entity.AddComponent(new Avatar(fraction.ToPlayerNumber()));
            entity.AddComponent(new ShipControl());
            entity.AddComponent(new WeaponControl());
            entity.AddComponent(new WeaponSound());
            entity.AddComponent(new TransformedRenderer(shipData.Texture));
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

            // Set value to max after equipping.
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

            return entity;
        }

        public static IEntity CreateProjectile(ProjectileData projectile, IEntity emitter, Fractions fraction)
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
                entity.AddComponent(new Index((ulong)fraction << CollisionSystem.FirstIndexGroup));
            }
            else if (projectile.Damage < 0)
            {
                entity.AddComponent(new Index((ulong)~(uint)fraction << CollisionSystem.FirstIndexGroup));
            }
            entity.AddComponent(new CollidableSphere(projectile.CollisionRadius, (uint)fraction));
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

        public static IEntity CreateStar(string texture, Vector2 position, AstronomicBodyType type, float mass)
        {
            var entity = new Entity();

            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup));

            var transform = new Transform();
            transform.Translation = position;
            entity.AddComponent(transform);

            entity.AddComponent(new Spin());

            var renderer = new TransformedRenderer();
            renderer.TextureName = texture;
            entity.AddComponent(renderer);

            var grav = new Gravitation();
            grav.GravitationType = Gravitation.GravitationTypes.Atractor;
            grav.Mass = mass;
            entity.AddComponent(grav);

            var astronomicBody = new AstronomicBody();
            astronomicBody.Type = type;
            entity.AddComponent(astronomicBody);
            return entity;
        }

        public static IEntity CreateStar(string texture, IEntity center, float majorRadius, float minorRadius, float angle, int period, AstronomicBodyType type, float mass)
        {
            var entity = new Entity();

            var transform = new Transform();
            transform.Translation = center.GetComponent<Transform>().Translation;
            entity.AddComponent(transform);

            entity.AddComponent(new Spin());

            var ellipse = new EllipsePath();
            ellipse.CenterEntityId = center.UID;
            ellipse.MajorRadius = majorRadius;
            ellipse.MinorRadius = minorRadius;
            ellipse.Angle = angle;
            ellipse.Period = period;
            entity.AddComponent(ellipse);

            entity.AddComponent(new Index(1ul << Gravitation.IndexGroup));

            var renderer = new PlanetRenderer();
            renderer.TextureName = texture;
            entity.AddComponent(renderer);

            var grav = new Gravitation();
            grav.GravitationType = Gravitation.GravitationTypes.Atractor;
            grav.Mass = mass;
            entity.AddComponent(grav);

            var astronomicBody = new AstronomicBody();
            astronomicBody.Type = type;
            entity.AddComponent(astronomicBody);
            return entity;

        }
    }
}
