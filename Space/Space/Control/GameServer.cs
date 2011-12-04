using System.Text;
using Engine.Network;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Model;
using Space.View;

namespace Space.Control
{
    public class GameServer : DrawableGameComponent
    {
        UdpProtocol protocol;
        IServerSession<PlayerInfo, PacketizerContext> session;
        internal ServerController Controller { get; private set; }
        SpriteFont font;

        public GameServer(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(50100, Encoding.ASCII.GetBytes("Space"));
            session = new ServerSession<PlayerInfo, PacketizerContext>(game, protocol, 8);
            Controller = new ServerController(game, session, 10, 0);
            Controller.UpdateOrder = 10;

            DrawOrder = 10;
        }

        public override void Initialize()
        {
            Game.Components.Add(Controller);

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
            session.Dispose();
            Controller.Dispose();

            Game.Components.Remove(Controller);

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
            var ngOffset = new Vector2(150, GraphicsDevice.Viewport.Height - 140);
            var sessionOffset = new Vector2(10, GraphicsDevice.Viewport.Height - 140);

            SessionInfo.Draw("Server", session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

            base.Draw(gameTime);
        }
    }
}
