using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.AIBehaviour;

namespace Space.ComponentSystem.Components
{
    public class AIComponent : AbstractComponent
    {
        public struct AICommand
        {
            public Vector2 target;
            public int maxDistance;
            public Order order;
            public AICommand(Vector2 target, int maxDistance, Order order)
            {
                this.target = target;
                this.maxDistance = maxDistance;
                this.order = order;
            }
        }
        public enum Order
        {
            Move,
            Guard,
            Attack
        }
        public float MaxHealth;
        public float MaxSpeed;
        public int counter;
        private Behaviour currentbehaviour;
        private bool starting;
        public AICommand AiCommand;
        public AIComponent(AICommand command)
        {
            MaxSpeed = -1;
            MaxHealth = -1;
            AiCommand = command;
            SwitchOrder();
        }

        public override void Update(object parameterization)
        {
            if (MaxSpeed == -1)
            {
                var shipinfo = Entity.GetComponent<ShipInfo>();
                if (shipinfo == null) return;
                MaxHealth = shipinfo.MaxHealth;
                MaxSpeed = shipinfo.MaxSpeed;
            }
            CalculateBehaviour();
            currentbehaviour.Update();
        }
        private void CalculateBehaviour()
        {
            // Get local player's avatar.
            var info = Entity.GetComponent<ShipInfo>();
            var position = info.Position;
            if (currentbehaviour is PatrolBehaviour)
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
                        currentbehaviour = new AttackBehaviour(this, neighbor.UID);
                        return;
                    }

                }
            }
            else if (currentbehaviour is AttackBehaviour)
            {
                var targetEntity = Entity.Manager.GetEntity(((AttackBehaviour)currentbehaviour).TargetEntity);
                
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
            switch (AiCommand.order)
            {
                case (Order.Guard):
                    currentbehaviour = new PatrolBehaviour(this);
                    break;
                default:
                    currentbehaviour = new PatrolBehaviour(this);
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

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                ;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
        }

        #endregion
    }
}