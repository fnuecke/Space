using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Space.ComponentSystem.Components;
using SpaceData;

namespace Space.ComponentSystem.Entities
{
    public class Ship : AbstractEntity
    {
        public Ship()
        {
            components.Add(new StaticPhysics(this));
            components.Add(new DynamicPhysics(this));
            components.Add(new CollidableSphere(this));
            components.Add(new MovementProperties(this));
            components.Add(new Armament(this));
            components.Add(new StaticPhysicsRenderer(this));
            components.Add(new ShipControl(this));
            components.Add(new Avatar(this));
        }

        public Ship(ShipData shipData, int playerNumber)
            : this()
        {
            var dphysics = GetComponent<DynamicPhysics>();
            dphysics.Damping = 0.9;
            dphysics.MinVelocity = 0.01;

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
