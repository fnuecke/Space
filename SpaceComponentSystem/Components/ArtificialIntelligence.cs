using System.Collections.Generic;
using System.Globalization;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.Behaviors;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component adds an AI controller to a ship.
    /// </summary>
    public sealed class ArtificialIntelligence : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

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

        /// <summary>
        /// This class contains some settings controlling an AI's behavior.
        /// </summary>
        public sealed class AIConfiguration : IPacketizable, IHashable, ICopyable<AIConfiguration>
        {
            #region Fields
            
            /// <summary>
            /// The distance up to which the AI will scan for enemies to attack.
            /// </summary>
            public float AggroRange = 1000;

            /// <summary>
            /// The radius in which we look for dangerous objects that we may
            /// want to avoid (damaging entities, possibly with gravity and
            /// normal enemies).
            /// </summary>
            public float MaxEscapeCheckDistance = 8000;

            /// <summary>
            /// How far away we want to stay from objects that hurt us, but don't
            /// attract us (i.e. have no gravitational pull). For those with gravity
            /// the distance is computed dynamically, based on the point of no return.
            /// </summary>
            public float MinDistanceToDamagers = 1000;

            /// <summary>
            /// For damagers that have a gravitational pull, this is the multiple
            /// of the distance that represents the point of no return (i.e. the
            /// point where our thrusters won't be enough to get away anymore)...
            /// the multiple of the point of no return we want to at least stay
            /// away form the damager.
            /// </summary>
            public float MinMultipleOfPointOfNoReturn = 2;

            /// <summary>
            /// How far away from enemy units AI ships will try to stay (this
            /// avoids them flying *into* their attack targets).
            /// TODO per unit dynamically based on attack range
            /// </summary>
            public float EnemySeparation = 500;

            /// <summary>
            /// The distance to another ship we need to be under for flocking
            /// to kick in (in particular for cohesion/alignment).
            /// </summary>
            public float FlockingThreshold = 400;

            /// <summary>
            /// The desired distance to keep to other flock members.
            /// </summary>
            public float FlockingSeparation = 200;

            /// <summary>
            /// The distance (scale) at which our vegetative input is considered
            /// urgent, i.e. is normalized to 1. Everything below will be scaled
            /// to the interval of [0, 1).
            /// </summary>
            public float VegetativeUrgencyDistance = 500;

            /// <summary>
            /// How important our vegetative direction comes into play. One means
            /// it's 50:50 with other behavior input, 0 means it's only other
            /// behavioral input.
            /// </summary>
            public float VegetativeWeight = 2;

            /// <summary>
            /// How much earlier to start firing our weapons, before the target
            /// enters our range of fire. This way we have a chance some shots
            /// will hit the target because it's flying into them.
            /// </summary>
            public float WeaponRangeEpsilon = 100;

            /// <summary>
            /// The angle ahead of the AI ship in which an enemy ship must be for
            /// the AI to start shooting (this avoids shooting if the enemy is
            /// behind the AI, which would be a waste of energy).
            /// </summary>
            public float WeaponFiringAngle = MathHelper.ToRadians(90);

            /// <summary>
            /// Distance we have to have to where we were when we started chasing
            /// our target before we give up and fall back to our previous behavior.
            /// </summary>
            public float ChaseDistance = 2000;

            #endregion

            #region Copying

            /// <summary>
            /// Creates a new copy of the object, that shares no mutable
            /// references with this instance.
            /// </summary>
            /// <returns>The copy.</returns>
            public AIConfiguration NewInstance()
            {
                return new AIConfiguration();
            }

            /// <summary>
            /// Creates a deep copy of the object, reusing the given object.
            /// </summary>
            /// <param name="into">The object to copy into.</param>
            /// <returns>The copy.</returns>
            public void CopyInto(AIConfiguration into)
            {
                into.AggroRange = AggroRange;
                into.MaxEscapeCheckDistance = MaxEscapeCheckDistance;
                into.MinDistanceToDamagers = MinDistanceToDamagers;
                into.MinMultipleOfPointOfNoReturn = MinMultipleOfPointOfNoReturn;
                into.EnemySeparation = EnemySeparation;
                into.FlockingThreshold = FlockingThreshold;
                into.FlockingSeparation = FlockingSeparation;
                into.VegetativeUrgencyDistance = VegetativeUrgencyDistance;
                into.VegetativeWeight = VegetativeWeight;
                into.WeaponRangeEpsilon = WeaponRangeEpsilon;
                into.WeaponFiringAngle = WeaponFiringAngle;
                into.ChaseDistance = ChaseDistance;
            }

            #endregion

            #region Serialization / Hashing
            
            /// <summary>
            /// Write the object's state to the given packet.
            /// </summary>
            /// <param name="packet">The packet to write the data to.</param>
            /// <returns>
            /// The packet after writing.
            /// </returns>
            public Packet Packetize(Packet packet)
            {
                return packet;
            }

            /// <summary>
            /// Bring the object to the state in the given packet. This is called
            /// before automatic depacketization is performed.
            /// </summary>
            /// <param name="packet">The packet to read from.</param>
            public void PreDepacketize(Packet packet)
            {
            }

            /// <summary>
            /// Bring the object to the state in the given packet. This is called
            /// after automatic depacketization has been performed.
            /// </summary>
            /// <param name="packet">The packet to read from.</param>
            public void PostDepacketize(Packet packet)
            {
            }

            /// <summary>
            /// Push some unique data of the object to the given hasher,
            /// to contribute to the generated hash.
            /// </summary>
            /// <param name="hasher">The hasher to push data to.</param>
            public void Hash(Hasher hasher)
            {
                hasher.Put(AggroRange);
                hasher.Put(MaxEscapeCheckDistance);
                hasher.Put(MinDistanceToDamagers);
                hasher.Put(MinMultipleOfPointOfNoReturn);
                hasher.Put(EnemySeparation);
                hasher.Put(FlockingThreshold);
                hasher.Put(FlockingSeparation);
                hasher.Put(VegetativeUrgencyDistance);
                hasher.Put(VegetativeWeight);
                hasher.Put(WeaponRangeEpsilon);
                hasher.Put(WeaponFiringAngle);
                hasher.Put(ChaseDistance);
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
                var h = new Hasher();
                h.Put(this);
                return h.Value.ToString(CultureInfo.InvariantCulture);
            }

            #endregion
        }

        #endregion

        #region Constants

        /// <summary>
        /// Index group containing all entities with an AI component.
        /// </summary>
        public static readonly ulong AIIndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the (mutable) configuration for this AI, controlling its behavior.
        /// </summary>
        public AIConfiguration Configuration
        {
            get { return _config; }
        }

        /// <summary>
        /// Gets the current behavior type.
        /// </summary>
        public BehaviorType CurrentBehavior
        {
            get { return _currentBehaviors.Count > 0 ? _currentBehaviors.Peek() : BehaviorType.None; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The randomizer we use to make pseudo random decisions.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        /// <summary>
        /// The configuration for this AI instance, controlling its behavior.
        /// </summary>
        private AIConfiguration _config = new AIConfiguration();

        /// <summary>
        /// The currently running behaviors, ordered as they were issued.
        /// </summary>
        [PacketizerIgnore]
        private readonly Stack<BehaviorType> _currentBehaviors = new Stack<BehaviorType>();

        /// <summary>
        /// List of all possible behaviors. This keeps us from having to
        /// re-allocate them over and over again. The only down-side is, that
        /// we cannot stack multiple behaviors of the same type, but that's
        /// probably not needed anyway.
        /// </summary>
        [PacketizerIgnore]
        private readonly Dictionary<BehaviorType, Behavior> _behaviors = new Dictionary<BehaviorType, Behavior>();

        #endregion

        #region Initialization

        public ArtificialIntelligence()
        {
            _behaviors.Add(BehaviorType.Roam, new RoamBehavior(this, _random));
            _behaviors.Add(BehaviorType.Attack, new AttackBehavior(this, _random));
            _behaviors.Add(BehaviorType.Move, new MoveBehavior(this, _random));
            _behaviors.Add(BehaviorType.AttackMove, new AttackMoveBehavior(this, _random));
            _behaviors.Add(BehaviorType.Guard, new GuardBehavior(this, _random));
        }

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherAI = (ArtificialIntelligence)other;
            otherAI._random.CopyInto(_random);
            otherAI._config.CopyInto(_config);
            _currentBehaviors.Clear();
            var behaviorTypes = otherAI._currentBehaviors.ToArray();
            // Stacks iterators work backwards (first is the last pushed element),
            // so we need to iterate backwards.
            for (var i = behaviorTypes.Length; i > 0; --i)
            {
                _currentBehaviors.Push(behaviorTypes[i - 1]);
            }
            foreach (var entry in otherAI._behaviors)
            {
                entry.Value.CopyInto(_behaviors[entry.Key]);
            }

            return this;
        }

        /// <summary>
        /// Initializes the AI with the specified random seed.
        /// </summary>
        /// <param name="seed">The seed to use.</param>
        /// <param name="config">The configuration to use for this AI.</param>
        /// <returns>
        /// This instance.
        /// </returns>
        public ArtificialIntelligence Initialize(ulong seed, AIConfiguration config = null)
        {
            _random.Seed(seed);
            if (config != null)
            {
                _config = config;
            }

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _random.Seed(0);
            _config = new AIConfiguration();
            _currentBehaviors.Clear();
            foreach (var behavior in _behaviors.Values)
            {
                behavior.Reset();
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the current behavior of this AI.
        /// </summary>
        public void Update()
        {
            // Update our current leading behavior, if we have one.
            if (_currentBehaviors.Count > 0)
            {
                _behaviors[_currentBehaviors.Peek()].Update();
            }
        }

        /// <summary>
        /// Called when an entity becomes an invalid target (removed from the
        /// system or died). This is intended to allow behaviors to stop in
        /// case their related entity is removed (e.g. target when attacking).
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        internal void OnEntityInvalidated(int entity)
        {
            foreach (var behavior in _behaviors)
            {
                behavior.Value.OnEntityInvalidated(entity);
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Makes the AI attack move to the nearest friendly station, then
        /// makes it dock there (removing the instance from the simulation).
        /// </summary>
        public void Dock()
        {
            PushBehavior(BehaviorType.Dock);
        }

        /// <summary>
        /// Makes the AI roams the specified area.
        /// </summary>
        /// <param name="area">The area.</param>
        public void Roam(ref FarRectangle area)
        {
            ((RoamBehavior)_behaviors[BehaviorType.Roam]).Area = area;
            PushBehavior(BehaviorType.Roam);
        }

        /// <summary>
        /// Makes the AI attack the specified entity.
        /// </summary>
        /// <param name="entity">The entity to attack.</param>
        public void Attack(int entity)
        {
            ((AttackBehavior)_behaviors[BehaviorType.Attack]).Target = entity;
            PushBehavior(BehaviorType.Attack);
        }

        /// <summary>
        /// Tells the AI to attack-move to the specified location.
        /// </summary>
        /// <param name="target">The target to move to.</param>
        public void AttackMove(ref FarPosition target)
        {
            ((AttackMoveBehavior)_behaviors[BehaviorType.AttackMove]).Target = target;
            PushBehavior(BehaviorType.AttackMove);
        }

        /// <summary>
        /// Tells the AI to guard the specified entity.
        /// </summary>
        /// <param name="entity">The entity to guard.</param>
        public void Guard(int entity)
        {
            ((GuardBehavior)_behaviors[BehaviorType.Guard]).Target = entity;
            PushBehavior(BehaviorType.Guard);
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Pops the top-most behavior, returning to the previous one.
        /// </summary>
        internal void PopBehavior()
        {
            _currentBehaviors.Pop();
        }

        /// <summary>
        /// Pushes the behavior to the stack, making it the executing one.
        /// </summary>
        /// <param name="behavior">The behavior to push.</param>
        private void PushBehavior(BehaviorType behavior)
        {
            // Remove all behaviors induced by this type of behavior and
            // the old instance, as well, then push the new behavior.
            while (_currentBehaviors.Contains(behavior))
            {
                _currentBehaviors.Pop();
            }
            _currentBehaviors.Push(behavior);
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
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_currentBehaviors.Count);
            var behaviorTypes = _currentBehaviors.ToArray();
            // Stacks iterators work backwards (first is the last pushed element),
            // so we need to iterate backwards.
            for (var i = behaviorTypes.Length; i > 0; --i)
            {
                packet.Write((byte)behaviorTypes[i - 1]);
            }

            foreach (var behavior in _behaviors.Values)
            {
                packet.Write(behavior);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void PostDepacketize(Packet packet)
        {
            base.PostDepacketize(packet);

            _currentBehaviors.Clear();
            var numBehaviors = packet.ReadInt32();
            for (var i = 0; i < numBehaviors; i++)
            {
                _currentBehaviors.Push((BehaviorType)packet.ReadByte());
            }

            foreach (var behaviorType in _behaviors.Keys)
            {
                var behavior = _behaviors[behaviorType];
                packet.ReadPacketizableInto(behavior);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(_random);
            hasher.Put(_config);
            foreach (var behaviorType in _currentBehaviors)
            {
                hasher.Put((byte)behaviorType);
            }
            hasher.Put(_behaviors.Values);
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
            return base.ToString() + ", Random=" + _random + ", Config=" + _config + ", CurrentBehaviors=[" + string.Join(", ", _currentBehaviors) + "], Behaviors=[" + string.Join(", ", _behaviors) + "]";
        }

        #endregion

        #region Debugging

        // The following are just accessors for th AI debug render system.

        public Vector2 GetLastEscape()
        {
#if DEBUG
            if (_currentBehaviors.Count > 0)
            {
                return _behaviors[_currentBehaviors.Peek()].GetLastEscape();
            }
#endif
            return Vector2.Zero;
        }

        public Vector2 GetLastSeparation()
        {
#if DEBUG
            if (_currentBehaviors.Count > 0)
            {
                return _behaviors[_currentBehaviors.Peek()].GetLastSeparation();
            }
#endif
            return Vector2.Zero;
        }

        public Vector2 GetLastCohesion()
        {
#if DEBUG
            if (_currentBehaviors.Count > 0)
            {
                return _behaviors[_currentBehaviors.Peek()].GetLastCohesion();
            }
#endif
            return Vector2.Zero;
        }

        public Vector2 GetLastFormation()
        {
#if DEBUG
            if (_currentBehaviors.Count > 0)
            {
                return _behaviors[_currentBehaviors.Peek()].GetLastFormation();
            }
#endif
            return Vector2.Zero;
        }

        public Vector2 GetBehaviorTargetDirection()
        {
#if DEBUG
            if (_currentBehaviors.Count > 0)
            {
                var behaviorType = _currentBehaviors.Peek();
                var behavior = _behaviors[behaviorType];
                switch (behaviorType)
                {
                    case BehaviorType.Attack:
                    {
                        var target = ((AttackBehavior)behavior).Target;
                        if (Manager.HasEntity(target))
                        {
                            var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                            var targetTransform = (Transform)Manager.GetComponent(target, Transform.TypeId);
                            return (Vector2)(targetTransform.Translation - transform.Translation);
                        }
                        break;
                    }
                    case BehaviorType.Move:
                    {
                        var target = ((MoveBehavior)behavior).Target;
                        var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                        return (Vector2)(target - transform.Translation);
                    }
                    case BehaviorType.AttackMove:
                    {
                        var target = ((AttackMoveBehavior)behavior).Target;
                        var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                        return (Vector2)(target - transform.Translation);
                    }
                    case BehaviorType.Guard:
                    {
                        var target = ((GuardBehavior)behavior).Target;
                        if (Manager.HasEntity(target))
                        {
                            var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                            var targetTransform = (Transform)Manager.GetComponent(target, Transform.TypeId);
                            return (Vector2)(targetTransform.Translation - transform.Translation);
                        }
                        break;
                    }
                }
            }
#endif
            return Vector2.Zero;
        }

        #endregion
    }
}