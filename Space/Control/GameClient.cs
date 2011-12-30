using System;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Space.Control
{
    public class GameClient : DrawableGameComponent
    {
        #region Logger
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties
        
        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public IClientController<IFrameCommand> Controller { get; set; }

        #endregion

        #region Fields

        private InputCommandEmitter _emitter;

        #endregion

        public GameClient(Game game, GameServer server)
            : base(game)
        {
            Controller = ControllerFactory.CreateLocalClient(Game, server.Controller);

        }

        public GameClient(Game game)
            : base(game)
        {
            Controller = ControllerFactory.CreateRemoteClient(Game);
        }

        public override void Initialize()
        {
            // Draw underneath menus etc.
            DrawOrder = -50;

            // Register for events.
            Controller.Session.GameInfoReceived += HandleGameInfoReceived;
            Controller.Session.PlayerJoined += HandlePlayerJoined;
            Controller.Session.PlayerLeft += HandlePlayerLeft;

            // Create our input command emitter, which is used to grab user
            // input and convert it into commands that can be injected into our
            // simulation.
            _emitter = new InputCommandEmitter(Game, Controller);
            Controller.AddEmitter(_emitter);
            Game.Components.Remove(_emitter);
        }

        protected override void Dispose(bool disposing)
        {
            Controller.Session.GameInfoReceived -= HandleGameInfoReceived;
            Controller.Session.PlayerJoined -= HandlePlayerJoined;
            Controller.Session.PlayerLeft -= HandlePlayerLeft;

            Controller.RemoveEmitter(_emitter);
            Game.Components.Remove(_emitter);
            _emitter.Dispose();

            Controller.Dispose();

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            Controller.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Controller.Draw();
        }

        /// <summary>
        /// Got info about an open game.
        /// </summary>
        protected void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;

            var info = args.Data.ReadString();
            logger.Debug("Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers);
        }

        /// <summary>
        /// Got info that a new player joined the game.
        /// </summary>
        protected void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            logger.Debug("{0} joined.", args.Player);
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            logger.Debug("{0} left.", args.Player);
        }
    }
}
