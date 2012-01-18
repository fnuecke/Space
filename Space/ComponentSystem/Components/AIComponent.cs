using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components.AIBehaviour;

namespace Space.ComponentSystem.Components
{
    public  class AIComponent : AbstractComponent
    {
        public struct AICommand
        {
            public Vector2 target;
            public int maxDistance;
            public Order order;
            public AICommand(Vector2 target,int maxDistance,Order order)
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
            switch (command.order)
            {
                case (Order.Guard):
                    currentbehaviour = new PatrolBehaviour(this);
                    break;
                default:
                    currentbehaviour = new PatrolBehaviour(this);
                    break;
            }
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
            currentbehaviour.Update();
            //var input = Entity.GetComponent<ShipControl>();
            //counter++;
            //counter %= 400;
            //if(counter == 0)

            //{

            //    input.Accelerate(Directions.South);
            //}
            //else if (counter == 100)
            //{
            //    input.StopAccelerate(Directions.South);
            //    input.Accelerate(Directions.West);
            //}
            //else if (counter == 200)
            //{
            //    input.StopAccelerate(Directions.West);
            //    input.Accelerate(Directions.North);
            //}
            //else if (counter == 300)
            //{
            //    input.StopAccelerate(Directions.North);
            //    input.Accelerate(Directions.East);
            //}
        }
        private void CalculateBehaviour()
        {
            // Get local player's avatar.
            var info =Entity.GetComponent<ShipInfo>();
            var escapeDir = new Vector2(0, 0);
            // Can't do anything without an avatar.
            if (info == null)
            {
                return ;
            }

            var position = info.Position;
            var radarRange = info.RadarRange;
            var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
            if (index == null) return;
            foreach (var neighbor in index.
               GetNeighbors(ref position, radarRange, Detectable.IndexGroup))
            {
                var transform = neighbor.GetComponent<Transform>();
                if (transform == null) continue;

                var neighborGravitation = neighbor.GetComponent<Gravitation>();
                var neighborCollisionDamage = neighbor.GetComponent<CollisionDamage>();
                if (neighborCollisionDamage != null && neighborGravitation != null &&
                    (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
                {

                    var direction = position - transform.Translation;
                    if (direction.Length() < 2000)
                    {
                        direction.Normalize();
                        escapeDir += direction;
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
            return parameterizationType == typeof (DefaultLogicParameterization);
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