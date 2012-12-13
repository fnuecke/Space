using System.Collections.Generic;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Space.ComponentSystem.Components.Behaviors;

namespace Space.ComponentSystem.Components
{
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

        #region Constants

        /// <summary>
        /// Index group containing all entities with an AI component.
        /// </summary>
        public static readonly ulong AIIndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The randomizer we use to make pseudo random decisions.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        /// <summary>
        /// The currently running behaviors, ordered as they were issued.
        /// </summary>
        private readonly Stack<Behavior.BehaviorType> _currentBehaviors = new Stack<Behavior.BehaviorType>();

        /// <summary>
        /// List of all possible behaviors. This keeps us from having to
        /// re-allocate them over and over again. The only down-side is, that
        /// we cannot stack multiple behaviors of the same type, but that's
        /// probably not needed anyway.
        /// </summary>
        private readonly Dictionary<Behavior.BehaviorType, Behavior> _behaviors = new Dictionary<Behavior.BehaviorType, Behavior>();

        #endregion

        #region Initialization

        public ArtificialIntelligence()
        {
            _behaviors.Add(Behavior.BehaviorType.Roam, new RoamBehavior(this, _random));
            _behaviors.Add(Behavior.BehaviorType.Attack, new AttackBehavior(this, _random));
            _behaviors.Add(Behavior.BehaviorType.Move, new MoveBehavior(this, _random));
            _behaviors.Add(Behavior.BehaviorType.AttackMove, new AttackMoveBehavior(this, _random));
            _behaviors.Add(Behavior.BehaviorType.Guard, new GuardBehavior(this, _random));
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
        /// <returns>This instance.</returns>
        public ArtificialIntelligence Initialize(ulong seed)
        {
            _random.Seed(seed);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

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

        #endregion

        #region Commands

        /// <summary>
        /// Makes the AI attack move to the nearest friendly station, then
        /// makes it dock there (removing the instance from the simulation).
        /// </summary>
        public void Dock()
        {
            PushBehavior(Behavior.BehaviorType.Dock);
        }

        /// <summary>
        /// Makes the AI roams the specified area.
        /// </summary>
        /// <param name="area">The area.</param>
        public void Roam(ref FarRectangle area)
        {
            ((RoamBehavior)_behaviors[Behavior.BehaviorType.Roam]).Area = area;
            PushBehavior(Behavior.BehaviorType.Roam);
        }

        /// <summary>
        /// Makes the AI attack the specified entity.
        /// </summary>
        /// <param name="entity">The entity to attack.</param>
        public void Attack(int entity)
        {
            ((AttackBehavior)_behaviors[Behavior.BehaviorType.Attack]).Target = entity;
            PushBehavior(Behavior.BehaviorType.Attack);
        }

        /// <summary>
        /// Tells the AI to attack-move to the specified location.
        /// </summary>
        /// <param name="target">The target to move to.</param>
        public void AttackMove(ref FarPosition target)
        {
            ((AttackMoveBehavior)_behaviors[Behavior.BehaviorType.AttackMove]).Target = target;
            PushBehavior(Behavior.BehaviorType.AttackMove);
        }

        /// <summary>
        /// Tells the AI to guard the specified entity.
        /// </summary>
        /// <param name="entity">The entity to guard.</param>
        public void Guard(int entity)
        {
            ((GuardBehavior)_behaviors[Behavior.BehaviorType.Guard]).Target = entity;
            PushBehavior(Behavior.BehaviorType.Guard);
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
        private void PushBehavior(Behavior.BehaviorType behavior)
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

            packet.Write(_random);
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
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            packet.ReadPacketizableInto(ref _random);
            _currentBehaviors.Clear();
            var numBehaviors = packet.ReadInt32();
            for (var i = 0; i < numBehaviors; i++)
            {
                _currentBehaviors.Push((Behavior.BehaviorType)packet.ReadByte());
            }

            foreach (var behaviorType in _behaviors.Keys)
            {
                var behavior = _behaviors[behaviorType];
                packet.ReadPacketizableInto(ref behavior);
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
            return base.ToString() + ", Random=" + _random + ", CurrentBehaviors=[" + string.Join(", ", _currentBehaviors) + "], Behaviors=[" + string.Join(", ", _behaviors) + "]";
        }

        #endregion
    }
}