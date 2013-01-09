using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;
using Space.Util;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>
    ///     Base class for AI ship behaviors. This implements rudimentary functionality that can be viewed as the
    ///     vegetative nervous system of the AI. For example, it tries to keep the AI away from danger, and 'automatically'
    ///     navigates to a desired destination.
    /// </summary>
    internal abstract class Behavior : IPacketizable, ICopyable<Behavior>
    {
        #region Fields

        /// <summary>The AI component this behavior belongs to.</summary>
        [PacketizerIgnore]
        protected readonly ArtificialIntelligence AI;

        /// <summary>The randomizer we use to make pseudo random decisions.</summary>
        /// <remarks>
        ///     The "owner" of this instance is the AI component we belong to, so we do not need to take care of serialization
        ///     or copying.
        /// </remarks>
        [CopyIgnore, PacketizerIgnore]
        protected readonly IUniformRandom Random;

        /// <summary>The poll rate in ticks how often to update this behavior.</summary>
        [PacketizerIgnore]
        private readonly int _pollRate;

        /// <summary>How many more ticks to wait before calling update on</summary>
        private int _ticksToWait;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Behavior"/> class.
        /// </summary>
        /// <param name="ai">The AI component.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        /// <param name="pollRate">The poll rate in seconds.</param>
        protected Behavior(ArtificialIntelligence ai, IUniformRandom random, float pollRate)
        {
            AI = ai;
            Random = random;
            _pollRate = (int) (pollRate * Settings.TicksPerSecond);
        }

        /// <summary>Reset this behavior so it can be reused later on.</summary>
        public virtual void Reset()
        {
            _ticksToWait = 0;
        }

        #endregion

        #region Logic

        /// <summary>Updates the behavior and returns the behavior type to switch to.</summary>
        public void Update()
        {
            // Don't update more often than we have to. For example, patrol
            // behaviors should require way fewer updates than attack
            // behaviors.
            if (_ticksToWait > 0)
            {
                --_ticksToWait;
                return;
            }

            // Do the behavior specific update.
            if (!UpdateInternal())
            {
                // Skip if we don't have to do the rest (e.g. popped self).
                return;
            }

            // No change, wait a bit with the next update.
            _ticksToWait = _pollRate / 2 + Random.NextInt32(_pollRate);

            // Figure out where we want to go.
            var targetPosition = GetTargetPosition();

            // And accordingly, which way to accelerate to get there.
            var direction =
                (Vector2)
                (targetPosition - ((Transform) AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation);

            // Normalize if it's not zero.
            var norm = direction.LengthSquared();
            if (norm > 0)
            {
                norm = (float) Math.Sqrt(norm);
                direction.X /= norm;
                direction.Y /= norm;
            }

            // Multiply with the desired acceleration.
            var speed = MathHelper.Clamp(GetThrusterPower(), 0, 1);
            direction.X *= speed;
            direction.Y *= speed;

            // Figure out where we want to go vegetatively (flocking).
            direction += GetVegetativeDirection() * AI.Configuration.VegetativeWeight;

            // Set our new acceleration direction and target rotation.
            var shipControl = ((ShipControl) AI.Manager.GetComponent(AI.Entity, ShipControl.TypeId));
            shipControl.SetAcceleration(direction);
            shipControl.SetTargetRotation(GetTargetRotation(direction));
        }

        /// <summary>
        ///     Called when an entity becomes an invalid target (removed from the system or died). This is intended to allow
        ///     behaviors to stop in case their related entity is removed (e.g. target when attacking).
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        internal virtual void OnEntityInvalidated(int entity) {}

        #endregion

        #region Behavior type specifics

        /// <summary>Behavior specific update logic, e.g. checking for nearby enemies.</summary>
        /// <returns>Whether to do the rest of the update.</returns>
        protected abstract bool UpdateInternal();

        /// <summary>Figure out where we want to go.</summary>
        /// <returns>The coordinate we want to fly to.</returns>
        protected virtual FarPosition GetTargetPosition()
        {
            // Per default we just stand still.
            return ((Transform) AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
        }

        /// <summary>How fast do we want to fly, relative to our maximum speed?</summary>
        /// <returns>The relative speed we want to fly at.</returns>
        protected virtual float GetThrusterPower()
        {
            return 0.5f;
        }

        /// <summary>Gets the target rotation we want to be facing.</summary>
        /// <param name="direction">The direction we're accelerating in.</param>
        /// <returns>The desired target rotation.</returns>
        protected virtual float GetTargetRotation(Vector2 direction)
        {
            // Per default just head the way we're flying.
            return (float) Math.Atan2(direction.Y, direction.X);
        }

        #endregion

        #region Vegetative nervous system / Utility

        /// <summary>
        ///     Filter function for <see cref="GetClosestEnemy"/> to only look for living enemies only.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>Whether to consider attacking that entity.</returns>
        protected int OwnerWithHealthFilter(int entity)
        {
            // Check if the entity has health.
            if (CheckHealth(entity))
            {
                return entity;
            }

            // Otherwise see if we can find an owner and check its health.
            entity = ((OwnerSystem) AI.Manager.GetSystem(OwnerSystem.TypeId)).GetRootOwner(entity);
            if (entity != 0 && CheckHealth(entity))
            {
                return entity;
            }

            // If we come here, entity is invalid.
            return 0;
        }

        /// <summary>Checks if an entity has health, and is not dead (health != 0).</summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>Whether the entity has any health left.</returns>
        private bool CheckHealth(int entity)
        {
            var health = (Health) AI.Manager.GetComponent(entity, Health.TypeId);
            return health != null && health.Enabled && health.Value > 0;
        }

        /// <summary>
        ///     Gets the closest enemy based on the specified criteria. This checks all enemies in the specified range, but
        ///     only takes into consideration those that pass the filter function. The enemy with the lowest distance will be
        ///     returned. Note that the filter function may also "transform" the id, for example to allow selecting the actual
        ///     owner of an entity (normally: projectiles).
        /// </summary>
        /// <param name="range">The maximum range up to which to search.</param>
        /// <param name="filter">The filter function, if any.</param>
        /// <returns></returns>
        protected int GetClosestEnemy(float range, Func<int, int> filter = null)
        {
            // See if there are any enemies nearby, if so attack them.
            var faction = ((Faction) AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var position = ((Transform) AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var index = (IndexSystem) AI.Manager.GetSystem(IndexSystem.TypeId);
            var shipInfo = (ShipInfo) AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId);
            var sensorRange = shipInfo != null ? shipInfo.RadarRange : 0f;
            ISet<int> neighbors = new HashSet<int>();
            index.Find(
                position,
                sensorRange > 0 ? Math.Min(sensorRange, range) : range,
                ref neighbors,
                CollisionSystem.IndexGroupMask);
            var closest = 0;
            var closestDistance = float.PositiveInfinity;
            foreach (var neighbor in neighbors)
            {
                // Apply our filter, if we have one.
                var filteredNeighbor = neighbor;
                if (filter != null)
                {
                    filteredNeighbor = filter(neighbor);
                }
                if (filteredNeighbor == 0)
                {
                    continue;
                }
                // Friend or foe? Don't care if it's a friend. Also filter based on passed
                // filter function.
                var neighborFaction = (Faction) AI.Manager.GetComponent(filteredNeighbor, Faction.TypeId);
                if (neighborFaction != null && (neighborFaction.Value & faction) == 0)
                {
                    // It's an enemy. Check the distance.
                    var enemyPosition =
                        ((Transform) AI.Manager.GetComponent(filteredNeighbor, Transform.TypeId)).Translation;
                    var distance = FarPosition.Distance(enemyPosition, position);
                    if (distance < closestDistance)
                    {
                        closest = filteredNeighbor;
                        closestDistance = distance;
                    }
                }
            }

            // Return whatever we found.
            return closest;
        }

        /// <summary>
        ///     Gets the escape direction, i.e. the direction in which to accelerate to avoid bad things (stuff that hurts us
        ///     on impact), together with a direction towards friendly ships with the direction those are flying in mixed in
        ///     (flocking).
        /// </summary>
        /// <returns>The averaged direction away from potential danger and towards desired objects.</returns>
        private Vector2 GetVegetativeDirection()
        {
            // Get some info about ourself.
            var faction = ((Faction) AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var squad = (Squad) AI.Manager.GetComponent(AI.Entity, Squad.TypeId);
            var info = ((ShipInfo) AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId));
            var position = info.Position;
            var mass = info.Mass;

            // Look for evil neighbors, in particular suns and the like.
            var index = (IndexSystem) AI.Manager.GetSystem(IndexSystem.TypeId);
            ISet<int> neighbors = new HashSet<int>();
            index.Find(
                position, AI.Configuration.MaxEscapeCheckDistance, ref neighbors, DetectableSystem.IndexGroupMask);
            var escape = Vector2.Zero;
            var escapeNormalizer = 0;
            foreach (var neighbor in neighbors)
            {
                // If it does damage we want to keep our distance.
                var neighborFaction = ((Faction) AI.Manager.GetComponent(neighbor, Faction.TypeId));
                var neighborCollisionDamage =
                    ((CollisionDamage) AI.Manager.GetComponent(neighbor, CollisionDamage.TypeId));
                if (neighborCollisionDamage != null &&
                    (neighborFaction == null || (neighborFaction.Value & faction) == 0))
                {
                    // This one does damage and is not our friend... try to avoid it.
                    var neighborGravitation = ((Gravitation) AI.Manager.GetComponent(neighbor, Gravitation.TypeId));
                    var neighborPosition = ((Transform) AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                    var toNeighbor = (Vector2) (position - neighborPosition);

                    // Does it pull?
                    if (neighborGravitation != null &&
                        (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
                    {
                        // Yes! Let's see how close we are comfortable to get.
                        var pointOfNoReturnSquared = mass * neighborGravitation.Mass / info.MaxAcceleration;
                        if (toNeighbor.LengthSquared() <
                            pointOfNoReturnSquared * AI.Configuration.MinMultipleOfPointOfNoReturn *
                            AI.Configuration.MinMultipleOfPointOfNoReturn)
                        {
                            // We're too close, let's pull out. Just use the square
                            // of the point of no return so it's really urgent.
                            toNeighbor.Normalize();
                            escape += AI.Configuration.MinMultipleOfPointOfNoReturn * pointOfNoReturnSquared *
                                      toNeighbor;
                            ++escapeNormalizer;
                        }
                    }
                    else
                    {
                        // OK, just a damager, but doesn't pull us in. Scale
                        // to make us reach a certain minimum distance.
                        toNeighbor.Normalize();
                        escape += AI.Configuration.MinDistanceToDamagers * toNeighbor;
                        ++escapeNormalizer;
                    }
                }
                else if (neighborFaction != null && (neighborFaction.Value & faction) == 0)
                {
                    // It's a normal enemy. Try to avoid it. This is similar to separation.
                    var neighborPosition = ((Transform) AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                    var toNeighbor = (Vector2) (neighborPosition - position);
                    var toNeighborDistanceSquared = toNeighbor.LengthSquared();
                    // Avoid NaNs when at same place as neighbor and see if we're close
                    // enough to care.
                    if (toNeighborDistanceSquared > 0f &&
                        toNeighborDistanceSquared <= AI.Configuration.EnemySeparation * AI.Configuration.EnemySeparation)
                    {
                        // Try to put some distance between us.
                        var distance = (float) Math.Sqrt(toNeighborDistanceSquared);
                        var escapeDir = toNeighbor * -(AI.Configuration.EnemySeparation / distance - 1);
                        // Add some of that perpendicular to the escape direction, to
                        // avoid enemies just stopping, instead making them circle their
                        // target. Make the direction the unit circles depend on its ID
                        // which should be sufficiently "random".
                        Vector2 perpendicular;
                        if (((AI.Entity + AI.Id) & 1) == 0)
                        {
                            perpendicular.X = escapeDir.Y * 0.5f;
                            perpendicular.Y = -escapeDir.X * 0.5f;
                        }
                        else
                        {
                            perpendicular.X = -escapeDir.Y * 0.5f;
                            perpendicular.Y = escapeDir.X * 0.5f;
                        }
                        escape += escapeDir * 0.5f + perpendicular;
                        ++escapeNormalizer;
                    }
                }
            }

            SetLastEscape(escape / Math.Max(1, escapeNormalizer));

            // Check all neighbors in normal flocking range. If we're in a squad, skip
            // other squad members and take our squad position into account instead.
            neighbors.Clear();
            index.Find(position, AI.Configuration.FlockingThreshold, ref neighbors, DetectableSystem.IndexGroupMask);
            var separation = Vector2.Zero;
            var separationNormalizer = 0;
            var cohesion = Vector2.Zero;
            var cohesionNormalizer = 0;
            foreach (var neighbor in neighbors)
            {
                // Ignore non-ships.
                if (AI.Manager.GetComponent(neighbor, ShipControl.TypeId) == null)
                {
                    continue;
                }

                // If squad leader, ignore followers.
                if (squad != null && AI.Entity == squad.Leader && squad.Contains(neighbor))
                {
                    continue;
                }

                // Get the position, direction and distance, needed for everything that follows.
                var neighborPosition = ((Transform) AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                var toNeighbor = (Vector2) (neighborPosition - position);
                var distance = (float) Math.Sqrt(toNeighbor.LengthSquared());
                // Avoid NaNs when at same place as neighbor...
                if (distance <= 0f)
                {
                    continue;
                }

                // Check if it's a friend, because if it is, we want to flock!
                var neighborFaction = (Faction) AI.Manager.GetComponent(neighbor, Faction.TypeId);
                if ((faction & neighborFaction.Value) != 0)
                {
                    // OK, flock. See if separation kicks in.
                    if (distance <= AI.Configuration.FlockingSeparation)
                    {
                        // Yes, somewhere outside the separation bounds of the other object. Note
                        // that we halve the separation because the other party will try to do the
                        // same thing (in the opposite direction), thus we reduce "bouncing" a
                        // little, i.e. oscillation between cohesion and separation.
                        separation -= toNeighbor * (AI.Configuration.FlockingSeparation / distance - 1) * 0.5f;
                        ++separationNormalizer;
                    }
                    else if (squad == null) // from query: && distance < FlockingThreshold
                    {
                        // No, add cohesion and alignment. Note that we only want to move up to
                        // the separation barrier. Halving has the same reason as separation above.
                        cohesion += toNeighbor * (1 - AI.Configuration.FlockingSeparation / distance) * 0.5f;

                        var neighborVelocity = (Velocity) AI.Manager.GetComponent(neighbor, Velocity.TypeId);
                        cohesion += neighborVelocity.Value;
                        ++cohesionNormalizer;
                    }
                }
            }

            SetLastSeparation(separation / Math.Max(1, separationNormalizer));
            SetLastCohesion(cohesion / Math.Max(1, cohesionNormalizer));

            // Apply formation preference for non-squad-leaders. Squads follow
            // special rules: the leader will not be influenced by its followers,
            // and will also look for separation from other friends (no cohesion
            // or alignment). Other squad members will try to separate from
            // each other, but will also not take cohesion or alignment into
            // account -- their relative position to their leader is exclusively
            // provided from the formation of the squad.
            var formation = Vector2.Zero;
            if (squad != null)
            {
                formation = (Vector2) (squad.ComputeFormationOffset() - position);
                if (AI.Entity != squad.Leader)
                {
                    var leaderVelocity = (Velocity) AI.Manager.GetComponent(squad.Leader, Velocity.TypeId);
                    if (leaderVelocity != null)
                    {
                        formation += leaderVelocity.Value;
                    }
                }
            }

            SetLastFormation(formation);

            // Compute composite direction.
            var direction = escape + separation + cohesion + formation;

            // If we have some influence, normalize it if necessary.
            direction /= AI.Configuration.VegetativeUrgencyDistance;
            if (direction.LengthSquared() > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        #endregion

        #region Serialization

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        [OnPacketize]
        public virtual IWritablePacket Packetize(IWritablePacket packet)
        {
            return packet;
        }

        /// <summary>
        ///     Bring the object to the state in the given packet. This is called after automatic depacketization has been
        ///     performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        [OnPostDepacketize]
        public virtual void Depacketize(IReadablePacket packet) {}

        [OnStringify]
        public virtual StreamWriter Dump(StreamWriter w, int indent)
        {
            return w;
        }

        #endregion

        #region Copying

        /// <summary>Creates a deep copy of the object.</summary>
        /// <returns>The copy.</returns>
        public Behavior NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>Creates a deep copy of the object, reusing the given object.</summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual void CopyInto(Behavior into)
        {
            Copyable.CopyInto(this, into);
        }

        #endregion

        #region Debugging

        [PacketizerIgnore]
        private Vector2 _lastEscape;

        [PacketizerIgnore]
        private Vector2 _lastSeparation;

        [PacketizerIgnore]
        private Vector2 _lastCohesion;

        [PacketizerIgnore]
        private Vector2 _lastFormation;

#if DEBUG

        public Vector2 GetLastEscape()
        {
            return _lastEscape;
        }

        public Vector2 GetLastSeparation()
        {
            return _lastSeparation;
        }

        public Vector2 GetLastCohesion()
        {
            return _lastCohesion;
        }

        public Vector2 GetLastFormation()
        {
            return _lastFormation;
        }

#endif

        [Conditional("DEBUG")]
        private void SetLastEscape(Vector2 value)
        {
            _lastEscape = value;
        }

        [Conditional("DEBUG")]
        private void SetLastSeparation(Vector2 value)
        {
            _lastSeparation = value;
        }

        [Conditional("DEBUG")]
        private void SetLastCohesion(Vector2 value)
        {
            _lastCohesion = value;
        }

        [Conditional("DEBUG")]
        private void SetLastFormation(Vector2 value)
        {
            _lastFormation = value;
        }

        #endregion
    }
}