using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Components.Behaviours;

namespace Space.ComponentSystem.Systems
{
    public sealed class AISystem : AbstractComponentSystem<AiComponent>
    {
        protected override void UpdateComponent(Microsoft.Xna.Framework.GameTime gameTime, long frame, AiComponent component)
        {
            if (_counter % 10 == 0)
            {
                CalculateBehaviour();
                _currentBehaviour.Update();
            }
        }

        #region Logic

        /// <summary>
        /// Calculates which behaviour to use
        /// </summary>
        private void CalculateBehaviour(int entity)
        {
            // Get local player's avatar. and position
            var info = Manager.GetComponent<ShipInfo>(entity);
            var position = info.Position;

            //check if there are enemys in the erea
            if (_currentBehaviour is PatrolBehaviour)
            {
                CheckNeighbours(ref position, ref position);
            }
            else if (_currentBehaviour is AttackBehaviour)
            {
                var attack = (AttackBehaviour)_currentBehaviour;
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

                if ((position - ((AttackBehaviour)_currentBehaviour).StartPosition).Length() > Command.MaxDistance)
                {
                    var move = (MoveBehaviour)_behaviours[Behaviour.Behaviours.Move];
                    move.Target = 0;
                    move.TargetPosition = ((AttackBehaviour)_currentBehaviour).StartPosition;
                    _currentBehaviour = move;
                    _returning = true;
                    return;
                }
            }
            else if (_currentBehaviour is MoveBehaviour)
            {
                if (_returning)
                {
                    var target = ((MoveBehaviour)_currentBehaviour).TargetPosition;
                    if ((target - position).Length() < 200)
                    {
                        SwitchOrder();
                        if (!(_currentBehaviour is MoveBehaviour))
                            _returning = false;
                    }

                    return;
                }

                CheckNeighbours(ref position, ref position);
            }
        }

        private void CheckAndSwitchToMoveBehaviour(ref Vector2 position)
        {
            var startposition = ((AttackBehaviour)_currentBehaviour).StartPosition;
            if (CheckNeighbours(ref position, ref startposition)) return;

            var move = (MoveBehaviour)_behaviours[Behaviour.Behaviours.Move];
            move.TargetPosition = startposition;
            move.Target = 0;
            _currentBehaviour = move;
        }

        private bool CheckNeighbours(ref Vector2 position, ref Vector2 startPosition)
        {
            //TODO only check every second or so
            if ((_counter %= 60) == 0)
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
                        var attack = (AttackBehaviour)_behaviours[Behaviour.Behaviours.Attack];
                        attack.TargetEntity = neighbor.UID;
                        attack.TargetDead = false;
                        _currentBehaviour = attack;
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
            switch (Command.Order)
            {
                case (AiComponent.Order.Guard):
                    _currentBehaviour = (PatrolBehaviour)_behaviours[Behaviour.Behaviours.Patrol];
                    break;
                case (AiComponent.Order.Move):
                    var behaviour = (MoveBehaviour)_behaviours[Behaviour.Behaviours.Move];
                    if (Command.TargetEntity == 0)
                    {
                        behaviour.TargetPosition = Command.Target;
                    }
                    else
                    {
                        behaviour.Target = Command.TargetEntity;
                    }
                    _currentBehaviour = behaviour;
                    break;

                default:
                    _currentBehaviour = (PatrolBehaviour)_behaviours[Behaviour.Behaviours.Patrol];
                    break;
            }
        }

        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityRemoved)
            {
                if (_currentBehaviour is AttackBehaviour)
                {
                    var beh = (AttackBehaviour)_currentBehaviour;
                    if (((EntityRemoved)((ValueType)message)).Entity.UID == beh.TargetEntity)
                    {
                        beh.TargetDead = true;
                    }
                }
                else if (_currentBehaviour is MoveBehaviour)
                {
                    var beh = (MoveBehaviour)_currentBehaviour;
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

        #endregion

    }
}
