using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.Random;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.Behaviors
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
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public AttackMoveBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random)
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
            var faction = ((Faction)AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var index = (IndexSystem)AI.Manager.GetSystem(IndexSystem.TypeId);
            ISet<int> neighbors = new HashSet<int>();
            index.Find(position, AggroRange, ref neighbors, DetectableSystem.IndexGroupMask);
            foreach (var neighbor in neighbors)
            {
                // See if it has health. Otherwise don't bother attacking.
                var health = ((Health)AI.Manager.GetComponent(neighbor, Health.TypeId));
                if (health == null || !health.Enabled || health.Value <= 0)
                {
                    continue;
                }

                // Friend or foe? Don't care if it's a friend.
                // TODO: unless it's in a fight, then we might want to support our allies?
                var neighborFaction = ((Faction)AI.Manager.GetComponent(neighbor, Faction.TypeId));
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
