using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.AI.Behaviors;

namespace Space.ComponentSystem.Components
{
    public sealed class ArtificialIntelligence : Component
    {
        #region Fields

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
            _behaviors.Add(Behavior.BehaviorType.Roam, new RoamBehavior(this));
            _behaviors.Add(Behavior.BehaviorType.Attack, new AttackBehavior(this));
            _behaviors.Add(Behavior.BehaviorType.Move, new MoveBehavior(this));
            _behaviors.Add(Behavior.BehaviorType.AttackMove, new AttackMoveBehavior(this));
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
            foreach (var behavior in otherAI._currentBehaviors)
            {
                _currentBehaviors.Push(behavior);
            }
            foreach (var behavior in otherAI._behaviors)
            {
                _behaviors[behavior.Key] = behavior.Value.DeepCopy(_behaviors[behavior.Key]);
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

            _currentBehaviors.Clear();
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
        public void Roam(ref Rectangle area)
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
        public void AttackMove(ref Vector2 target)
        {
            ((AttackMoveBehavior)_behaviors[Behavior.BehaviorType.AttackMove]).Target = target;
            PushBehavior(Behavior.BehaviorType.AttackMove);
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

            packet.Write(_currentBehaviors.Count);
            foreach (var behavior in _currentBehaviors)
            {
                packet.Write((byte)behavior);
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

            _currentBehaviors.Clear();
            var numBehaviors = packet.ReadInt32();
            for (int i = 0; i < numBehaviors; i++)
            {
                _currentBehaviors.Push((Behavior.BehaviorType)packet.ReadByte());
            }

            foreach (var behavior in _behaviors.Values)
            {
                packet.ReadPacketizableInto(behavior);
            }
        }

        #endregion
    }
}