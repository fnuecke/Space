using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Math;
using Space.ComponentSystem.Components;
using SpaceData;

namespace Space.ComponentSystem.Entities
{
    public class Ship : AbstractEntity
    {
        public Ship()
        {
            AddComponent(new StaticPhysics());
            AddComponent(new DynamicPhysics());
            AddComponent(new CollidableSphere());
            AddComponent(new MovementProperties());
            AddComponent(new Armament());
            AddComponent(new StaticPhysicsRenderer());
            AddComponent(new ShipControl());
            AddComponent(new Avatar());
        }

        public Ship(ShipData shipData, int playerNumber)
            : this()
        {
            var dphysics = GetComponent<DynamicPhysics>();
            dphysics.Damping = (Fixed)0.01;
            dphysics.MinVelocity = (Fixed)0.005;

            var collidable = GetComponent<CollidableSphere>();
            collidable.Radius = shipData.Radius;

            var movement = GetComponent<MovementProperties>();
            movement.Acceleration = shipData.Acceleration;
            movement.RotationSpeed = shipData.RotationSpeed;

            var renderer = GetComponent<StaticPhysicsRenderer>();
            renderer.TextureName = shipData.Texture;

            var avatar = GetComponent<Avatar>();
            avatar.PlayerNumber = playerNumber;
        }
    }
}
