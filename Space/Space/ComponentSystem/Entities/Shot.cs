using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Math;
using Engine.Serialization;

namespace Space.ComponentSystem.Entities
{
    public class Shot : AbstractEntity
    {
        public Shot()
        {
        }

        public Shot(string textureName, FPoint position, FPoint velocity)
        {
            // Give this entity a position.
            StaticPhysics sphysics = new StaticPhysics();
            sphysics.Position = position;
            sphysics.Rotation = Fixed.Atan2(velocity.Y, velocity.X);

            // And a dynamic component for movement and rotation.
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            dphysics.Velocity = velocity;

            // Also make it collidable.
            CollidableSphere collidable = new CollidableSphere(sphysics);
            collidable.Radius = 5;

            // And finally, allow it to be rendered.
            StaticPhysicsRenderer draw = new StaticPhysicsRenderer(sphysics);
            draw.TextureName = textureName;

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(draw);

            //context.weaponsSounds[name].Play();
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        public override void Depacketize(Packet packet)
        {
            StaticPhysics sphysics = new StaticPhysics();
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            CollidableSphere collidable = new CollidableSphere(sphysics);
            StaticPhysicsRenderer draw = new StaticPhysicsRenderer(sphysics);

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(draw);

            base.Depacketize(packet);
        }
    }
}
