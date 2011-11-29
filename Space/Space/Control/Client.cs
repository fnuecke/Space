using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Space.Commands;
using Space.Model;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class Client : AbstractUdpClient<PlayerInfo, GameCommandType>
    {
        #region Fields

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject, GameCommandType, PlayerInfo> simulation;

        /// <summary>
        /// Last known player movement direction.
        /// </summary>
        private Direction lastDirection = Direction.Invalid;

        #endregion

        public Client(Game game)
            : base(game, 8443, "5p4c3")
        {
            simulation = new TSS<GameState, IGameObject, GameCommandType, PlayerInfo>(new[] { 50, 100 }, new GameState(game, Session));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Session.ConnectionState == ClientState.Connected && !simulation.WaitingForSynchronization)
            {
                Direction direction = Direction.Invalid;
                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                {
                    direction |= Direction.North;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    direction |= Direction.East;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Down))
                {
                    direction |= Direction.South;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                {
                    direction |= Direction.West;
                }
                switch (direction)
                {
                    case Direction.North:
                    case Direction.NorthAlt:
                    case Direction.East:
                    case Direction.EastAlt:
                    case Direction.South:
                    case Direction.SouthAlt:
                    case Direction.West:
                    case Direction.WestAlt:
                    case Direction.NorthEast:
                    case Direction.NorthWest:
                    case Direction.SouthEast:
                    case Direction.SouthWest:
                        break;
                    default:
                        direction = Direction.Invalid;
                        break;
                }
                if (direction != lastDirection)
                {
                    lastDirection = direction;
                    PlayerInputCommand command;
                    if (direction == Direction.Invalid)
                    {
                        command = new PlayerInputCommand(Session.LocalPlayer, simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.StopMovement, direction);
                    }
                    else
                    {
                        command = new PlayerInputCommand(Session.LocalPlayer, simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.Accelerate, direction);
                    }
                    simulation.PushCommand(command);
                    SendAll(command, 20);
                }

                // Drive game logic.
                simulation.Update();
            }
        }

        protected override void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;
            var info = args.Data.ReadString();
            console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;

            console.WriteLine(string.Format("CLT.NET: Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));

            if (args.WasSuccess)
            {
                simulation.Depacketize(args.Data);
            }
            else
            {
                // TODO
            }
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo>)e;
            console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo>)e;
            console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    {
                        var simulationCommand = (ISimulationCommand<GameCommandType, PlayerInfo>)command;
                        if (simulationCommand.Frame > simulation.TrailingFrame)
                        {
                            simulation.PushCommand(simulationCommand);
                        }
                        else
                        {
                            console.WriteLine("Got a command we couldn't use, " + simulationCommand.Frame + "<" + simulation.TrailingFrame);
                        }
                    }
                    break;
                case GameCommandType.AddPlayerShip:
                    {
                        var simulationCommand = (ISimulationCommand<GameCommandType, PlayerInfo>)command;
                        if (simulationCommand.Frame > simulation.TrailingFrame)
                        {
                            simulation.PushCommand(simulationCommand);
                        }
                        else
                        {
                            console.WriteLine("Got a command we couldn't use, " + simulationCommand.Frame + "<" + simulation.TrailingFrame);
                        }
                    }
                    break;
            }
        }

#region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            // Draw player 0 ship.
            {
                var player = Session.LocalPlayer;
                if (player != null)
                {
                    var ship = (Ship)simulation.Get(player.Data.ShipUID);
                    if (ship != null)
                    {
                        ship.Draw(null, new Vector2(100, 0), spriteBatch);
                    }
                }
            }

            spriteBatch.End();
        }

        internal long DEBUG_CurrentFrame { get { return simulation.CurrentFrame; } }

#endregion
    }
}
