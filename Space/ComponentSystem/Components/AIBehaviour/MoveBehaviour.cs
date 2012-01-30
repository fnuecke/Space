using System;
using Microsoft.Xna.Framework;
using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    sealed class MoveBehaviour : Behaviour
    {

        public MoveBehaviour(AiComponent component)
            : base(component)
        {

        }
        public MoveBehaviour()
        {

        }
        public Vector2 TargetPosition;
        public int Target;
        public override void Update()
        {
            var info = AiComponent.Entity.GetComponent<ShipInfo>();
            var input = AiComponent.Entity.GetComponent<ShipControl>();

            var position = info.Position;
            if (Target == 0)
                direction = TargetPosition - position;
            else
            {
                var target = AiComponent.Entity.Manager.GetEntity(Target);
                var transform = target.GetComponent<Transform>();
                direction = transform.Translation - position;
            }
            if(direction != Vector2.Zero)
                direction.Normalize();

            //look to flight direction
            input.SetTargetRotation((float)Math.Atan2(direction.Y, direction.X));

            var escapeDir = CalculateEscapeDirection();
            direction += 2 * escapeDir;
            
            input.SetAcceleration(direction);
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

        public override Behaviour DeepCopy(Behaviour into)
        {
            var copy = (MoveBehaviour)base.DeepCopy(into);

            if (copy == into)
            {
                copy.TargetPosition = TargetPosition;
                copy.Target = Target;
            }

            return copy;
        }
    }
}
