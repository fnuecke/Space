using System.Collections.Generic;
using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
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

        protected override void HandleCommand(ISimulationCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
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
                                case PlayerInputCommand.PlayerInput.Accelerate:
                                    // Start accelerating in the given direction.
                                    ship.Accelerate(inputCommand.Direction);
                                    break;
                                case PlayerInputCommand.PlayerInput.StopMovement:
                                    // Stop accelerating.
                                    ship.StopMovement();
                                    break;
                                case PlayerInputCommand.PlayerInput.TurnLeft:
                                    // Begin turning to the left.
                                    ship.TurnLeft();
                                    break;
                                case PlayerInputCommand.PlayerInput.TurnRight:
                                    // Begin turning to the right.
                                    ship.TurnRight();
                                    break;
                                case PlayerInputCommand.PlayerInput.StopRotation:
                                    // Stop turning.
                                    ship.StopRotating();
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
            if (commands.ContainsKey(CurrentFrame + 1))
            {
                List<ISimulationCommand<GameCommandType, PlayerInfo, PacketizerContext>> list = commands[CurrentFrame + 1];
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
