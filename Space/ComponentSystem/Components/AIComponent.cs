using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.AIBehaviour;

namespace Space.ComponentSystem.Components
{
    public class AiComponent : AbstractComponent
    {
        #region Structs/Enums
        public class AiCommand : IPacketizable
        {
            public Vector2 Target;
            public int MaxDistance;
            public Order order;
            public AiCommand(){}
            public AiCommand(Vector2 target, int maxDistance, Order order)
            {
                Target = target;
                MaxDistance = maxDistance;
                this.order = order;
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
                order = (Order) packet.ReadByte();
            }
        }
        public enum Order
        {
            Move,
            Guard,
            Attack
        }
        #endregion

        #region Fields
        private Behaviour _currentbehaviour;
        public AiCommand Command;
        #endregion

        #region Constructor

        public AiComponent(AiCommand command)
        {
           
            Command = command;
            SwitchOrder();
        }
        #endregion
        #region Logic

        public override void Update(object parameterization)
        {
            
            CalculateBehaviour();
            _currentbehaviour.Update();
        }
        private void CalculateBehaviour()
        {
            // Get local player's avatar.
            var info = Entity.GetComponent<ShipInfo>();
            var position = info.Position;
            if (_currentbehaviour is PatrolBehaviour)
            {
                var currentFaction = Entity.GetComponent<Faction>().Value;
                var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
                if (index == null) return;
                foreach (var neighbor in index.
               GetNeighbors(ref position, 3000, Detectable.IndexGroup))
                {
                    var transform = neighbor.GetComponent<Transform>();
                    if (transform == null) continue;
                    var health = neighbor.GetComponent<Health>();
                    if (health == null||health.Value == 0) continue;
                    var faction = neighbor.GetComponent<Faction>();
                    if(faction == null) continue;
                    if ((faction.Value & currentFaction) == 0)
                    {
                        _currentbehaviour = new AttackBehaviour(this, neighbor.UID);
                        return;
                    }

                }
            }
            else if (_currentbehaviour is AttackBehaviour)
            {
                var targetEntity = Entity.Manager.GetEntity(((AttackBehaviour)_currentbehaviour).TargetEntity);
                
                if (targetEntity == null)
                {
                    SwitchOrder();
                    return;

                }
                var health = targetEntity.GetComponent<Health>();
                var transform = targetEntity.GetComponent<Transform>();
                if (health == null ||health.Value == 0|| transform == null)
                {
                    SwitchOrder();
                    return;
                }

                var direction = position - transform.Translation;
                if(direction.Length()>3000)
                    SwitchOrder();
                else
                {
                    return;
                }

            }




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
            base.Packetize(packet);
            packet.WriteWithTypeInfo(_currentbehaviour)
                .Write(Command);
            
            return packet;
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
                copy._currentbehaviour = _currentbehaviour;
                copy.Command = Command;
            }

            return copy;
        }
        #endregion
    }
}