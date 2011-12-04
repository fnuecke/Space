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

        UdpProtocol protocol;
        private InputCommandEmitter emitter;

        SpriteFont font;

        public GameClient(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(50101, Encoding.ASCII.GetBytes("Space"));
            Session = new ClientSession<PlayerInfo, PacketizerContext>(game, protocol);
            Controller = new ClientController(game, Session);
            Controller.UpdateOrder = 10;

            emitter = new InputCommandEmitter(game, Session, Controller);
            Controller.AddEmitter(emitter);

            DrawOrder = 10;
        }

        public override void Initialize()
        {
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
            var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 340, GraphicsDevice.Viewport.Height - 140);

            SessionInfo.Draw("Client", Session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

            base.Draw(gameTime);
        }
    }
}
