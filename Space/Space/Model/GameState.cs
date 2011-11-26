using System.Collections.Generic;
using Engine.Commands;
using Engine.Simulation;
using Space.Simulation.Commands;

namespace Space.Model
{
    class GameState : PhysicsEnabledState<IGameObject>
    {

        private Dictionary<int, Ship> players = new Dictionary<int, Ship>();

        public Ship GetPlayerShip(int player)
        {
            if (!players.ContainsKey(player))
            {
                foreach (var child in steppables)
                {
                    if (child is Ship)
                    {
                        var ship = (Ship)child;
                        if (ship.Player == player)
                        {
                            players.Add(player, ship);
                            return ship;
                        }
                    }
                }
                return null;
            }
            else
            {
                return players[player];
            }
        }

        protected override void HandleCommand(ISimulationCommand command)
        {
            switch (command.Type)
            {
                case 50:
                    {
                        var inputCommand = (PlayerInputCommand)command;
                        Ship ship = GetPlayerShip(command.Player);
                        if (ship == null)
                        {
                            // No ship for this player.
                            return;
                        }
                        switch (inputCommand.Input)
                        {
                            case PlayerInputCommand.PlayerInput.Accelerate:
                                ship.Accelerate();
                                break;
                            case PlayerInputCommand.PlayerInput.Decelerate:
                                ship.Decelerate();
                                break;
                            case PlayerInputCommand.PlayerInput.StopMovement:
                                ship.StopMovement();
                                break;
                            case PlayerInputCommand.PlayerInput.TurnLeft:
                                ship.TurnLeft();
                                break;
                            case PlayerInputCommand.PlayerInput.TurnRight:
                                ship.TurnRight();
                                break;
                            case PlayerInputCommand.PlayerInput.StopRotation:
                                ship.StopRotating();
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public override object Clone()
        {
            var clone = new GameState();

            return CloneTo(clone);
        }

    }
}
