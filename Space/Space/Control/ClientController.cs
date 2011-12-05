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
    class ClientController : AbstractTssClient<GameState, IGameObject, GameCommand, GameCommandType, PlayerInfo, PacketizerContext>
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
        public ClientController(Game game, IClientSession<PlayerInfo, PacketizerContext> session)
            : base(game, session)
        {
            Simulation.Initialize(new GameState(game, Session));
        }

        protected override void LoadContent()
        {
            background = Game.Content.Load<Texture2D>("Textures/stars");

            base.LoadContent();
        }

        #endregion

        #region Logic

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

        #endregion

        #region Events

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
        /// Got a locally generated command, apply it.
        /// </summary>
        protected override void HandleLocalCommand(GameCommand command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player input command, high send priority.
                    Apply(command, PacketPriority.High);
                    break;
            }
        }

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override bool HandleRemoteCommand(IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            // Check what we have.
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    Apply(command, PacketPriority.None);
                    return true;

                default:
#if DEBUG
                    Console.WriteLine("Client: got unknown command type: " + command.Type);
#endif
                    break;
            }

            // Got here -> unhandled.
            return false;
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
