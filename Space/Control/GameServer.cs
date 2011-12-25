using Engine.Session;
using Microsoft.Xna.Framework;
using Space.Simulation;

namespace Space.Control
{
    public class GameServer : GameComponent
    {
        internal IServerSession Session { get; private set; }
        internal ServerController Controller { get; private set; }

        public GameServer(Game game)
            : base(game)
        {
            Session = new HybridServerSession<PlayerInfo>(50100, 8);
            Controller = new ServerController(game, Session, 10, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Session.Dispose();
                Controller.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            Session.Update();
            Controller.Update(gameTime);
        }
    }
}
