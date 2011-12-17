using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Space.ComponentSystem.Components;
using SpaceData;

namespace Space.ComponentSystem.Entities
{
    public class Ship : AbstractEntity
    {
        public Ship()
        {
        }

        public Ship(ShipData shipData, int playerNumber)
        {
            StaticPhysics sphysics = new StaticPhysics();
            
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            dphysics.Damping = 0.99;
            dphysics.MinVelocity = 0.01;

            CollidableSphere collidable = new CollidableSphere(sphysics);
            collidable.Radius = shipData.Radius;

            MovementProperties movement = new MovementProperties();
            movement.Acceleration = shipData.Acceleration;
            movement.RotationSpeed = shipData.RotationSpeed;

            Armament guns = new Armament();

            StaticPhysicsRenderer renderer = new StaticPhysicsRenderer(sphysics);
            renderer.TextureName = shipData.Texture;

            ShipControl input = new ShipControl(dphysics, movement, guns);

            Avatar avatar = new Avatar(this);
            avatar.PlayerNumber = playerNumber;

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(movement);
            components.Add(guns);
            components.Add(renderer);
            components.Add(input);
            components.Add(avatar);
        }

        public override void Depacketize(Packet packet)
        {
            StaticPhysics sphysics = new StaticPhysics();
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            CollidableSphere collidable = new CollidableSphere(sphysics);
            MovementProperties movement = new MovementProperties();
            Armament guns = new Armament();
            StaticPhysicsRenderer renderer = new StaticPhysicsRenderer(sphysics);
            ShipControl input = new ShipControl(dphysics, movement, guns);
            Avatar avatar = new Avatar(this);

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(movement);
            components.Add(guns);
            components.Add(renderer);
            components.Add(input);
            components.Add(avatar);

            base.Depacketize(packet);
        }

        public override object Clone()
        {
            var copy = new Ship();
            // TODO copy component settings
            return copy;
        }
    }
}
