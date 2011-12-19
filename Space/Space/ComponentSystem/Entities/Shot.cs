using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Math;
using SpaceData;

namespace Space.ComponentSystem.Entities
{
    public class Shot : AbstractEntity
    {
        public Shot()
        {
            AddComponent(new Transform());
            AddComponent(new Velocity());
            AddComponent(new Physics());

            AddComponent(new CollidableSphere());

            AddComponent(new TransformedRenderer());
        }

        public Shot(WeaponData weaponData, FPoint position, FPoint velocity)
            : this()
        {
            // Give this entity a position.
            var sphysics = GetComponent<Transform>();
            sphysics.Translation = position;
            sphysics.Rotation = Fixed.Atan2(velocity.Y, velocity.X);

            // And a dynamic component for movement and rotation.
            var speed = GetComponent<Velocity>();
            speed.Value = velocity;

            // Also make it collidable.
            var collidable = GetComponent<CollidableSphere>();
            collidable.Radius = weaponData.ProjectileRadius;

            // And finally, allow it to be rendered.
            var draw = GetComponent<TransformedRenderer>();
            draw.TextureName = weaponData.ProjectileTexture;
        }
    }
}
