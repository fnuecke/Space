using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.Model;
using Space.View;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class Server : AbstractTssUdpServer<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>
    {
        #region Fields

        /// <summary>
        /// The static base information about the game world.
        /// </summary>
        private StaticWorld world;

        #endregion

        public Server(Game game, int maxPlayers, byte worldSize, long worldSeed)
            : base(game, maxPlayers, 50100, "5p4c3!")
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation.Initialize(new GameState(game, Session));
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
            var ship = new Ship(args.Player.Data.ShipType, args.Player.Number, packetizer.Context);
            args.Player.Data.ShipUID = AddSteppable(ship);
            Console.WriteLine("{0} => ship id: {1}", args.Player, ship.UID);

            // Now serialize the game state, with the player ship in it.
            simulation.Packetize(args.Data);
        }

        protected override bool HandleCommand(IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    // Player sent input.
                    {
                        Apply(command, 30);
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

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("Server: {0} joined.", args.Player));
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("Server: {0} left.", args.Player));

            // Player left the game, remove his ship.
            RemoveSteppable(args.Player.Data.ShipUID);
        }

        #region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // Draw debug stuff.
            SpriteFont font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");

            var ngOffset = new Vector2(150, Game.GraphicsDevice.Viewport.Height - 100);
            var sessionOffset = new Vector2(10, Game.GraphicsDevice.Viewport.Height - 100);

            SessionInfo.Draw("Server", Session, sessionOffset, font, spriteBatch);
            NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);
        }

        internal long DEBUG_CurrentFrame { get { return simulation.CurrentFrame; } }

        #endregion
    }
}
