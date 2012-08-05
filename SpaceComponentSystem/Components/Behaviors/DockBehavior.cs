using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.Behaviors
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
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public DockBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0)
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
            var faction = ((Faction)AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var index = (IndexSystem)AI.Manager.GetSystem(IndexSystem.TypeId);

            // The closest station we were able to find and how far it is away.
            var closestStation = 0;
            var distanceSquared = float.MaxValue;

            ISet<int> neighbors = new HashSet<int>();
            index.Find(position, ScanRange, ref neighbors, DetectableSystem.IndexGroupMask);
            foreach (var neighbor in neighbors)
            {
                // See if it's a station.
                // TODO...

                // Friend or foe?
                var neighborFaction = ((Faction)AI.Manager.GetComponent(neighbor, Faction.TypeId));
                if (neighborFaction != null && (neighborFaction.Value & faction) != 0)
                {
                    // Friend. Closer than any other?
                    var neighborPosition = ((Transform)AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                    var neighborDistanceSquared = ((Vector2)(position - neighborPosition)).LengthSquared();
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
                var neighborPosition = ((Transform)AI.Manager.GetComponent(closestStation, Transform.TypeId)).Translation;
                // Close enough to dock?
                if (((Vector2)(position - neighborPosition)).LengthSquared() < DockingRange * DockingRange)
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
