using System;
using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Commands;

namespace Space.Model
{
    class GameState : AbstractState<PlayerInfo, PacketizerContext>, IReversibleSubstate<PlayerInfo, PacketizerContext>
    {
        private Game game;

        private ISession<PlayerInfo, PacketizerContext> session;

        public GameState(Game game, ISession<PlayerInfo, PacketizerContext> session)
            : base(((IPacketizer<PlayerInfo, PacketizerContext>)game.Services.GetService(typeof(IPacketizer<PlayerInfo, PacketizerContext>))).CopyFor(session))
        {
            this.game = game;
            this.session = session;
        }

        protected override void HandleCommand(ICommand<PlayerInfo, PacketizerContext> command)
        {
            switch ((GameCommandType)command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player input command, apply it.
                    {
                        Ship ship = (Ship)GetEntity(command.Player.Data.ShipUID);
                        if (ship != null)
                        {
                            // What did he do?
                            var inputCommand = (PlayerInputCommand)command;
                            switch (inputCommand.Input)
                            {
                                // Start accelerating in the given direction.
                                case PlayerInputCommand.PlayerInput.AccelerateUp:
                                    ship.Accelerate(Directions.North);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateRight:
                                    ship.Accelerate(Directions.East);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateDown:
                                    ship.Accelerate(Directions.South);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateLeft:
                                    ship.Accelerate(Directions.West);
                                    break;

                                // Stop accelerating in the given direction.
                                case PlayerInputCommand.PlayerInput.StopUp:
                                    ship.StopAccelerate(Directions.North);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopRight:
                                    ship.StopAccelerate(Directions.East);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopDown:
                                    ship.StopAccelerate(Directions.South);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopLeft:
                                    ship.StopAccelerate(Directions.West);
                                    break;

                                // Begin rotating.
                                case PlayerInputCommand.PlayerInput.Rotate:
                                    ship.RotateTo(inputCommand.TargetAngle);
                                    break;

                                // Begin/stop shooting.
                                case PlayerInputCommand.PlayerInput.Shoot:
                                    Console.WriteLine("start shooting");
                                    ship.Shoot();
                                    break;
                                case PlayerInputCommand.PlayerInput.CeaseFire:
                                    ship.CeaseFire();
                                    break;
                            }
                        }
                    }
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
            for (int i = commands.Count - 1; i >= 0; --i)
            {
                if (!commands[i].IsAuthoritative)
                {
                    hadTentative = true;
                    commands.RemoveAt(i);
                }
            }
            return hadTentative;
        }
    }
}
