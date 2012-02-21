using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    sealed class PatrolBehavior : Behavior
    {
        private MersenneTwister random = new MersenneTwister();

        public PatrolBehavior(AI component)
            : base(component)
        {
        }

        public override void Update()
        {
            Direction.X += MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());
            Direction.Y += MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());

            if (Direction != Vector2.Zero)
            {
                Direction.Normalize();
            }
            var info = AI.Entity.GetComponent<ShipInfo>();
            var input = AI.Entity.GetComponent<ShipControl>();
            input.SetShooting(false);
            input.Stabilizing = true;
            //Next, we'll turn the characters back towards the center of the screen, to
            //prevent them from getting stuck on the edges of the screen.   
            float distanceFromCenter = Vector2.Distance(AI.Command.Target, info.Position);

            //calculate if there is a dangerous object.. if yes get the hell out of here!
            var escapeDir = CalculateEscapeDirection();
            Direction += 2 * escapeDir;

            //Rotate torwards our destination
            input.SetTargetRotation((float)Math.Atan2(Direction.Y, Direction.X));
            //not fullspeed if there is noting to fear about
            if (escapeDir == Vector2.Zero && 3 * info.Speed > info.MaxSpeed)
            {
                input.SetAcceleration(Vector2.Zero);
            }
            else//accelerate towrads Destiny
            {
                input.SetAcceleration(Direction);
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

        public override Behavior DeepCopy(Behavior into)
        {
            var copy = (PatrolBehavior)base.DeepCopy(into);

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
