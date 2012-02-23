using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
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
            /// AI follows a specified aiComponent and tries to protect it by
            /// attacking enemies that get in range.
            /// </summary>
            Escort
        }

        #endregion

        #region Constants

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
        private const float FlockingThreshold = 600;

        /// <summary>
        /// The desired distance to keep to other flock members.
        /// </summary>
        protected const float FlockingSeparation = 300;

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
        /// <param name="pollRate">The poll rate.</param>
        protected Behavior(ArtificialIntelligence ai, int pollRate)
        {
            this.AI = ai;
            _pollRate = pollRate;
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
            _ticksToWait = _pollRate;

            // Figure out where we want to go.
            var targetPosition = GetTargetPosition();

            // And accordingly, which way to accelerate to get there.
            var direction = targetPosition - AI.Manager.GetComponent<Transform>(AI.Entity).Translation;

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
            var shipControl = AI.Manager.GetComponent<ShipControl>(AI.Entity);
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
        protected virtual Vector2 GetTargetPosition()
        {
            // Per default we just stand still.
            return AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
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
            var flightDirection = direction - AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            return (float)Math.Atan2(flightDirection.Y, flightDirection.X);
        }

        #endregion

        #region Vegetative nervous system

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
            var faction = AI.Manager.GetComponent<Faction>(AI.Entity).Value;
            var info = AI.Manager.GetComponent<ShipInfo>(AI.Entity);
            var position = info.Position;
            var mass = info.Mass;

            // Look for evil neighbors, in particular suns and the like.
            var index = AI.Manager.GetSystem<IndexSystem>();
            foreach (var neighbor in index.RangeQuery(ref position, MaxEscapeCheckDistance, Detectable.IndexGroup))
            {
                // If it does damage we want to keep our distance.
                var neighborFaction = AI.Manager.GetComponent<Faction>(neighbor);
                var neighborCollisionDamage = AI.Manager.GetComponent<CollisionDamage>(neighbor);
                if (neighborCollisionDamage == null ||
                    (neighborFaction != null && (neighborFaction.Value & faction) != 0))
                {
                    // Either this one does no damage, or it's a friendly.
                    continue;
                }

                // Does it pull?
                var neighborGravitation = AI.Manager.GetComponent<Gravitation>(neighbor);
                var neighborPosition = AI.Manager.GetComponent<Transform>(neighbor).Translation;
                var toNeighbor = position - neighborPosition;

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
            
            // Check all neighbors in normal flocking range.
            foreach (var neighbor in index.RangeQuery(ref position, FlockingThreshold, Detectable.IndexGroup))
            {
                // Ignore non-ships.
                if (AI.Manager.GetComponent<ShipControl>(neighbor) == null)
                {
                    continue;
                }

                // Get the position, needed for everything that follows.
                var neighborPosition = AI.Manager.GetComponent<Transform>(neighbor).Translation;
                var toNeighbor = position - neighborPosition;

                // See if separation kicks in.
                if (toNeighbor.LengthSquared() < FlockingSeparation * FlockingSeparation)
                {
                    // Yes. The closer we are, the faster we want to get away.
                    if (toNeighbor.LengthSquared() > 0)
                    {
                        // Somewhere outside the other object (which should
                        // really be the normal case).
                        toNeighbor.Normalize();
                        direction += FlockingSeparation * toNeighbor;
                    }
                    // Else we're exactly inside the other object... this
                    // is so unlikely that we just won't bother handling it.
                }
                else
                {
                    // No, check if it's a friend, because if it is, we want to flock!
                    var neighborFaction = AI.Manager.GetComponent<Faction>(neighbor);
                    if ((faction & neighborFaction.Value) != 0)
                    {
                        // Friend, add to cohesion and alignment.
                        direction.X += toNeighbor.X;
                        direction.Y += toNeighbor.Y;

                        var neighborVelocity = AI.Manager.GetComponent<Velocity>(neighbor);
                        direction.X += neighborVelocity.Value.X;
                        direction.Y += neighborVelocity.Value.Y;
                    }
                }
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
        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(_ticksToWait));
        }

        #endregion

        #region Copying
        
        /// <summary>
        /// Creates a deep copy of the object.
        /// </summary>
        /// <returns>The copy.</returns>
        public Behavior DeepCopy()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual Behavior DeepCopy(Behavior into)
        {
            if (into.GetType() != GetType())
            {
                throw new ArgumentException("Invalid instance, type mismatch.", "into");
            }

            into._ticksToWait = _ticksToWait;

            return into;
        }

        #endregion
    }
}
