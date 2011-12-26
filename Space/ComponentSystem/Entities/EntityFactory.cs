using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Math;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Entities
{
    class EntityFactory
    {
        public static IEntity CreateShip(ShipData shipData, int playerNumber)
        {
            var entity = new Entity();

            var transform = new Transform();
            transform.Translation = FPoint.Create((Fixed)2000, (Fixed)2000);
            entity.AddComponent(transform);

            var friction = new Friction();
            friction.Value = (Fixed)0.01;
            friction.MinVelocity = (Fixed)0.02;
            entity.AddComponent(friction);

            var collidable = new CollidableSphere();
            collidable.Radius = shipData.Radius;
            collidable.CollisionGroup = playerNumber;
            entity.AddComponent(collidable);

            var modules = new EntityModules<EntityAttributeType>();
            entity.AddComponent(modules);

            var avatar = new Avatar();
            avatar.PlayerNumber = playerNumber;
            entity.AddComponent(avatar);

            var renderer = new TransformedRenderer();
            renderer.TextureName = shipData.Texture;
            entity.AddComponent(renderer);

            entity.AddComponent(new Acceleration());
            entity.AddComponent(new Spin());
            entity.AddComponent(new Velocity());
            entity.AddComponent(new WeaponControl());
            entity.AddComponent(new WeaponSound());
            entity.AddComponent(new ShipControl());
            entity.AddComponent(new Index());

            // Add before modules to get proper values.
            var health = new Health();
            entity.AddComponent(health);
            var energy = new Energy();
            entity.AddComponent(energy);

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

        public static IEntity CreateProjectile(IEntity emitter, ProjectileData projectile)
        {
            var entity = new Entity();

            // Give the projectile its position.
            var transform = new Transform();
            var emitterTransform = emitter.GetComponent<Transform>();
            if (emitterTransform != null)
            {
                transform.Translation = emitterTransform.Translation;
                transform.Rotation = emitterTransform.Rotation;
            }
            entity.AddComponent(transform);

            // Make it visible.
            if (!string.IsNullOrWhiteSpace(projectile.Texture))
            {
                var renderer = new TransformedRenderer();
                renderer.TextureName = projectile.Texture;
                entity.AddComponent(renderer);
            }

            // Give it its initial velocity.
            var velocity = new Velocity();
            if (projectile.InitialVelocity != Fixed.Zero)
            {
                FPoint rotation = FPoint.Create((Fixed)1, (Fixed)0);
                if (emitterTransform != null)
                {
                    rotation = FPoint.Rotate(rotation, transform.Rotation);
                }
                velocity.Value = rotation * projectile.InitialVelocity;
            }
            var emitterVelocity = emitter.GetComponent<Velocity>();
            if (emitterVelocity != null)
            {
                velocity.Value += emitterVelocity.Value;
            }
            entity.AddComponent(velocity);

            // Make it collidable.
            var collidable = new CollidableSphere();
            collidable.Radius = projectile.CollisionRadius;
            var avatar = emitter.GetComponent<Avatar>();
            if (avatar != null)
            {
                collidable.CollisionGroup = avatar.PlayerNumber;
            }
            entity.AddComponent(collidable);

            // Give it some friction.
            if (projectile.Friction > 0)
            {
                var friction = new Friction();
                friction.Value = projectile.Friction;
                entity.AddComponent(friction);
            }

            // Make it expire after some time.
            if (projectile.TimeToLive > 0)
            {
                var expiration = new Expiration();
                expiration.TimeToLive = projectile.TimeToLive;
                entity.AddComponent(expiration);
            }

            // The damage it does.
            if (projectile.Damage != 0)
            {
                var damage = new CollisionDamage();
                damage.Damage = projectile.Damage;
                entity.AddComponent(damage);
            }

            // Make it indexable.
            entity.AddComponent(new Index());

            return entity;
        }

        public static IEntity CreateStar(string texture, FPoint position, AstronomicBodyType type)
        {
            var entity = new Entity();

            var transform = new Transform();
            transform.Translation = position;
            entity.AddComponent(transform);

            var renderer = new TransformedRenderer();
            renderer.TextureName = texture;
            entity.AddComponent(renderer);


            var astronomicBody = new AstronomicBody();
            astronomicBody.Type = type;
            entity.AddComponent(astronomicBody);
            return entity;
        }

        public static IEntity CreateStar(string texture, IEntity center, Fixed majorRadius, Fixed minorRadius, Fixed angle, int period, AstronomicBodyType type)
        {
            var entity = new Entity();

            var transform = new Transform();
            transform.Translation = center.GetComponent<Transform>().Translation;
            entity.AddComponent(transform);

            var ellipse = new EllipsePathComponent();
            ellipse.CenterEntityId = center.UID;
            ellipse.MajorRadius = majorRadius;
            ellipse.MinorRadius = minorRadius;
            ellipse.Angle = angle;
            ellipse.Period = period;
            entity.AddComponent(ellipse);

            var renderer = new TransformedRenderer();
            renderer.TextureName = texture;
            entity.AddComponent(renderer);

            var astronomicBody = new AstronomicBody();
            astronomicBody.Type = type;
            entity.AddComponent(astronomicBody);
            return entity;
        }
    }
}
