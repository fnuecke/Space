using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    class Behaviour
    {
        protected Entity entity;
        public Behaviour(Entity entity)
        {
            this.entity = entity;
        }
        public void TurnToFace(Vector2 facePosition, float turnSpeed)
        {
            var input = entity.GetComponent<ShipControl>();
            
            Transform transform = entity.GetComponent<Transform>();
            float x = facePosition.X - transform.Translation.X;
            float y = facePosition.Y - transform.Translation.Y;

            float desiredAngle = (float)Math.Atan2(y, x);
            float difference = WrapAngle(desiredAngle - transform.Rotation);

            difference = MathHelper.Clamp(difference, -turnSpeed, turnSpeed);
            input.TargetRotation = WrapAngle(transform.Rotation + difference);
        }
        /// <summary>
        /// Returns the angle expressed in radians between -Pi and Pi.
        /// <param name="radians">the angle to wrap, in radians.</param>
        /// <returns>the input value expressed in radians from -Pi to Pi.</returns>
        /// </summary>
        public static float WrapAngle(float radians)
        {
            while (radians < -MathHelper.Pi)
            {
                radians += MathHelper.TwoPi;
            }
            while (radians > MathHelper.Pi)
            {
                radians -= MathHelper.TwoPi;
            }
            return radians;
        }
    }
}
