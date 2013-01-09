using Engine.FarMath;
using Engine.Random;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>Makes AI ships roam a specified area.</summary>
    internal sealed class RoamBehavior : Behavior
    {
        #region Fields

        /// <summary>The region we're roaming in.</summary>
        public FarRectangle Area;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoamBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public RoamBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0) {}

        /// <summary>Reset this behavior so it can be reused later on.</summary>
        public override void Reset()
        {
            base.Reset();

            Area = FarRectangle.Empty;
        }

        #endregion

        #region Logic

        /// <summary>Pick a new target in our region and issue a patrol command towards it.</summary>
        /// <returns>The behavior change.</returns>
        protected override bool UpdateInternal()
        {
            // We got here, so we have to pick a new destination.
            FarPosition target;
            target.X = Random.NextInt32((int) Area.Left, (int) Area.Right);
            target.Y = Random.NextInt32((int) Area.Top, (int) Area.Bottom);

            // And move towards it.
            AI.AttackMove(ref target);

            // Nothing to do. Ever.
            return false;
        }

        #endregion
    }
}