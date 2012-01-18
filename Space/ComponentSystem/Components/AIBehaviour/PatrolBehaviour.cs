using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    public class PatrolBehaviour : Behaviour
    {

        private static Random random = new Random();
        public PatrolBehaviour(AIComponent component)
            : base(component)
        {

        }
        public override void Update()
        {
            wanderDirection.X +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());
            wanderDirection.Y +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());

            if (wanderDirection != Vector2.Zero)
            {
                wanderDirection.Normalize();
            }
            var info = AiComponent.Entity.GetComponent<ShipInfo>();
            var input = AiComponent.Entity.GetComponent<ShipControl>();

            //Next, we'll turn the characters back towards the center of the screen, to
            //prevent them from getting stuck on the edges of the screen.   



            float distanceFromCenter = Vector2.Distance(AiComponent.AiCommand.target, info.Position);


            //float normalizedDistance = distanceFromCenter / AiComponent.AiCommand.maxDistance;

            var escapeDir = CalculateEscapeDirection();

            // Once we've calculated how much we want to turn towards the center, we can
            // use the TurnToFace function to actually do the work.

            wanderDirection += 2 * escapeDir;

            input.TargetRotation = (float)Math.Atan2(wanderDirection.Y, wanderDirection.X);
            if (escapeDir == Vector2.Zero && 3 * info.Speed > AiComponent.MaxSpeed)
                input.StopAccelerate();
            else
            {
                input.Accelerate(wanderDirection);
            }

        }
    }
}
