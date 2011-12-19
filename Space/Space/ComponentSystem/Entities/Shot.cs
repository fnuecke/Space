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
            AddComponent(new StaticPhysics());
            AddComponent(new DynamicPhysics());
            AddComponent(new CollidableSphere());
            AddComponent(new StaticPhysicsRenderer());
        }

        public Shot(WeaponData weaponData, FPoint position, FPoint velocity)
            : this()
        {
            // Give this entity a position.
            var sphysics = GetComponent<StaticPhysics>();
            sphysics.Position = position;
            sphysics.Rotation = Fixed.Atan2(velocity.Y, velocity.X);

            // And a dynamic component for movement and rotation.
            var dphysics = GetComponent<DynamicPhysics>();
            dphysics.Velocity = velocity;

            // Also make it collidable.
            var collidable = GetComponent<CollidableSphere>();
            collidable.Radius = weaponData.ProjectileRadius;

            // And finally, allow it to be rendered.
            var draw = GetComponent<StaticPhysicsRenderer>();
            draw.TextureName = weaponData.ProjectileTexture;
        }
    }
}
