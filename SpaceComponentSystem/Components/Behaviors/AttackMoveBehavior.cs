using Engine.Random;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>
    ///     Patrols to a specified point, attacks enemies that get close, falls back to previous behavior if it reaches
    ///     its goal.
    /// </summary>
    internal sealed class AttackMoveBehavior : MoveBehavior
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="AttackMoveBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public AttackMoveBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random) {}

        #endregion

        #region Logic

        /// <summary>Check if there are any enemies nearby, if not check if we have arrived at our target.</summary>
        /// <returns>Whether to do the rest of the update.</returns>
        protected override bool UpdateInternal()
        {
            // Check for nearby enemies.
            var enemy = GetClosestEnemy(AI.Configuration.AggroRange, OwnerWithHealthFilter);
            if (enemy != 0)
            {
                // It's an enemy. Attack it.
                AI.Attack(enemy);
                return false;
            }

            // Nothing is distracting us, keep going as usual.
            return base.UpdateInternal();
        }

        #endregion
    }
}