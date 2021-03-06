﻿using System;
using Engine.ComponentSystem.Spatial.Components;
using Engine.FarMath;
using Engine.Random;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>Makes an AI move to a specified location without letting itself be interrupted.</summary>
    internal class MoveBehavior : Behavior
    {
        #region Constants

        /// <summary>Consider our target reached when we're in an epsilon range with this radius of the target position.</summary>
        private const float ReachedEpsilon = 100;

        #endregion

        #region Fields

        /// <summary>The position to move to.</summary>
        public FarPosition Target;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="MoveBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public MoveBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 1) {}

        /// <summary>Reset this behavior so it can be reused later on.</summary>
        public override void Reset()
        {
            base.Reset();

            Target = FarPosition.Zero;
        }

        #endregion

        #region Logic

        /// <summary>Check if we reached our target.</summary>
        /// <returns>Whether to do the rest of the update.</returns>
        protected override bool UpdateInternal()
        {
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;
            if (FarPosition.DistanceSquared(position, Target) < ReachedEpsilon * ReachedEpsilon)
            {
                // We have reached our target, pop self.
                AI.PopBehavior();
                return false;
            }

            return true;
        }

        /// <summary>Figure out where we want to go.</summary>
        /// <returns>The coordinate we want to fly to.</returns>
        protected override FarPosition GetTargetPosition()
        {
            return Target;
        }
        
        protected override float GetTargetRotation(Vector2 direction)
        {
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;
            var toTarget = (Vector2) (Target - position);
            return (float) Math.Atan2(toTarget.Y, toTarget.X);
        }

        #endregion
    }
}