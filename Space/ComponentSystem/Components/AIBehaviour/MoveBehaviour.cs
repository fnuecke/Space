using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    class MoveBehaviour:Behaviour
    {
        public Vector2 TargetPosition;
        public override void Update()
        {
            var info = AiComponent.Entity.GetComponent<ShipInfo>();
            var input = AiComponent.Entity.GetComponent<ShipControl>();

            var position = info.Position;
            direction = TargetPosition - position;
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
                .Write(TargetPosition);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);
            TargetPosition = packet.ReadVector2();
        }
    }
}
