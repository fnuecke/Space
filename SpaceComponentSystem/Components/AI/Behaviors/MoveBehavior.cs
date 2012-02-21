using System;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    sealed class MoveBehavior : Behavior
    {

        public Vector2 TargetPosition;

        public int Target;

        public MoveBehavior(AI component)
            : base(component)
        {

        }

        public override void Update()
        {
            var info = AI.Entity.GetComponent<ShipInfo>();
            var input = AI.Entity.GetComponent<ShipControl>();

            var position = info.Position;
            if (Target == 0)
                Direction = TargetPosition - position;
            else
            {
                var target = AI.Entity.Manager.GetEntity(Target);
                var transform = target.GetComponent<Transform>();
                Direction = transform.Translation - position;
            }
            if(Direction != Vector2.Zero)
                Direction.Normalize();

            //look to flight direction
            input.SetTargetRotation((float)Math.Atan2(Direction.Y, Direction.X));

            var escapeDir = CalculateEscapeDirection();
            Direction += 2 * escapeDir;
            
            input.SetAcceleration(Direction);
        }

        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(TargetPosition)
                .Write(Target);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            TargetPosition = packet.ReadVector2();
            Target = packet.ReadInt32();
        }

        public override Behavior DeepCopy(Behavior into)
        {
            var copy = (MoveBehavior)base.DeepCopy(into);

            if (copy == into)
            {
                copy.TargetPosition = TargetPosition;
                copy.Target = Target;
            }

            return copy;
        }
    }
}
