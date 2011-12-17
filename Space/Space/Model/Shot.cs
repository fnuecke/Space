using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Engine.Simulation;

namespace Space.Model
{
    class Shot : AbstractEntity
    {
        public Shot()
        {
        }

        public Shot(string textureName, FPoint position, FPoint velocity, PacketizerContext context)
        {
            PhysicsComponent physics = new PhysicsComponent();
            physics.Position = position;
            physics.Velocity = velocity;
            components.Add(physics);

            PhysicsDrawComponent draw = new PhysicsDrawComponent(physics);
            draw.TextureName = textureName;

            //context.weaponsSounds[name].Play();
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        public override void Depacketize(Packet packet, IPacketizerContext context)
        {
            PhysicsComponent physics = new PhysicsComponent();
            components.Add(physics);
            components.Add(new PhysicsDrawComponent(physics));

            base.Depacketize(packet, context);
        }
    }
}
