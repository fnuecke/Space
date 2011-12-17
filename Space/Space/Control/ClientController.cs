using System;
using Engine.Commands;
using Engine.Controller;
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
    class ClientController : AbstractTssClient<GameCommand>
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

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
        public ClientController(Game game, IClientSession session)
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
                Ship ship = (Ship)Simulation.GetEntity(Session.LocalPlayer.Data.ShipUID);
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
                    //child.Draw(null, translation, spriteBatch);
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
            // OK, we were allowed to join, invalidate our simulation to request the current state.
            Simulation.Invalidate();
        }

        /// <summary>
        /// Got a locally generated command, apply it.
        /// </summary>
        protected override void HandleLocalCommand(GameCommand command)
        {
            switch ((GameCommandType)command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player input command, high send priority.
                    Apply(command);
                    break;
            }
        }

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override bool HandleRemoteCommand(IFrameCommand command)
        {
            // Check what we have.
            switch ((GameCommandType)command.Type)
            {
                case GameCommandType.PlayerInput:
                    Apply(command);
                    return true;

                default:
                    logger.Debug("Got unknown command type: {0}", command.Type);
                    break;
            }

            // Got here -> unhandled.
            return false;
        }

        #endregion

        #region Debugging stuff

        internal void DEBUG_InvalidateSimulation()
        {
            Simulation.Invalidate();
        }

        #endregion
    }
}
