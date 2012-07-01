using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    /// <summary>
    /// Patrols to a specified point, attacks enemies that get close, falls
    /// back to previous behavior if it reaches its goal.
    /// </summary>
    internal sealed class AttackMoveBehavior : MoveBehavior
    {
        #region Constants

        /// <summary>
        /// The distance enemy units must get closer than for us to attack
        /// them.
        /// </summary>
        private const float AggroRange = 2500;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackMoveBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai.</param>
        public AttackMoveBehavior(ArtificialIntelligence ai)
            : base(ai)
        {
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Check if there are any enemies nearby, if not check if we have
        /// arrived at our target.
        /// </summary>
        /// <returns>
        /// Whether to do the rest of the update.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // See if there are any enemies nearby, if so attack them.
            var faction = AI.Manager.GetComponent<Faction>(AI.Entity).Value;
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            var index = AI.Manager.GetSystem<IndexSystem>();
            ICollection<int> neighbors = new List<int>(); // TODO use reusable list to avoid reallocation each update
            index.RangeQuery(ref position, AggroRange, ref neighbors, Detectable.IndexGroup);
            foreach (var neighbor in neighbors)
            {
                // See if it has health. Otherwise don't bother attacking.
                var health = AI.Manager.GetComponent<Health>(neighbor);
                if (health == null || !health.Enabled || health.Value <= 0)
                {
                    continue;
                }

                // Friend or foe? Don't care if it's a friend.
                // TODO: unless it's in a fight, then we might want to support our allies?
                var neighborFaction = AI.Manager.GetComponent<Faction>(neighbor);
                if (neighborFaction != null && (neighborFaction.Value & faction) == 0)
                {
                    // It's an enemy. Attack it.
                    AI.Attack(neighbor);
                    return false;
                }
            }

            // Nothing is distracting us, keep going as usual.
            return base.UpdateInternal();
        }

        #endregion
    }
}   
