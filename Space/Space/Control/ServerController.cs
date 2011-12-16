using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.Commands;
using Space.Model;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class ServerController : AbstractTssServer<GameCommand, PlayerInfo, PacketizerContext>
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

        public ServerController(Game game, IServerSession<PlayerInfo, PacketizerContext> session, byte worldSize, long worldSeed)
            : base(game, session)
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            Simulation.Initialize(new GameState(game, Session));
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
            var args = (JoinRequestEventArgs<PlayerInfo, PacketizerContext>)e;

            // Create a ship for the player.
            // TODO validate ship data (i.e. valid ship with valid equipment etc.)
            var ship = new Ship(args.Player.Data.ShipType, args.Player.Number, Packetizer.Context);
            args.Player.Data.ShipUID = AddEntity(ship);
        }

        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            // Player left the game, remove his ship.
            RemoveEntity(args.Player.Data.ShipUID);
        }

        protected override bool HandleRemoteCommand(IFrameCommand<PlayerInfo, PacketizerContext> command)
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
