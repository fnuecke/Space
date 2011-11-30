using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Space.Commands;
using Space.Model;
using Space.View;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class Client : AbstractUdpClient<PlayerInfo, GameCommandType, PacketizerContext>
    {
        #region Fields

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext> simulation;

        /// <summary>
        /// Last known player movement direction.
        /// </summary>
        private Direction lastDirection = Direction.Invalid;

        /// <summary>
        /// Last time we sent a sync command to the server.
        /// </summary>
        private long lastSyncTime = 0;

        /// <summary>
        /// Averages frame deltas, used for clock sync.
        /// </summary>
        private Average frameDeltas = new Average(20);

        #endregion

        public Client(Game game)
            : base(game, 50101, "5p4c3!")
        {
            simulation = new TSS<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>(new uint[] { 50, 100 }, new GameState(game, Session));
            simulation.ThresholdExceeded += HandleThresholdExceeded;
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

            // Send sync command every now and then, to keep game clock synched.
            if (Session.ConnectionState == ClientState.Connected &&
                new TimeSpan(DateTime.Now.Ticks - lastSyncTime).TotalMilliseconds > 200)
            {
                lastSyncTime = DateTime.Now.Ticks;
                Send(new SynchronizeCommand(simulation.CurrentFrame), 0);
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
                simulation.Depacketize(args.Data, packetizer.Context);
            }
            else
            {
                // TODO
            }
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            if (Session.ConnectionState != ClientState.Connected)
            {
                throw new InvalidOperationException();
            }
            switch (command.Type)
            {
                case GameCommandType.Synchronize:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        SynchronizeCommand syncCommand = (SynchronizeCommand)command;
                        long latency = (simulation.CurrentFrame - syncCommand.ClientFrame) / 2;
                        long clientServerDelta = (syncCommand.ServerFrame - simulation.CurrentFrame);
                        long frameDelta = clientServerDelta + latency / 2;
                        if (frameDelta != 0)
                        {
                            Console.WriteLine("Correcting for " + frameDelta + " frames.");
                            simulation.RunToFrame(simulation.CurrentFrame + frameDelta);
                        }
                    }
                    break;
                case GameCommandType.GameStateResponse:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        simulation.Depacketize(((GameStateResponseCommand)command).GameState, packetizer.Context);
                    }
                    break;
                case GameCommandType.PlayerDataChanged:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        if (command.Player == null)
                        {
                            throw new ArgumentException("command.Player");
                        }
                        Console.WriteLine("CLT: player data");
                        var changeCommand = (PlayerDataChangedCommand)command;
                        switch (changeCommand.Field)
                        {
                            case PlayerInfoField.ShipId:
                                changeCommand.Player.Data.ShipUID = changeCommand.Value.ReadInt64();
                                break;
                            case PlayerInfoField.ShipType:
                                changeCommand.Player.Data.ShipType = changeCommand.Value.ReadString();
                                break;
                        }
                    }
                    break;
                case GameCommandType.AddGameObject:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        Console.WriteLine("CLT: add object");
                        var addCommand = (AddGameObjectCommand)command;
                        addCommand.GameObject.Rewind();
                        IGameObject obj = packetizer.Depacketize<IGameObject>(addCommand.GameObject);
                        simulation.RunToFrame(addCommand.Frame);
                        simulation.Add(obj, true);
                    }
                    break;
                case GameCommandType.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        Console.WriteLine("CLT: remove object");
                        var removeCommand = (RemoveGameObjectCommand)command;
                        simulation.RunToFrame(removeCommand.Frame);
                        simulation.Remove(removeCommand.GameObjectUID);
                    }
                    break;
                case GameCommandType.PlayerInput:
                    {
                        var simulationCommand = (ISimulationCommand<GameCommandType, PlayerInfo, PacketizerContext>)command;
                        simulation.PushCommand(simulationCommand);
                    }
                    break;
                default:
                    throw new ArgumentException("command");
            }
        }

        private void HandleThresholdExceeded(object sender, EventArgs e)
        {
            Send(new GameStateRequestCommand(), 200);
        }

#region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // Draw world elements.
            spriteBatch.Begin();
            foreach (var child in simulation.Children)
            {
                child.Draw(null, Vector2.Zero, spriteBatch);
            }
            spriteBatch.End();

            // Draw debug stuff.
            SpriteFont font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");

            var ngOffset = new Vector2(Game.GraphicsDevice.Viewport.Width - 200, Game.GraphicsDevice.Viewport.Height - 100);
            var sessionOffset = new Vector2(Game.GraphicsDevice.Viewport.Width - 340, Game.GraphicsDevice.Viewport.Height - 100);

            SessionInfo.Draw("Client", Session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);
        }

        internal long DEBUG_CurrentFrame { get { return simulation.CurrentFrame; } }

#endregion
    }
}
