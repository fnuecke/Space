using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    /// <summary>
    /// Moves to the nearest friendly station, then docks.
    /// </summary>
    internal sealed class DockBehavior : Behavior
    {
        #region Constants

        /// <summary>
        /// How far we want to look for a station. This can be a fairly high
        /// number, because we don't do this very often. Hopefully :D
        /// </summary>
        private const float ScanRange = 30000;

        /// <summary>
        /// How close we want to be to the station to allow docking.
        /// </summary>
        private const float DockingRange = 100;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DockBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai.</param>
        public DockBehavior(ArtificialIntelligence ai)
            : base(ai, 0)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Finds the closest friendly station and moves to it, if we're close
        /// enough we dock (delete the instance).
        /// </summary>
        /// <returns>
        /// Whether to do the rest of the update.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // See if there are any stations nearby.
            var faction = AI.Manager.GetComponent<Faction>(AI.Entity).Value;
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            var index = AI.Manager.GetSystem<IndexSystem>();

            // The closest station we were able to find and how far it is away.
            var closestStation = 0;
            var distanceSquared = float.MaxValue;

            ICollection<int> neighbors = new List<int>(); // TODO use reusable list to avoid reallocation each update
            index.Find(position, ScanRange, ref neighbors, Detectable.IndexGroup);
            foreach (var neighbor in neighbors)
            {
                // See if it's a station.
                // TODO...

                // Friend or foe?
                var neighborFaction = AI.Manager.GetComponent<Faction>(neighbor);
                if (neighborFaction != null && (neighborFaction.Value & faction) != 0)
                {
                    // Friend. Closer than any other?
                    var neighborPosition = AI.Manager.GetComponent<Transform>(neighbor).Translation;
                    var neighborDistanceSquared = (position - neighborPosition).LengthSquared();
                    if (neighborDistanceSquared < distanceSquared)
                    {
                        distanceSquared = neighborDistanceSquared;
                        closestStation = neighbor;
                    }
                }
            }

            // Do we have a closest station?
            if (closestStation > 0)
            {
                var neighborPosition = AI.Manager.GetComponent<Transform>(closestStation).Translation;
                // Close enough to dock?
                if ((position - neighborPosition).LengthSquared() < DockingRange * DockingRange)
                {
                    // Yes! Kill ourselves.
                    // TODO: display some particle effects to hint we're docking.
                    AI.Manager.RemoveEntity(AI.Entity);
                }
                else
                {
                    // No. Let's try to get there.
                    AI.AttackMove(ref neighborPosition);
                }
            }

            // We never have anything else to do anyway.
            return false;
        }

        #endregion
    }
}
