﻿using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>
    /// Makes AI ships roam a specified area.
    /// </summary>
    internal sealed class RoamBehavior : Behavior
    {
        #region Fields
        
        /// <summary>
        /// The region we're roaming in.
        /// </summary>
        public FarRectangle Area;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RoamBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public RoamBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0)
        {
        }

        /// <summary>
        /// Reset this behavior so it can be reused later on.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Area = FarRectangle.Empty;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Pick a new target in our region and issue a patrol command towards
        /// it.
        /// </summary>
        /// <returns>
        /// The behavior change.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // We got here, so we have to pick a new destination.
            FarPosition target;
            target.X = Random.NextInt32((int)Area.Left, (int)Area.Right);
            target.Y = Random.NextInt32((int)Area.Top, (int)Area.Bottom);

            // And move towards it.
            AI.AttackMove(ref target);

            // Nothing to do. Ever.
            return false;
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public override void CopyInto(Behavior into)
        {
            base.CopyInto(into);

            var copy = (RoamBehavior)into;

            copy.Area = Area;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Area=" + Area;
        }

        #endregion
    }
}
