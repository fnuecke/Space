using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.Behaviours;

namespace Space.ComponentSystem.Components
{
    public sealed class ArtificialIntelligence : Component
    {
        #region Types

        public enum Order
        {
            Move,
            Guard,
            Attack
        }

        public sealed class AiCommand : IPacketizable
        {
            public Vector2 Target;

            public int MaxDistance;

            public Order Order;

            public int OriginEntity;

            public int TargetEntity;

            public AiCommand(Vector2 target, int maxDistance, Order order)
            {
                Target = target;
                MaxDistance = maxDistance;
                this.Order = order;
            }

            public AiCommand(int target, int maxDistance, Order order, int origin = 0)
            {
                TargetEntity = target;
                MaxDistance = maxDistance;
                this.Order = order;
                OriginEntity = origin;
            }

            public AiCommand()
            {
            }

            public Packet Packetize(Packet packet)
            {
                return packet.Write(Target)
                    .Write(MaxDistance)
                    .Write((byte)Order);
            }

            public void Depacketize(Packet packet)
            {
                Target = packet.ReadVector2();
                MaxDistance = packet.ReadInt32();
                Order = (Order)packet.ReadByte();
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The command this entity has
        /// </summary>
        public AiCommand Command;

        /// <summary>
        /// The current behaviour
        /// </summary>
        private Behavior.BehaviorType _currentBehaviorType;

        private Dictionary<Behavior.BehaviorType, Behavior> _behaviours = new Dictionary<Behavior.BehaviorType, Behavior>();

        /// <summary>
        /// A counter used to only update every few milliseconds
        /// </summary>
        private int _counter;

        private bool _returning;

        #endregion

        #region Initialization

        public ArtificialIntelligence()
        {
            _behaviours.Add(Behavior.BehaviorType.Patrol, new PatrolBehavior(this));
            _behaviours.Add(Behavior.BehaviorType.Move, new MoveBehavior(this));
            _behaviours.Add(Behavior.BehaviorType.Attack, new AttackBehavior(this));
        }

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherAI = (AI)other;
            Command = otherAI.Command;
            _currentBehaviorType = otherAI._currentBehaviorType;
            _counter = otherAI._counter;
            foreach (var behaviour in otherAI._behaviours)
            {
                _behaviours[behaviour.Key] = behaviour.Value.DeepCopy(_behaviours[behaviour.Key]);
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

            Command = null;
            _currentBehaviorType = Behavior.BehaviorType.None;
            _counter = 0;
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet).WriteWithTypeInfo((IPacketizable)_currentBehaviorType)
                .Write(Command)
                .Write(_counter);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _currentBehaviorType = packet.ReadPacketizableWithTypeInfo<Behavior>();
            _currentBehaviorType.AiComponent = this;
            Command = packet.ReadPacketizable<AiCommand>();
            _counter = packet.ReadInt32();
        }

        #endregion
    }
}