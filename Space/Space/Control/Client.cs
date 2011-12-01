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
        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a synchronization request to the
        /// server. The lower the value the better the synchronization, but, obviously, also
        /// more network traffic.
        /// </summary>
        private const int SyncInterval = 1000;

        #endregion

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

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new game client, ready to connect to an open game.
        /// </summary>
        /// <param name="game"></param>
        public Client(Game game)
            : base(game, 50101, "5p4c3!")
        {
            simulation = new TSS<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>(new uint[] { 50, 100 }, new GameState(game, Session));
            simulation.ThresholdExceeded += HandleThresholdExceeded;
        }

        #endregion

        #region Logic

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
                    SendAll(command, 10);
                }

                // Drive game logic.
                //simulation.Update();
                // Compensate for dynamic timestep.
                simulation.RunToFrame(simulation.CurrentFrame + (int)Math.Round(gameTime.ElapsedGameTime.TotalMilliseconds / Game.TargetElapsedTime.TotalMilliseconds));

                // Send sync command every now and then, to keep game clock synched.
                if (new TimeSpan(DateTime.Now.Ticks - lastSyncTime).TotalMilliseconds > SyncInterval)
                {
                    lastSyncTime = DateTime.Now.Ticks;
                    Send(new SynchronizeCommand(simulation.CurrentFrame), 0);
                }
            }
        }

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            // This should only happen while we're connected.
            if (Session.ConnectionState != ClientState.Connected)
            {
                throw new InvalidOperationException();
            }

            // Check what we have.
            switch (command.Type)
            {
                case GameCommandType.Synchronize:
                    // Answer to a synchronization request.
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        SynchronizeCommand syncCommand = (SynchronizeCommand)command;
                        long latency = (simulation.CurrentFrame - syncCommand.ClientFrame) / 2;
                        long clientServerDelta = (syncCommand.ServerFrame - simulation.CurrentFrame);
                        long frameDelta = clientServerDelta + latency / 2;
                        if (System.Math.Abs(frameDelta) > 2)
                        {
                            console.WriteLine("Correcting for " + frameDelta + " frames.");
                            simulation.RunToFrame(simulation.CurrentFrame + frameDelta);
                        }
                    }
                    break;
                case GameCommandType.GameStateResponse:
                    // Got a simulation snap shot (normally after requesting it due to
                    // our simulation going out of scope for an older event).
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        simulation.Depacketize(((GameStateResponseCommand)command).GameState, packetizer.Context);
                    }
                    break;
                case GameCommandType.PlayerDataChanged:
                    // Player information has somehow changed.
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        // The player has to be in the game for this to work... this can
                        // fail if the message from the server that a client joined reached
                        // us before the join message.
                        if (command.Player == null)
                        {
                            throw new ArgumentException("command.Player");
                        }
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
                        // Make sure we keep the ID as defined by the server.
                        var id = obj.UID;
                        simulation.Add(obj, addCommand.Frame);
                        obj.UID = id;
                    }
                    break;
                case GameCommandType.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (!command.IsTentative)
                    {
                        Console.WriteLine("CLT: remove object");
                        var removeCommand = (RemoveGameObjectCommand)command;
                        simulation.Remove(removeCommand.GameObjectUID, removeCommand.Frame);
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

        #endregion

        #region Events
        
        /// <summary>
        /// Got info about an open game.
        /// </summary>
        protected override void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;

            var info = args.Data.ReadString();
            console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        /// <summary>
        /// Got a server response to our request to join it.
        /// </summary>
        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;

            console.WriteLine(string.Format("CLT.NET: Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));

            // Were we allowed to join?
            if (args.WasSuccess)
            {
                // Yes! Use the received simulation information.
                simulation.Depacketize(args.Data, packetizer.Context);
            }
            else
            {
                // No :( See if we know why and notify the user.
                // TODO
            }
        }

        /// <summary>
        /// Got info that a new player joined the game.
        /// </summary>
        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;

            console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;

            console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

        /// <summary>
        /// Called when our simulation cannot accomodate an update or rollback,
        /// meaning we have to get a server snapshot.
        /// </summary>
        private void HandleThresholdExceeded(object sender, EventArgs e)
        {
            // So we request it.
            Send(new GameStateRequestCommand(), 100);
        }

        #endregion

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
