using System;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Simulation;
using Space.View;

namespace Space.Control
{
    class GameClient : DrawableGameComponent
    {
        internal IClientSession Session { get; private set; }
        internal ClientController Controller { get; private set; }

        private InputCommandEmitter emitter;

        SpriteFont font;

        public GameClient(Game game)
            : base(game)
        {
            Session = new HybridClientSession<PlayerInfo>(game);
            Controller = new ClientController(game, Session);
            Controller.UpdateOrder = 10;

            //emitter = new InputCommandEmitter(game, Session, Controller.Simulation.SystemManager.GetSystem<AvatarSystem>());
            Controller.AddEmitter(emitter);

            DrawOrder = 10;
        }

        public override void Initialize()
        {
            Session.PlayerJoined += HandlePlayerJoined;
            Session.PlayerLeft += HandlePlayerLeft;

            Game.Components.Add(Controller);
            Game.Components.Add(emitter);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");

            base.LoadContent();
        }

        protected override void Dispose(bool disposing)
        {
            Session.PlayerJoined -= HandlePlayerJoined;
            Session.PlayerLeft -= HandlePlayerLeft;

            Session.Dispose();
            Controller.Dispose();
            emitter.Dispose();

            Game.Components.Remove(Controller);
            Game.Components.Remove(emitter);

            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            var spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));

            // Draw debug stuff.
            var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 230, GraphicsDevice.Viewport.Height - 140);
            var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 360, GraphicsDevice.Viewport.Height - 140);

            SessionInfo.Draw("Client", Session, sessionOffset, font, spriteBatch);
            //NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Got info about an open game.
        /// </summary>
        protected void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;

            var info = args.Data.ReadString();
            Console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        /// <summary>
        /// Got info that a new player joined the game.
        /// </summary>
        protected void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            Console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            Console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

    }
}
