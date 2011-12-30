using System;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Entities;
using Space.Data;
using Space.Session;

namespace Space.Control
{
    public class GameServer : GameComponent
    {
        public ISimulationController<IServerSession> Controller { get; set; }

        public GameServer(Game game)
            : base(game)
        {
            Controller = ControllerFactory.CreateServer(game);

            Controller.Session.GameInfoRequested += HandleGameInfoRequested;
            Controller.Session.PlayerLeft += HandlePlayerLeft;
            Controller.Session.JoinRequested += HandleJoinRequested;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Controller.Session.GameInfoRequested -= HandleGameInfoRequested;
                Controller.Session.PlayerLeft -= HandlePlayerLeft;
                Controller.Session.JoinRequested -= HandleJoinRequested;

                Controller.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            Controller.Update(gameTime);
        }

        private void HandleGameInfoRequested(object sender, EventArgs e)
        {
            var args = (RequestEventArgs)e;

            args.Data.Write("Hello there!");
        }

        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            // Player left the game, remove his ship.
            Controller.Simulation.EntityManager.RemoveEntity(Controller.Simulation.EntityManager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(args.Player.Number).UID);
        }

        private void HandleJoinRequested(object sender, EventArgs e)
        {
            // Send current game state to client.
            var args = (JoinRequestEventArgs)e;

            // Create a ship for the player.
            // TODO validate ship data (i.e. valid ship with valid equipment etc.)
            var playerData = (PlayerData)args.Player.Data;
            var ship = EntityFactory.CreateShip(playerData.Ship, args.Player.Number.ToFraction());
            Controller.Simulation.EntityManager.AddEntity(ship);
        }
    }
}
