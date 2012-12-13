using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Base class for AI ship behaviors. This implements rudimentary
    /// functionality that can be viewed as the vegetative nervous system
    /// of the AI. For example, it tries to keep the AI away from danger,
    /// and 'automatically' navigates to a desired destination.
    /// </summary>
    internal abstract class Behavior : IPacketizable, IHashable, ICopyable<Behavior>
    {
        #region Types
        
        /// <summary>
        /// Possible AI behaviors.
        /// </summary>
        public enum BehaviorType
        {
            /// <summary>
            /// No behavior at all. Note that this does not mean the AI will
            /// stop, but that it simply won't change its state anymore.
            /// </summary>
            None,

            /// <summary>
            /// Makes an AI patrol to the nearest friendly station and then
            /// dock at it, removing the AI from the game. This is a 'cleanup'
            /// behavior to get rid of ships that have lost their purpose.
            /// </summary>
            Dock,

            /// <summary>
            /// Performs a random walk of the AI in the range of a specified
            /// point, using the patrol behavior.
            /// </summary>
            Roam,

            /// <summary>
            /// Emulates trading behavior by making the AI fly from one
            /// friendly base to another, picking the next station to fly to
            /// randomly.
            /// </summary>
            Trade,

            /// <summary>
            /// AI tries to kill a specified aiComponent.
            /// </summary>
            Attack,

            /// <summary>
            /// AI moves to a specified point, ignoring what it flies past.
            /// </summary>
            Move,

            /// <summary>
            /// AI moves to a specified point, but checks if it gets in range
            /// of enemy ships it will then attack.
            /// </summary>
            AttackMove,

            /// <summary>
            /// AI follows a specified entity and tries to protect it by
            /// attacking enemies that get in range.
            /// </summary>
            Guard
        }

        #endregion

        #region Constants

        /// <summary>
        /// The distance enemy units must get closer than for us to attack
        /// them.
        /// </summary>
        protected const float DefaultAggroRange = 2500;

        /// <summary>
        /// The radius around ourself we check for objects we want to evade.
        /// </summary>
        private const float MaxEscapeCheckDistance = 8000;

        /// <summary>
        /// How far away we want to stay from objects that hurt us, but don't
        /// attract us (i.e. have no gravitational pull).
        /// </summary>
        private const float MinDistanceToDamagers = 1000;

        /// <summary>
        /// The distance to another ship we need to be under for flocking
        /// to kick in.
        /// </summary>
        private const float FlockingThreshold = 100;

        /// <summary>
        /// The desired distance to keep to other flock members.
        /// </summary>
        protected const float FlockingSeparation = 30;

        /// <summary>
        /// For damagers that have a gravitational pull, this is the multiple
        /// of the distance that represents the point of no return (i.e. the
        /// point where our thrusters won't be enough to get away anymore)...
        /// the multiple of the point of no return we want to at least stay
        /// away form the damager.
        /// </summary>
        private const float MinMultipleOfPointOfNoReturn = 2;

        /// <summary>
        /// The distance (scale) at which our vegetative input is considered
        /// urgent, i.e. is normalized to 1. Everything below will be scaled
        /// to the interval of [0, 1).
        /// </summary>
        private const float VegetativeUrgencyDistance = 500;

        /// <summary>
        /// How important our vegetative direction comes into play. One means
        /// it's 50:50 with other behavior input, 0 means it's only other
        /// behavioral input.
        /// </summary>
        private const float VegetativeWeight = 2;

        #endregion

        #region Fields

        /// <summary>
        /// The AI component this behavior belongs to.
        /// </summary>
        protected readonly ArtificialIntelligence AI;

        /// <summary>
        /// The randomizer we use to make pseudo random decisions.
        /// </summary>
        /// <remarks>
        /// The "owner" of this instance is the AI component we belong to,
        /// so we do not need to take care of serialization or copying.
        /// </remarks>
        protected readonly IUniformRandom Random;

        /// <summary>
        /// The poll rate in ticks how often to update this behavior.
        /// </summary>
        private readonly int _pollRate;

        /// <summary>
        /// How many more ticks to wait before calling update on 
        /// </summary>
        private int _ticksToWait;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Behavior"/> class.
        /// </summary>
        /// <param name="ai">The AI component.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        /// <param name="pollRate">The poll rate in seconds.</param>
        protected Behavior(ArtificialIntelligence ai, IUniformRandom random, float pollRate)
        {
            AI = ai;
            Random = random;
            _pollRate = (int)(pollRate * Settings.TicksPerSecond);
        }

        /// <summary>
        /// Reset this behavior so it can be reused later on.
        /// </summary>
        public virtual void Reset()
        {
            _ticksToWait = 0;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the behavior and returns the behavior type to switch to.
        /// </summary>
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
            var direction = (Vector2)(targetPosition - ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation);

            // Normalize if it's not zero.
            var norm = direction.LengthSquared();
            if (norm > 0)
            {
                norm = (float)Math.Sqrt(norm);
                direction.X /= norm;
                direction.Y /= norm;
            }

            // Multiply with the desired acceleration.
            var speed = MathHelper.Clamp(GetThrusterPower(), 0, 1);
            direction.X *= speed;
            direction.Y *= speed;

            // Figure out where we want to go vegetatively (flocking).
            direction += GetVegetativeDirection() * VegetativeWeight;

            // Set our new acceleration direction and target rotation.
            var shipControl = ((ShipControl)AI.Manager.GetComponent(AI.Entity, ShipControl.TypeId));
            shipControl.SetAcceleration(direction);
            shipControl.SetTargetRotation(GetTargetRotation(ref direction));
        }

        #endregion

        #region Behavior type specifics

        /// <summary>
        /// Behavior specific update logic, e.g. checking for nearby enemies.
        /// </summary>
        /// <returns>
        /// Whether to do the rest of the update.
        /// </returns>
        protected abstract bool UpdateInternal();

        /// <summary>
        /// Figure out where we want to go.
        /// </summary>
        /// <returns>
        /// The coordinate we want to fly to.
        /// </returns>
        protected virtual FarPosition GetTargetPosition()
        {
            // Per default we just stand still.
            return ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
        }

        /// <summary>
        /// How fast do we want to fly, relative to our maximum speed?
        /// </summary>
        /// <returns>
        /// The relative speed we want to fly at.
        /// </returns>
        protected virtual float GetThrusterPower()
        {
            return 0.5f;
        }

        /// <summary>
        /// Gets the target rotation we want to be facing.
        /// </summary>
        /// <param name="direction">The direction we're accelerating in.</param>
        /// <returns>
        /// The desired target rotation.
        /// </returns>
        protected virtual float GetTargetRotation(ref Vector2 direction)
        {
            // Per default just head the way we're flying.
            return (float)Math.Atan2(direction.Y, direction.X);
        }

        #endregion

        #region Vegetative nervous system / Utility

        /// <summary>
        /// Filter function for <see cref="GetClosestEnemy"/> to only look for living
        /// enemies only.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>Whether to consider attacking that entity.</returns>
        protected bool HealthFilter(int entity)
        {
            var health = (Health)AI.Manager.GetComponent(entity, Health.TypeId);
            return health != null && health.Enabled && health.Value > 0;
        }

        /// <summary>
        /// Gets the closest enemy based on the specified criteria. This checks all
        /// enemies in the specified range, but only takes into consideration those
        /// that pass the filter function. The enemy with the lowest distance will
        /// be returned.
        /// </summary>
        /// <param name="range">The maximum range up to which to search.</param>
        /// <param name="filter">The filter function, if any.</param>
        /// <returns></returns>
        protected int GetClosestEnemy(float range, Func<int, bool> filter = null)
        {
            // See if there are any enemies nearby, if so attack them.
            var faction = ((Faction)AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var index = (IndexSystem)AI.Manager.GetSystem(IndexSystem.TypeId);
            var shipInfo = (ShipInfo)AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId);
            var sensorRange = shipInfo != null ? shipInfo.RadarRange : 0f;
            ISet<int> neighbors = new HashSet<int>();
            index.Find(position, sensorRange > 0 ? sensorRange : range, ref neighbors, DetectableSystem.IndexGroupMask);
            var closest = 0;
            var closestDistance = float.PositiveInfinity;
            foreach (var neighbor in neighbors)
            {
                // Friend or foe? Don't care if it's a friend. Also filter based on passed
                // filter function.
                // TODO: unless it's in a fight, then we might want to support our allies?
                //       i.e. check if it's behavior is 'Attack' and if so what its target is.
                //       Also, in that case don't jump... too far? I.e. add a range check for
                //       indirect targets.
                var neighborFaction = (Faction)AI.Manager.GetComponent(neighbor, Faction.TypeId);
                if (neighborFaction != null &&
                    (neighborFaction.Value & faction) == 0 &&
                    (filter == null || filter(neighbor)))
                {
                    // It's an enemy. Check the distance.
                    var enemyPosition = ((Transform)AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                    var distance = ((Vector2)(position - enemyPosition)).LengthSquared();
                    if (distance < closestDistance)
                    {
                        closest = neighbor;
                        closestDistance = distance;
                    }
                }
            }

            // Return whatever we found.
            return closest;
        }

        /// <summary>
        /// Gets the escape direction, i.e. the direction in which to
        /// accelerate to avoid bad things (stuff that hurts us on impact),
        /// together with a direction towards friendly ships with the
        /// direction those are flying in mixed in (flocking).
        /// </summary>
        /// <returns>
        /// The averaged direction away from potential danger and towards
        /// desired objects.
        /// </returns>
        private Vector2 GetVegetativeDirection()
        {
            // Accumulated directions we want to travel in.
            var direction = Vector2.Zero;

            // Get some info about ourself.
            var faction = ((Faction)AI.Manager.GetComponent(AI.Entity, Faction.TypeId)).Value;
            var squad = (Squad)AI.Manager.GetComponent(AI.Entity, Squad.TypeId);
            var info = ((ShipInfo)AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId));
            var position = info.Position;
            var mass = info.Mass;

            // Look for evil neighbors, in particular suns and the like.
            var index = (IndexSystem)AI.Manager.GetSystem(IndexSystem.TypeId);
            ISet<int> neighbors = new HashSet<int>();
            index.Find(position, MaxEscapeCheckDistance, ref neighbors, DetectableSystem.IndexGroupMask);
            foreach (var neighbor in neighbors)
            {
                // If it does damage we want to keep our distance.
                var neighborFaction = ((Faction)AI.Manager.GetComponent(neighbor, Faction.TypeId));
                var neighborCollisionDamage = ((CollisionDamage)AI.Manager.GetComponent(neighbor, CollisionDamage.TypeId));
                if (neighborCollisionDamage == null ||
                    (neighborFaction != null && (neighborFaction.Value & faction) != 0))
                {
                    // Either this one does no damage, or it's a friendly.
                    continue;
                }

                // Does it pull?
                var neighborGravitation = ((Gravitation)AI.Manager.GetComponent(neighbor, Gravitation.TypeId));
                var neighborPosition = ((Transform)AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                var toNeighbor = (Vector2)(position - neighborPosition);

                if (neighborGravitation != null &&
                    (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
                {
                    // Yes! Let's see how close we are comfortable to get.
                    var pointOfNoReturnSquared = mass * neighborGravitation.Mass / info.MaxAcceleration;
                    if (toNeighbor.LengthSquared() < pointOfNoReturnSquared * MinMultipleOfPointOfNoReturn * MinMultipleOfPointOfNoReturn)
                    {
                        // We're too close, let's pull out. Just use the square
                        // of the point of no return so it's really urgent.
                        toNeighbor.Normalize();
                        direction += MinMultipleOfPointOfNoReturn * pointOfNoReturnSquared * toNeighbor;
                    }
                }
                else
                {
                    // OK, just a damager, but doesn't pull us in. Scale
                    // to make us reach a certain minimum distance.
                    toNeighbor.Normalize();
                    direction += MinDistanceToDamagers * toNeighbor;
                }
            }
            
            // Check all neighbors in normal flocking range. If we're in a squad, skip
            // other squad members and take our squad position into account instead.
            neighbors.Clear();
            index.Find(position, FlockingThreshold, ref neighbors, DetectableSystem.IndexGroupMask);
            foreach (var neighbor in neighbors)
            {
                // Ignore non-ships.
                if (AI.Manager.GetComponent(neighbor, ShipControl.TypeId) == null)
                {
                    continue;
                }

                // Get the position, needed for everything that follows.
                var neighborPosition = ((Transform)AI.Manager.GetComponent(neighbor, Transform.TypeId)).Translation;
                var toNeighbor = (Vector2)(neighborPosition - position);

                // See if separation kicks in.
                if (toNeighbor.LengthSquared() < FlockingSeparation * FlockingSeparation)
                {
                    // Yes. The closer we are, the faster we want to get away.
                    if (toNeighbor.LengthSquared() > 0)
                    {
                        // Somewhere outside the other object (which should
                        // really be the normal case).
                        toNeighbor.Normalize();
                        direction -= FlockingSeparation * toNeighbor;
                    }
                    // Else we're exactly inside the other object... this
                    // is so unlikely that we just won't bother handling it.
                }
                else if (squad == null || AI.Entity == squad.Leader)
                {
                    // No, check if it's a friend, because if it is, we want to flock!
                    var neighborFaction = ((Faction)AI.Manager.GetComponent(neighbor, Faction.TypeId));
                    if ((faction & neighborFaction.Value) != 0)
                    {
                        // Friend, add to cohesion and alignment.
                        direction.X += toNeighbor.X;
                        direction.Y += toNeighbor.Y;

                        var neighborVelocity = ((Velocity)AI.Manager.GetComponent(neighbor, Velocity.TypeId));
                        direction.X += neighborVelocity.Value.X;
                        direction.Y += neighborVelocity.Value.Y;
                    }
                }
            }

            // Apply "formation flocking" for non-squad-leaders.
            if (squad != null && AI.Entity != squad.Leader)
            {
                var toLeader = (Vector2)(squad.ComputeFormationOffset() - position);

                direction.X += toLeader.X;
                direction.Y += toLeader.Y;
            }

            // If we have some influence, normalize it if necessary.
            direction.X /= VegetativeUrgencyDistance;
            direction.Y /= VegetativeUrgencyDistance;
            if (direction.LengthSquared() > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet
                .Write(_ticksToWait);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            _ticksToWait = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(_pollRate);
            hasher.Put(_ticksToWait);
        }

        #endregion

        #region Copying
        
        /// <summary>
        /// Creates a deep copy of the object.
        /// </summary>
        /// <returns>The copy.</returns>
        public Behavior NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual void CopyInto(Behavior into)
        {
            Debug.Assert(into.GetType().TypeHandle.Equals(GetType().TypeHandle));
            Debug.Assert(into != this);

            into._ticksToWait = _ticksToWait;
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
            return GetType().Name + ": TicksToWait=" + _ticksToWait;
        }

        #endregion
    }
}
