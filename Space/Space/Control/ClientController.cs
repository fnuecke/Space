using System;
using Engine.Commands;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Simulation;

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
            tss.Initialize(new GameState());

            var renderer = new PlayerCenteredRenderSystem((SpriteBatch)game.Services.GetService(typeof(SpriteBatch)), game.Content, Session);
            renderer.AddComponent(new Background("Textures/stars"));

            tss.SystemManager.AddSystem(new PhysicsSystem());
            tss.SystemManager.AddSystem(new ShipControlSystem());
            tss.SystemManager.AddSystem(new AvatarSystem());
            tss.SystemManager.AddSystem(renderer);
        }

        protected override void LoadContent()
        {
            background = Game.Content.Load<Texture2D>("Textures/stars");

            base.LoadContent();
        }

        #endregion

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (Session.ConnectionState == ClientState.Connected)
            {
                tss.SystemManager.Update(ComponentSystemUpdateType.Display);
            }
        }

        #region Events

        /// <summary>
        /// Got a server response to our request to join it.
        /// </summary>
        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            // OK, we were allowed to join, invalidate our simulation to request the current state.
            tss.Invalidate();
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
            tss.Invalidate();
        }

        #endregion
    }
}
