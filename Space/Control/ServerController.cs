using System;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Simulation;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class ServerController : AbstractTssServer
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        public ServerController(Game game, IServerSession session, byte worldSize, ulong worldSeed)
            : base(session)
        {
            var simulation = new DefaultSimulation();
            simulation.Command += GameCommandHandler.HandleCommand;
            tss.Initialize(simulation);

            tss.EntityManager.SystemManager
                .AddSystem(new DefaultLogicSystem())
                .AddSystem(new ShipControlSystem())
                .AddSystem(new AvatarSystem())
                .AddSystem(new CellSystem())
                .AddSystem(new IndexSystem())
                .AddSystem(new CollisionSystem())
                .AddSystem(new UniversalSystem(game.Content.Load<WorldConstaints>("Data/world")));

        }

        protected override void HandleJoinRequested(object sender, EventArgs e)
        {
            // Send current game state to client.
            var args = (JoinRequestEventArgs)e;

            // Create a ship for the player.
            // TODO validate ship data (i.e. valid ship with valid equipment etc.)
            var playerData = (PlayerInfo)args.Player.Data;
            var ship = EntityFactory.CreateShip(playerData.Ship, args.Player.Number);
            AddEntity(ship, Simulation.CurrentFrame);
        }
    }
}
