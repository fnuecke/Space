using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Space.ComponentSystem.Components;
using SpaceData;

namespace Space.ComponentSystem.Entities
{
    public class Ship : AbstractEntity
    {
        public int PlayerNumber { get; private set; }

        public ShipData Data { get; private set; }

        private bool shooting;

        private int shotCooldown;

        public Ship()
        {
            this.Data = new ShipData();
        }

        public Ship(ShipData shipData, int player)
        {
            this.Data = shipData;
            this.PlayerNumber = player;

            StaticPhysics sphysics = new StaticPhysics();
            
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            dphysics.Damping = 0.99;
            dphysics.MinVelocity = 0.01;

            CollidableSphere collidable = new CollidableSphere(sphysics);
            collidable.Radius = Data.Radius;

            MovementProperties movement = new MovementProperties();
            movement.Acceleration = Data.Acceleration;
            movement.RotationSpeed = Data.RotationSpeed;

            Armament guns = new Armament();

            StaticPhysicsRenderer renderer = new StaticPhysicsRenderer(sphysics);
            renderer.TextureName = Data.Texture;

            ShipControl input = new ShipControl(dphysics, movement, guns);

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(movement);
            components.Add(guns);
            components.Add(renderer);
            components.Add(input);
        }

        public void Shoot()
        {
            shooting = true;
        }

        public void CeaseFire()
        {
            shooting = false;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write(PlayerNumber);
            packet.Write(Data);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            PlayerNumber = packet.ReadInt32();
            Data.Depacketize(packet);

            StaticPhysics sphysics = new StaticPhysics();
            DynamicPhysics dphysics = new DynamicPhysics(sphysics);
            CollidableSphere collidable = new CollidableSphere(sphysics);
            MovementProperties movement = new MovementProperties();
            Armament guns = new Armament();
            StaticPhysicsRenderer renderer = new StaticPhysicsRenderer(sphysics);
            ShipControl input = new ShipControl(dphysics, movement, guns);

            components.Add(sphysics);
            components.Add(dphysics);
            components.Add(collidable);
            components.Add(movement);
            components.Add(guns);
            components.Add(renderer);
            components.Add(input);

            base.Depacketize(packet);
        }
    }
}
