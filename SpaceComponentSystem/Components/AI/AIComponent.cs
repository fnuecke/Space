using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.Behaviours;

namespace Space.ComponentSystem.Components
{
    public sealed class AiComponent : AbstractComponent
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

            public int OriginEntity;

            public int TargetEntity;

            public AiCommand(Vector2 target, int maxDistance, Order order)
            {
                Target = target;
                MaxDistance = maxDistance;
                this.order = order;
            }

            public AiCommand(int target, int maxDistance, Order order, int origin = 0)
            {
                TargetEntity = target;
                MaxDistance = maxDistance;
                this.order = order;
                OriginEntity = origin;
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

        /// <summary>
        /// The command this entity has
        /// </summary>
        public AiCommand Command;

        /// <summary>
        /// The current behaviour
        /// </summary>
        private Behaviour _currentbehaviour;

        private Dictionary<Behaviour.Behaviours, Behaviour> behaviours = new Dictionary<Behaviour.Behaviours, Behaviour>();

        /// <summary>
        /// A counter used to only update every few milliseconds
        /// </summary>
        private int counter;

        private bool returning;

        #endregion

        #region Constructor

        public AiComponent(AiCommand command)
        {
            behaviours.Add(Behaviour.Behaviours.Patrol, new PatrolBehaviour(this));
            behaviours.Add(Behaviour.Behaviours.Move, new MoveBehaviour(this));
            behaviours.Add(Behaviour.Behaviours.Attack, new AttackBehaviour(this));
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
            if (counter % 10 == 0)
            {
                CalculateBehaviour();
                _currentbehaviour.Update();
            }
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
                    var move = (MoveBehaviour)behaviours[Behaviour.Behaviours.Move];
                    move.Target = 0;
                    move.TargetPosition = ((AttackBehaviour)_currentbehaviour).StartPosition;
                    _currentbehaviour = move;
                    returning = true;
                    return;
                }
            }
            else if (_currentbehaviour is MoveBehaviour)
            {
                if (returning)
                {
                    var target = ((MoveBehaviour)_currentbehaviour).TargetPosition;
                    if ((target - position).Length() < 200)
                    {
                        SwitchOrder();
                        if (!(_currentbehaviour is MoveBehaviour))
                            returning = false;
                    }

                    return;
                }

                CheckNeighbours(ref position, ref position);
            }
        }

        private void CheckAndSwitchToMoveBehaviour(ref Vector2 position)
        {
            var startposition = ((AttackBehaviour)_currentbehaviour).StartPosition;
            if (CheckNeighbours(ref position, ref startposition)) return;

            var move = (MoveBehaviour)behaviours[Behaviour.Behaviours.Move];
            move.TargetPosition = startposition;
            move.Target = 0;
            _currentbehaviour = move;
        }

        private bool CheckNeighbours(ref Vector2 position, ref Vector2 startPosition)
        {
            //TODO only check every second or so
            if ((counter %= 60) == 0)
            {
                var currentFaction = Entity.GetComponent<Faction>().Value;
                var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
                if (index == null) return false;
                foreach (var neighbor in index.
                    RangeQuery(ref position, 3000, Detectable.IndexGroup))
                {
                    var transform = neighbor.GetComponent<Transform>();
                    if (transform == null) continue;

                    var health = neighbor.GetComponent<Health>();
                    if (health == null || health.Value == 0) continue;

                    var faction = neighbor.GetComponent<Faction>();
                    if (faction == null) continue;

                    if ((faction.Value & currentFaction) == 0)
                    {
                        var attack = (AttackBehaviour)behaviours[Behaviour.Behaviours.Attack];
                        attack.TargetEntity = neighbor.UID;
                        attack.TargetDead = false;
                        _currentbehaviour = attack;
                        attack.StartPosition = startPosition;
                        return true;
                    }

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
                    _currentbehaviour = (PatrolBehaviour)behaviours[Behaviour.Behaviours.Patrol];
                    break;
                case (Order.Move):
                    var behaviour = (MoveBehaviour)behaviours[Behaviour.Behaviours.Move];
                    if (Command.TargetEntity == 0)
                    {
                        behaviour.TargetPosition = Command.Target;
                    }
                    else
                    {
                        behaviour.Target = Command.TargetEntity;
                    }
                    _currentbehaviour = behaviour;
                    break;

                default:
                    _currentbehaviour = (PatrolBehaviour)behaviours[Behaviour.Behaviours.Patrol];
                    break;
            }
        }

        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityRemoved)
            {
                if (_currentbehaviour is AttackBehaviour)
                {
                    var beh = (AttackBehaviour)_currentbehaviour;
                    if (((EntityRemoved)((ValueType)message)).Entity.UID == beh.TargetEntity)
                    {
                        beh.TargetDead = true;
                    }
                }
                else if (_currentbehaviour is MoveBehaviour)
                {
                    var beh = (MoveBehaviour)_currentbehaviour;
                    if (((EntityRemoved)((ValueType)message)).Entity.UID == beh.Target)
                    {
                        beh.Target = 0;
                    }
                }
                if (Command.OriginEntity == ((EntityRemoved)((ValueType)message)).Entity.UID)
                    Command.OriginEntity = 0;
                if (Command.TargetEntity == ((EntityRemoved)((ValueType)message)).Entity.UID)
                {
                    if (Command.OriginEntity == 0)
                    {
                        Entity.Manager.RemoveEntity(Entity);
                    }
                    else
                    {
                        Command.TargetEntity = Command.OriginEntity;
                    }
                }
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
                .Write(Command)
                .Write(counter);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _currentbehaviour = packet.ReadPacketizableWithTypeInfo<Behaviour>();
            _currentbehaviour.AiComponent = this;
            Command = packet.ReadPacketizable<AiCommand>();
            counter = packet.ReadInt32();
        }

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AiComponent)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Command = Command;
                copy._currentbehaviour = _currentbehaviour.DeepCopy(copy._currentbehaviour);
                copy.counter = counter;
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