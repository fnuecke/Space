using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Network;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.Model;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class Client : AbstractTssClient<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>
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
        public Client(Game game, IClientSession<PlayerInfo, PacketizerContext> session)
            : base(game, session)
        {
            Simulation.Initialize(new GameState(game, Session));
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
                Ship ship = (Ship)Simulation.GetSteppable(Session.LocalPlayer.Data.ShipUID);
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
                foreach (var child in Simulation.Children)
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
        protected override bool HandleRemoteCommand(IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            // Only handle stuff while we're connected.
            if (Session.ConnectionState != ClientState.Connected)
            {
                return false;
            }

            // Check what we have.
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    {
                        // The player has to be in the game for this to work... this can
                        // fail if the message from the server that a client joined reached
                        // us before the join message.
                        if (command.Player == null)
                        {
                            return false;
                        }
                        Simulation.PushCommand(command, command.Frame);
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
            Console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        /// <summary>
        /// Got a server response to our request to join it.
        /// </summary>
        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;

            Console.WriteLine(string.Format("CLT.NET: Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));

            // Were we allowed to join?
            if (args.WasSuccess)
            {
                // Yes! Use the received simulation information.
                Simulation.Depacketize(args.Data, Packetizer.Context);
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

            Console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;

            Console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

        /// <summary>
        /// Got a locally generated command, apply it.
        /// </summary>
        protected override void HandleLocalCommand(IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player input command, high send priority.
                    Apply(command, PacketPriority.High);
                    break;
            }
        }

        #endregion

        #region Debugging stuff

        internal long DEBUG_CurrentFrame { get { return Simulation.CurrentFrame; } }

        internal void DEBUG_InvalidateSimulation()
        {
            Simulation.Invalidate();
        }

        #endregion
    }
}
