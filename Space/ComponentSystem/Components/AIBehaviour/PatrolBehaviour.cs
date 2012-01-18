using System;
using Microsoft.Xna.Framework;

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


            float normalizedDistance = distanceFromCenter / AiComponent.AiCommand.maxDistance;

           

            // Once we've calculated how much we want to turn towards the center, we can
            // use the TurnToFace function to actually do the work.

            wanderDirection += 2 * CalculateEscapeDirection();

            input.SetTargetRotation((float)Math.Atan2(wanderDirection.Y,wanderDirection.X));
            if (3 * info.Speed > AiComponent.MaxSpeed)
            {
                input.SetAcceleration(Vector2.Zero);
            }
            else
            {
                input.SetAcceleration(wanderDirection);
            }
        }
    }
}   
