using System;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.Session;

namespace Space.Control
{
    public class GameServer : GameComponent
    {
        internal IServerSession Session { get; private set; }
        internal ServerController Controller { get; private set; }

        public GameServer(Game game)
            : base(game)
        {
            Session = new HybridServerSession<PlayerData>(7777, 8);
            Controller = new ServerController(game, Session, 10, 0);

            Session.GameInfoRequested += HandleGameInfoRequested;
            Session.PlayerLeft += HandlePlayerLeft;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Session.GameInfoRequested -= HandleGameInfoRequested;
                Session.PlayerLeft -= HandlePlayerLeft;

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

        private void HandleGameInfoRequested(object sender, EventArgs e)
        {
            var args = (RequestEventArgs)e;
            args.Data.Write("Hello there!");
        }

        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            // Player left the game, remove his ship.
            Controller.RemoveEntity(Controller.Simulation.EntityManager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(args.Player.Number).UID, Controller.Simulation.CurrentFrame);
        }
    }
}
