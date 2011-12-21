using System;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
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

        #region Fields

        /// <summary>
        /// The static base information about the game world.
        /// </summary>
        private StaticWorld world;

        #endregion

        public ServerController(Game game, IServerSession session, byte worldSize, long worldSeed)
            : base(game, session)
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));

            var simulation = new DefaultSimulation();
            simulation.Command += GameCommandHandler.HandleCommand;
            tss.Initialize(simulation);

            tss.EntityManager.SystemManager.AddSystem(new PhysicsSystem())
                .AddSystem(new ShipControlSystem())
                .AddSystem(new WeaponSystem())
                .AddSystem(new AvatarSystem());
        }

        public override void Initialize()
        {
            Session.PlayerLeft += HandlePlayerLeft;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (Session != null)
            {
                Session.PlayerLeft -= HandlePlayerLeft;
            }

            base.Dispose(disposing);
        }

        protected override void HandleGameInfoRequested(object sender, EventArgs e)
        {
            var args = (RequestEventArgs)e;
            args.Data.Write("Hello there!");
        }

        protected override void HandleJoinRequested(object sender, EventArgs e)
        {
            // Send current game state to client.
            var args = (JoinRequestEventArgs)e;

            // Create a ship for the player.
            // TODO validate ship data (i.e. valid ship with valid equipment etc.)
            var playerData = (PlayerInfo)args.Player.Data;
            var ship = EntityFactory.CreateShip(playerData.Ship, args.Player.Number);
            ship.GetComponent<WeaponSlot>().Weapon = playerData.Weapon;
            AddEntity(ship, Simulation.CurrentFrame);
        }

        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            // Player left the game, remove his ship.
            RemoveEntity(tss.EntityManager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(args.Player.Number).UID, Simulation.CurrentFrame);
        }
    }
}
