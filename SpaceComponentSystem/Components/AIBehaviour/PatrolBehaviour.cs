using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    sealed class PatrolBehaviour : Behaviour
    {
        private MersenneTwister random = new MersenneTwister();

        public PatrolBehaviour(AiComponent component)
            : base(component)
        {
        }

        public PatrolBehaviour()
        {
        }

        public override void Update()
        {
            direction.X += MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());
            direction.Y += MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }
            var info = AiComponent.Entity.GetComponent<ShipInfo>();
            var input = AiComponent.Entity.GetComponent<ShipControl>();
            input.SetShooting(false);
            input.Stabilizing = true;
            //Next, we'll turn the characters back towards the center of the screen, to
            //prevent them from getting stuck on the edges of the screen.   
            float distanceFromCenter = Vector2.Distance(AiComponent.Command.Target, info.Position);

            //calculate if there is a dangerous object.. if yes get the hell out of here!
            var escapeDir = CalculateEscapeDirection();
            direction += 2 * escapeDir;

            //Rotate torwards our destination
            input.SetTargetRotation((float)Math.Atan2(direction.Y, direction.X));
            //not fullspeed if there is noting to fear about
            if (escapeDir == Vector2.Zero && 3 * info.Speed > info.MaxSpeed)
            {
                input.SetAcceleration(Vector2.Zero);
            }
            else//accelerate towrads Destiny
            {
                input.SetAcceleration(direction);
            }
        }

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
              .Write(random);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            random = packet.ReadPacketizable<MersenneTwister>();
        }

        public override Behaviour DeepCopy(Behaviour into)
        {
            var copy = (PatrolBehaviour)base.DeepCopy(into);

            if (copy == into)
            {
                copy.random = random.DeepCopy(copy.random);
            }
            else
            {
                copy.random = random.DeepCopy();
            }

            return copy;
        }
    }
}   
