using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Input;
using Engine.Session;
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
    class Client : AbstractTssUdpClient<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>
    {
        #region Fields

        /// <summary>
        /// Overall background used (spaaace :P).
        /// </summary>
        private Texture2D background;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new game client, ready to connect to an open game.
        /// </summary>
        /// <param name="game"></param>
        public Client(Game game)
            : base(game, 50101, "5p4c3!")
        {
            simulation.Initialize(new GameState(game, Session));
        }

        #endregion

        #region Logic

        protected override void LoadContent()
        {
            background = Game.Content.Load<Texture2D>("Textures/stars");

            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                var spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));

                // Get player's ship's position.
                var translation = Vector2.Zero;
                Ship ship = (Ship)simulation.Get(Session.LocalPlayer.Data.ShipUID);
                if (ship != null)
                {
                    translation.X = -ship.Position.X.IntValue + GraphicsDevice.Viewport.Width / 2;
                    translation.Y = -ship.Position.Y.IntValue + GraphicsDevice.Viewport.Height / 2;
                }

                spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
                spriteBatch.Draw(background, Vector2.Zero, new Rectangle(-(int)translation.X, -(int)translation.Y, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                spriteBatch.End();

                // Draw world elements.
                spriteBatch.Begin();
                foreach (var child in simulation.Children)
                {
                    child.Draw(null, translation, spriteBatch);
                }
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override bool HandleCommand(ICommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            // Only handle stuff while we're connected.
            if (Session.ConnectionState != ClientState.Connected)
            {
                return false;
            }

            // Check what we have.
            switch (command.Type)
            {
                case GameCommandType.PlayerDataChanged:
                    // Player information has somehow changed.
                    // Only accept these when they come from the server.
                    if (command.IsAuthoritative)
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
                        return true;
                    }
                    break;
                case GameCommandType.PlayerInput:
                    {
                        var inputCommand = (PlayerInputCommand)command;
                        simulation.PushCommand(inputCommand, inputCommand.Frame);
                    }
                    return true;
                default:
#if DEBUG
                    Console.WriteLine("Client: got a command we couldn't handle: " + command.Type);
#endif
                    break;
            }

            // Got here -> unhandled.
            return false;
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
        /// Player pressed a key.
        /// </summary>
        protected override void HandleKeyPressed(object sender, EventArgs e)
        {
            if (Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (KeyboardInputEventArgs)e;

            PlayerInputCommand command = null;
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.S:
                    // Accelerate downwards.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.AccelerateDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    // Accelerate left.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.AccelerateLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    // Accelerate right.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.AccelerateRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    // Accelerate upwards.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.AccelerateUp);
                    break;

                case Keys.Q:
                    // Rotate to the left.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.TurnLeft);
                    break;
                case Keys.E:
                    // Rotate to the right.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.TurnRight);
                    break;

                default:
                    break;
            }

            if (command != null)
            {
                simulation.PushCommand(command);
                SendAll(command, 10);
            }
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        protected override void HandleKeyReleased(object sender, EventArgs e)
        {
            if (Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (KeyboardInputEventArgs)e;

            PlayerInputCommand command = null;
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.S:
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopUp);
                    break;

                case Keys.Q:
                    // Stop rotating left.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopTurnLeft);
                    break;
                case Keys.E:
                    // Stop rotating right.
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopTurnRight);
                    break;

                default:
                    break;
            }

            if (command != null)
            {
                simulation.PushCommand(command);
                SendAll(command, 10);
            }
        }

        protected override void HandleMouseMoved(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

        }

        #endregion

        #region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
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
