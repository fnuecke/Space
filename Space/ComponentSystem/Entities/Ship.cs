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
            AddComponent(new Transform());
            AddComponent(new Velocity());
            AddComponent(new Spin());
            AddComponent(new Acceleration());
            AddComponent(new Friction());
            AddComponent(new Physics());

            AddComponent(new CollidableSphere());

            AddComponent(new Armament());
            AddComponent(new MovementProperties());
            AddComponent(new ShipControl());

            AddComponent(new Avatar());

            AddComponent(new TransformedRenderer());
        }

        public Ship(ShipData shipData, int playerNumber)
            : this()
        {
            var friction = GetComponent<Friction>();
            friction.Value = (Fixed)0.01;
            friction.MinVelocity = (Fixed)0.005;

            var collidable = GetComponent<CollidableSphere>();
            collidable.Radius = shipData.Radius;

            var movement = GetComponent<MovementProperties>();
            movement.Acceleration = shipData.Acceleration;
            movement.RotationSpeed = shipData.RotationSpeed;

            var renderer = GetComponent<TransformedRenderer>();
            renderer.TextureName = shipData.Texture;

            var avatar = GetComponent<Avatar>();
            avatar.PlayerNumber = playerNumber;
        }
    }
}
