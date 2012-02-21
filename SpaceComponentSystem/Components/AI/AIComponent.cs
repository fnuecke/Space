using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.Behaviours;

namespace Space.ComponentSystem.Components
{
    public sealed class AiComponent : Component
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
        private Behaviour.Behaviours _currentBehaviour;

        private Dictionary<Behaviour.Behaviours, Behaviour> _behaviours = new Dictionary<Behaviour.Behaviours, Behaviour>();

        /// <summary>
        /// A counter used to only update every few milliseconds
        /// </summary>
        private int _counter;

        private bool _returning;

        #endregion

        #region Initialization

        public AiComponent()
        {
            _behaviours.Add(Behaviour.Behaviours.Patrol, new PatrolBehaviour(this));
            _behaviours.Add(Behaviour.Behaviours.Move, new MoveBehaviour(this));
            _behaviours.Add(Behaviour.Behaviours.Attack, new AttackBehaviour(this));
        }

        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherAI = (AiComponent)other;
            Command = otherAI.Command;
            _currentBehaviour = otherAI._currentBehaviour;
            _counter = otherAI._counter;
            foreach (var behaviour in otherAI._behaviours)
            {
                _behaviours[behaviour.Key] = behaviour.Value.DeepCopy(_behaviours[behaviour.Key]);
            }

            return this;
        }

        public override Component DeepCopy(Component into)
        {
            var copy = (AiComponent)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Command = Command;
                copy._counter = _counter;
            }
            else
            {
                copy._currentBehaviour = _currentBehaviour.DeepCopy();
                copy._behaviours = new Dictionary<Behaviour.Behaviours, Behaviour>();
                copy._behaviours.Add(Behaviour.Behaviours.Patrol, new PatrolBehaviour(copy));
                copy._behaviours.Add(Behaviour.Behaviours.Move, new MoveBehaviour(copy));
                copy._behaviours.Add(Behaviour.Behaviours.Attack, new AttackBehaviour(copy));
            }

            return copy;
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .WriteWithTypeInfo(_currentBehaviour)
                .Write(Command)
                .Write(_counter);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _currentBehaviour = packet.ReadPacketizableWithTypeInfo<Behaviour>();
            _currentBehaviour.AiComponent = this;
            Command = packet.ReadPacketizable<AiCommand>();
            _counter = packet.ReadInt32();
        }

        #endregion
    }
}