using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.Commands;

namespace Space.Model
{
    class GameState : PhysicsEnabledState<GameState, IGameObject, GameCommandType, PlayerInfo>, IReversibleSubstate<GameState, IGameObject, GameCommandType, PlayerInfo>
    {
        protected override GameState ThisState { get { return this; } }

        private Game game;

        private ISession<PlayerInfo> session;

        public GameState()
        {
        }

        public GameState(Game game, ISession<PlayerInfo> session)
        {
            this.game = game;
            this.session = session;
        }

        protected override void HandleCommand(ISimulationCommand<GameCommandType, PlayerInfo> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    {
                        var inputCommand = (PlayerInputCommand)command;
                        Ship ship = (Ship)Get(command.Player.Data.ShipUID);
                        if (ship == null)
                        {
                            // No ship for this player.
                            return;
                        }
                        switch (inputCommand.Input)
                        {
                            case PlayerInputCommand.PlayerInput.Accelerate:
                                ship.Accelerate(inputCommand.Direction);
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
                case GameCommandType.AddPlayerShip:
                    Console.WriteLine("add ship");
                    {
                        var addCommand = (AddPlayerCommand)command;
                        var ship = ((GameObjectFactory)game.Services.GetService(typeof(IGameObjectFactory))).CreateShip(addCommand.Player.Data.ShipName, addCommand.Player.Number, this);
                        if (addCommand.Player.Data.ShipUID > 0)
                        {
                            ship.UID = addCommand.Player.Data.ShipUID;
                        }
                        else
                        {
                            addCommand.Player.Data.ShipUID = ship.UID;
                        }
                        Add(ship);
                    }
                    break;
                case GameCommandType.RemovePlayerShip:
                    break;
                default:
                    break;
            }
        }

        public override object Clone()
        {
            var clone = new GameState(game, session);

            return CloneTo(clone);
        }

        public bool SkipTentativeCommands()
        {
            bool hadTentative = false;
            if (commands.ContainsKey(CurrentFrame + 1))
            {
                List<ISimulationCommand<GameCommandType, PlayerInfo>> list = commands[CurrentFrame + 1];
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (list[i].IsTentative)
                    {
                        hadTentative = true;
                        list.RemoveAt(i);
                    }
                }
            }
            return hadTentative;
        }
    }
}
