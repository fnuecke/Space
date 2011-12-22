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
            var ship = new Entity();
            ship.AddComponent(new Transform());
            ship.AddComponent(new Acceleration());
            ship.AddComponent(new Friction());
            ship.AddComponent(new Spin());
            ship.AddComponent(new Velocity());
            ship.AddComponent(new CollidableSphere());
            ship.AddComponent(new EntityModules<ShipModule, EntityAttributeType>());
            ship.AddComponent(new WeaponControl());
            ship.AddComponent(new WeaponSlot());
            ship.AddComponent(new ShipControl());
            ship.AddComponent(new Avatar());
            ship.AddComponent(new TransformedRenderer());

            var friction = ship.GetComponent<Friction>();
            friction.Value = (Fixed)0.01;
            friction.MinVelocity = (Fixed)0.02;

            var collidable = ship.GetComponent<CollidableSphere>();
            collidable.Radius = shipData.Radius;

            var modules = ship.GetComponent<EntityModules<ShipModule, EntityAttributeType>>();
            modules.AddModules(shipData.BaseModules);

            var avatar = ship.GetComponent<Avatar>();
            avatar.PlayerNumber = playerNumber;

            var renderer = ship.GetComponent<TransformedRenderer>();
            renderer.TextureName = shipData.Texture;

            return ship;
        }

        public static IEntity CreateShot(WeaponData weaponData, FPoint position, FPoint velocity, FPoint direction)
        {
            var shot = new Entity();
            shot.AddComponent(new Transform());
            shot.AddComponent(new Velocity());
            shot.AddComponent(new Expiration());
            shot.AddComponent(new CollidableSphere());
            shot.AddComponent(new TransformedRenderer());

            // Give this entity a position.
            var sphysics = shot.GetComponent<Transform>();
            sphysics.Translation = position;
            sphysics.Rotation = Fixed.Atan2(direction.Y, direction.X);

            // And a dynamic component for movement and rotation.
            var speed = shot.GetComponent<Velocity>();
            speed.Value = velocity;

            var expiration = shot.GetComponent<Expiration>();
            expiration.TimeToLive = weaponData.ProjectileTTL;

            // Also make it collidable.
            var collidable = shot.GetComponent<CollidableSphere>();
            collidable.Radius = weaponData.ProjectileRadius;

            // And finally, allow it to be rendered.
            var draw = shot.GetComponent<TransformedRenderer>();
            draw.TextureName = weaponData.ProjectileTexture;

            return shot;
        }
    }
}
