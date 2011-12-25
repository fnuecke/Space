using System;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Simulation;

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
        public SimpleClientController<PlayerInfo> Controller { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        ///  Command emitter used to get player input.
        /// </summary>
        private InputCommandEmitter _emitter;

        #endregion

        SpriteFont font;

        public GameClient(Game game)
            : base(game)
        {
            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

            // Create our client controller.
            Controller = new SimpleClientController<PlayerInfo>(GameCommandHandler.HandleCommand);

            // Register for events.
            Controller.Session.PlayerJoined += HandlePlayerJoined;
            Controller.Session.PlayerLeft += HandlePlayerLeft;

            // Add all systems we need in our game.
            Controller.Simulation.EntityManager.SystemManager.AddSystems(
                new[]
                {
                    new DefaultLogicSystem(),
                    new ShipControlSystem(),
                    new AvatarSystem(),
                    new CellSystem(),
                    new PlayerCenteredSoundSystem(soundBank, Controller.Session),
                    new PlayerCenteredRenderSystem(spriteBatch, game.Content, Controller.Session)
                                .AddComponent(new Background("Textures/stars")),
                    new UniversalSystem(game.Content.Load<WorldConstaints>("Data/world"))
                });

            // Create our input command emitter, which is used to grab user
            // input and convert it into commands that can be injected into our
            // simulation.
            _emitter = new InputCommandEmitter(game, Controller.Session, Controller.Simulation);
            Controller.AddEmitter(_emitter);
            Game.Components.Add(_emitter);

            // Draw underneath menus etc.
            DrawOrder = -50;
        }

        protected override void LoadContent()
        {
            font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");

            base.LoadContent();
        }

        protected override void Dispose(bool disposing)
        {
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
