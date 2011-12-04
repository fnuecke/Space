using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Network;
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
    class ServerController : AbstractTssServer<GameState, IGameObject, GameCommand, GameCommandType, PlayerInfo, PacketizerContext>
    {
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
            Session.PlayerLeft -= HandlePlayerLeft;

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
            var ship = new Ship(args.Player.Data.ShipType, args.Player.Number, Packetizer.Context);
            args.Player.Data.ShipUID = AddSteppable(ship);
#if DEBUG
            Console.WriteLine("{0} => ship id: {1}", args.Player, ship.UID);
#endif

            // Now serialize the game state, with the player ship in it.
            Simulation.Packetize(args.Data);
        }

        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            // Player left the game, remove his ship.
            RemoveSteppable(args.Player.Data.ShipUID);
        }

        protected override bool HandleRemoteCommand(IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player sent input.
                    {
                        Apply(command, PacketPriority.High);
                    }
                    return true;

                default:
#if DEBUG
                    Console.WriteLine("Server: got a command we couldn't handle: " + command.Type);
#endif
                    break;
            }

            // Got here -> unhandled.
            return false;
        }

        #region Debugging stuff

        internal long DEBUG_CurrentFrame { get { return Simulation.CurrentFrame; } }

        #endregion
    }
}
