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
        ServerController controller;
        SpriteFont font;

        public GameServer(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(50100, Encoding.ASCII.GetBytes("Space"));
            session = new ServerSession<PlayerInfo, PacketizerContext>(game, protocol, 8);
            controller = new ServerController(game, session, 10, 0);
            controller.UpdateOrder = 10;

            DrawOrder = 10;
        }

        public override void Initialize()
        {
            Game.Components.Add(controller);

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
            controller.Dispose();

            Game.Components.Remove(controller);

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
            var ngOffset = new Vector2(150, GraphicsDevice.Viewport.Height - 100);
            var sessionOffset = new Vector2(10, GraphicsDevice.Viewport.Height - 100);

            SessionInfo.Draw("Server", session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

            base.Draw(gameTime);
        }
    }
}
