using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Commands;

namespace Space.Model
{
    class GameState : PhysicsEnabledState<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>, IReversibleSubstate<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>
    {
        protected override GameState ThisState { get { return this; } }

        private Game game;

        private ISession<PlayerInfo, PacketizerContext> session;

        public GameState(Game game, ISession<PlayerInfo, PacketizerContext> session)
            : base((IPacketizer<PacketizerContext>)game.Services.GetService(typeof(IPacketizer<PacketizerContext>)))
        {
            this.game = game;
            this.session = session;
        }

        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player input command, apply it.
                    {
                        Ship ship = (Ship)Get(command.Player.Data.ShipUID);
                        if (ship != null)
                        {
                            // What did he do?
                            var inputCommand = (PlayerInputCommand)command;
                            switch (inputCommand.Input)
                            {
                                // Start accelerating in the given direction.
                                case PlayerInputCommand.PlayerInput.AccelerateUp:
                                    ship.Accelerate(Direction.North);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateRight:
                                    ship.Accelerate(Direction.East);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateDown:
                                    ship.Accelerate(Direction.South);
                                    break;
                                case PlayerInputCommand.PlayerInput.AccelerateLeft:
                                    ship.Accelerate(Direction.West);
                                    break;

                                // Stop accelerating in the given direction.
                                case PlayerInputCommand.PlayerInput.StopUp:
                                    ship.StopAccelerate(Direction.North);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopRight:
                                    ship.StopAccelerate(Direction.East);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopDown:
                                    ship.StopAccelerate(Direction.South);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopLeft:
                                    ship.StopAccelerate(Direction.West);
                                    break;

                                // Begin turning to the left.
                                case PlayerInputCommand.PlayerInput.TurnLeft:
                                    ship.Rotate(Direction.Left);
                                    break;
                                // Begin turning to the right.
                                case PlayerInputCommand.PlayerInput.TurnRight:
                                    ship.Rotate(Direction.Right);
                                    break;
                                // Stop turning left.
                                case PlayerInputCommand.PlayerInput.StopTurnLeft:
                                    ship.StopRotate(Direction.Left);
                                    break;
                                // Stop turning left.
                                case PlayerInputCommand.PlayerInput.StopTurnRight:
                                    ship.StopRotate(Direction.Right);
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
