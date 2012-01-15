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
    class PatrolBehaviour:Behaviour
    {
        private Vector2 wanderDirection;
        private static Random random = new Random();
        public PatrolBehaviour(Entity entity)
            :base(entity)
        {
            
        }
        public void update()
        {
            wanderDirection.X +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());
            wanderDirection.Y +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());

            if (wanderDirection != Vector2.Zero)
            {
                wanderDirection.Normalize();
            }
            Transform transform = entity.GetComponent<Transform>();
            var modules = entity.GetComponent<EntityModules<EntityAttributeType>>();
            float mass = modules.GetValue(EntityAttributeType.Mass);
            var rotation = modules.GetValue(EntityAttributeType.RotationForce) / mass;
            TurnToFace(transform.Translation + wanderDirection, .15f * rotation);
            var input = entity.GetComponent<ShipControl>();
            // Next, we'll turn the characters back towards the center of the screen, to
            // prevent them from getting stuck on the edges of the screen.   
            //Vector2 screenCenter = new Vector2(Entity.LevelBoundary.Width / 2,
            //    Entity.LevelBoundary.Height / 2);

            //float distanceFromCenter = Vector2.Distance(screenCenter,transform.Translation);
            //float MaxDistanceFromScreenCenter =
            //    Math.Min(screenCenter.Y, screenCenter.X);

            //float normalizedDistance = distanceFromCenter / MaxDistanceFromScreenCenter;

            //float turnToCenterSpeed = .3f * normalizedDistance * normalizedDistance *
            //    rotation;

            //// Once we've calculated how much we want to turn towards the center, we can
            //// use the TurnToFace function to actually do the work.
            //TurnToFace(screenCenter, turnToCenterSpeed);
            
            //input.Accelerate();
        }
    }
}
