using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Util;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>Moves to the nearest friendly station, then docks.</summary>
    internal sealed class DockBehavior : Behavior
    {
        #region Constants

        /// <summary>
        ///     How far we want to look for a station. This can be a fairly high number, because we don't do this very often.
        ///     Hopefully :D
        /// </summary>
        private static readonly float ScanRange = UnitConversion.ToSimulationUnits(30000);

        /// <summary>How close we want to be to the station to allow docking.</summary>
        private static readonly float DockingRange = UnitConversion.ToSimulationUnits(100);

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="DockBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public DockBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0) {}

        #endregion

        #region Logic

        /// <summary>Finds the closest friendly station and moves to it, if we're close enough we dock (delete the instance).</summary>
        /// <returns>Whether to do the rest of the update.</returns>
        protected override bool UpdateInternal()
        {
            // See if there are any stations nearby.
            var faction = ((Faction) AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;
            var index = (IndexSystem) AI.Manager.GetSystem(IndexSystem.TypeId);

            // The closest station we were able to find and how far it is away.
            var closestStation = 0;
            var distanceSquared = float.MaxValue;

            ISet<int> neighbors = new HashSet<int>();
            index.Find(position, ScanRange, neighbors, DetectableSystem.IndexGroupMask);
            foreach (IIndexable neighbor in neighbors.Select(AI.Manager.GetComponentById))
            {
                // See if it's a station.
                // TODO...

                // Friend or foe?
                var neighborFaction = ((Faction) AI.Manager.GetComponent(neighbor.Entity, Faction.TypeId));
                if (neighborFaction != null && (neighborFaction.Value & faction) != 0)
                {
                    // Friend. Closer than any other?
                    var neighborPosition = ((ITransform) AI.Manager.GetComponent(neighbor.Entity, TransformTypeId)).Position;
                    var neighborDistanceSquared = FarPosition.DistanceSquared(position, neighborPosition);
                    if (neighborDistanceSquared < distanceSquared)
                    {
                        distanceSquared = neighborDistanceSquared;
                        closestStation = neighbor.Entity;
                    }
                }
            }

            // Do we have a closest station?
            if (closestStation > 0)
            {
                var neighborPosition =
                    ((ITransform) AI.Manager.GetComponent(closestStation, TransformTypeId)).Position;
                // Close enough to dock?
                if (FarPosition.DistanceSquared(position, neighborPosition) < DockingRange * DockingRange)
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