using System;
using Engine.Commands;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.Commands;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems;
using Space.Simulation;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class ServerController : AbstractTssServer<GameCommand>
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

            Simulation.Initialize(new GameState());

            PhysicsSystem physics = new PhysicsSystem();
            ShipControlSystem controls = new ShipControlSystem();

            Simulation.SystemManager.AddSystem(physics);
            Simulation.SystemManager.AddSystem(controls);
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
            AddEntity(new Ship(playerData.Ship, args.Player.Number));
        }

        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            // Player left the game, remove his ship.
            RemoveEntity(Simulation.SystemManager.GetSystem<AvatarSystem>().GetAvatar(args.Player).UID);
        }

        protected override bool HandleRemoteCommand(IFrameCommand command)
        {
            // Check what we have.
            switch ((GameCommandType)command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player sent input.
                    Apply(command);
                    return true;

                default:
                    logger.Debug("Got unknown command type: {0}", command.Type);
                    break;
            }

            // Got here -> unhandled.
            return false;
        }
    }
}
