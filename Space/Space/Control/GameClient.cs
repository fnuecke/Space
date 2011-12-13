using System;
using System.Text;
using Engine.Network;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Model;
using Space.View;

namespace Space.Control
{
    class GameClient : DrawableGameComponent
    {
        public IClientSession<PlayerInfo, PacketizerContext> Session { get; private set; }
        public ClientController Controller { get; private set; }

        DeprecatedUdpProtocol protocol;
        private InputCommandEmitter emitter;

        SpriteFont font;

        public GameClient(Game game)
            : base(game)
        {
            protocol = new DeprecatedUdpProtocol(50101, Encoding.ASCII.GetBytes("Space"));
            Session = new ClientSession<PlayerInfo, PacketizerContext>(game, protocol);
            Controller = new ClientController(game, Session);
            Controller.UpdateOrder = 10;

            emitter = new InputCommandEmitter(game, Session, Controller);
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

            protocol.Dispose();
            Session.Dispose();
            Controller.Dispose();
            emitter.Dispose();

            Game.Components.Remove(Controller);
            Game.Components.Remove(emitter);

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            protocol.Update();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            var spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));

            // Draw debug stuff.
            var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 230, GraphicsDevice.Viewport.Height - 140);
            var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 360, GraphicsDevice.Viewport.Height - 140);

            SessionInfo.Draw("Client", Session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

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
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;

            Console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;

            Console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }

    }
}
