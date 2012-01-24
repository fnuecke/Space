using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.AIBehaviour;

namespace Space.ComponentSystem.Components
{
    sealed class AiComponent : AbstractComponent
    {
        #region Types

        public enum Order
        {
            Move,
            Guard,
            Attack
        }

        public class AiCommand : IPacketizable
        {
            public Vector2 Target;

            public int MaxDistance;

            public Order order;

            public AiCommand(Vector2 target, int maxDistance, Order order)
            {
                Target = target;
                MaxDistance = maxDistance;
                this.order = order;
            }

            public AiCommand()
            {
            }

            public Packet Packetize(Packet packet)
            {
                return packet.Write(Target)
                    .Write(MaxDistance)
                    .Write((byte)order);
            }

            public void Depacketize(Packet packet)
            {
                Target = packet.ReadVector2();
                MaxDistance = packet.ReadInt32();
                order = (Order)packet.ReadByte();
            }
        }

        #endregion

        #region Fields

        public AiCommand Command;

        private Behaviour _currentbehaviour;

        #endregion

        #region Constructor

        public AiComponent(AiCommand command)
        {
            Command = command;
            SwitchOrder();
        }

        public AiComponent()
        {
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
            CalculateBehaviour();
            _currentbehaviour.Update();
        }

        /// <summary>
        /// Calculates which behaviour to use
        /// </summary>
        private void CalculateBehaviour()
        {
            // Get local player's avatar. and position
            var info = Entity.GetComponent<ShipInfo>();
            var position = info.Position;

            //check if there are enemys in the erea
            if (_currentbehaviour is PatrolBehaviour)
            {
                CheckNeighbours(ref position, ref position);
            }
            else if (_currentbehaviour is AttackBehaviour)
            {
                var attack = (AttackBehaviour)_currentbehaviour;
                if (attack.TargetDead)
                {
                    CheckAndSwitchToMoveBehaviour(ref position);
                    return;
                }
                var targetEntity = Entity.Manager.GetEntity(attack.TargetEntity);

                if (targetEntity == null)
                {
                    CheckAndSwitchToMoveBehaviour(ref position);
                    return;
                }
                var health = targetEntity.GetComponent<Health>();
                var transform = targetEntity.GetComponent<Transform>();
                if (health == null || health.Value == 0 || transform == null)
                {
                    CheckAndSwitchToMoveBehaviour(ref position);
                    return;
                }

                var direction = position - transform.Translation;
                if (direction.Length() > 3000)
                {
                    CheckAndSwitchToMoveBehaviour(ref position);
                    return;
                }
                if ((position - ((AttackBehaviour)_currentbehaviour).StartPosition).Length() > Command.MaxDistance)
                {
                    var move = new MoveBehaviour
                    {
                        TargetPosition = ((AttackBehaviour)_currentbehaviour).StartPosition
                    };
                    _currentbehaviour = move;
                    return;
                }

            }
            else if (_currentbehaviour is MoveBehaviour)
            {
                var target = ((MoveBehaviour)_currentbehaviour).TargetPosition;
                if ((target - position).Length() < 200)
                    SwitchOrder();
                return;
            }
        }

        private void CheckAndSwitchToMoveBehaviour(ref Vector2 position)
        {
            var startposition = ((AttackBehaviour)_currentbehaviour).StartPosition;
            if (CheckNeighbours(ref position, ref startposition)) return;
            var move = new MoveBehaviour
                           {
                               TargetPosition = startposition,
                               AiComponent = this
                           };
            _currentbehaviour = move;
        }

        private bool CheckNeighbours(ref Vector2 position, ref Vector2 startPosition)
        {
            var currentFaction = Entity.GetComponent<Faction>().Value;
            var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
            if (index == null) return false;
            foreach (var neighbor in index.
           GetNeighbors(ref position, 3000, Detectable.IndexGroup))
            {
                var transform = neighbor.GetComponent<Transform>();
                if (transform == null) continue;
                var health = neighbor.GetComponent<Health>();
                if (health == null || health.Value == 0) continue;
                var faction = neighbor.GetComponent<Faction>();
                if (faction == null) continue;
                if ((faction.Value & currentFaction) == 0)
                {
                    var attack = new AttackBehaviour(this, neighbor.UID);
                    _currentbehaviour = attack;
                    attack.StartPosition = startPosition;
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// Calculates the Current Behaviour according to the given command
        /// </summary>
        private void SwitchOrder()
        {
            switch (Command.order)
            {
                case (Order.Guard):
                    _currentbehaviour = new PatrolBehaviour(this);
                    break;
                default:
                    _currentbehaviour = new PatrolBehaviour(this);
                    break;
            }
        }

        public override void HandleMessage<T>(ref T message)
        {
            if (_currentbehaviour is AttackBehaviour && message is EntityRemoved)
            {
                var beh = (AttackBehaviour)_currentbehaviour;
                if (((EntityRemoved)((ValueType)message)).Entity.UID == beh.TargetEntity)
                    beh.TargetDead = true;
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .WriteWithTypeInfo(_currentbehaviour)
                .Write(Command);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _currentbehaviour = packet.ReadPacketizableWithTypeInfo<Behaviour>();
            _currentbehaviour.AiComponent = this;

            Command = packet.ReadPacketizable<AiCommand>();
        }

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AiComponent)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Command = Command;
                copy._currentbehaviour = _currentbehaviour.DeepCopy(copy._currentbehaviour);
            }
            else
            {
                copy._currentbehaviour = _currentbehaviour.DeepCopy();
            }

            return copy;
        }

        #endregion
    }
}