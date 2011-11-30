using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
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
    class Server : AbstractUdpServer<PlayerInfo, GameCommandType, PacketizerContext>
    {
        #region Fields

        /// <summary>
        /// The static base information about the game world.
        /// </summary>
        private StaticWorld world;

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext> simulation;

        #endregion

        public Server(Game game, int maxPlayers, byte worldSize, long worldSeed)
            : base(game, maxPlayers, 50100, "5p4c3!")
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation = new TSS<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>(new uint[] { 50 }, new GameState(game, Session));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Drive game logic.
            simulation.Update();
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
            simulation.Packetize(args.Data);

            // Validate player data.
            args.PlayerData.ShipUID = 0;
        }

        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo, PacketizerContext> command)
        {
            switch (command.Type)
            {
                case GameCommandType.Synchronize:
                    // Client resyncing.
                    {
                        SynchronizeCommand syncCommand = (SynchronizeCommand)command;
                        Send(command.Player.Number, new SynchronizeCommand(syncCommand.ClientFrame, simulation.CurrentFrame));
                    }
                    break;
                case GameCommandType.PlayerInput:
                    // Player sent input.
                    {
                        var simulationCommand = (ISimulationCommand<GameCommandType, PlayerInfo, PacketizerContext>)command;
                        if (simulationCommand.Frame > simulation.TrailingFrame)
                        {
                            // OK, in allowed timeframe, mark as valid and send it to all clients.
                            simulationCommand.IsTentative = false;
                            simulation.PushCommand(simulationCommand);
                            SendAll(command);
                        }
                        else
                        {
                            console.WriteLine("Got a command we couldn't use, " + simulationCommand.Frame + "<" + simulation.TrailingFrame);
                        }
                    }
                    break;
                case GameCommandType.GameStateRequest:
                    // Client needs game state.
                    {
                        Send(command.Player.Number, new GameStateResponseCommand(simulation), 200);
                    }
                    break;
                default:
                    throw new ArgumentException("command");
            }
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("SRV.NET: {0} joined.", args.Player));

            // New player joined the game, create a ship for him.
            var ship = new Ship(args.Player.Data.ShipType, args.Player.Number, packetizer.Context);
            simulation.Add(ship);
            Console.WriteLine("{0} => ship id: {1}", args.Player, ship.UID);
            SetPlayerShipUid(args.Player, ship.UID);
            SendObjectAdded(ship);
        }

        private void SetPlayerShipUid(Player<PlayerInfo, PacketizerContext> player, long id)
        {
            player.Data.ShipUID = id;
            Packet packet = new Packet();
            packet.Write(id);
            SendAll(new PlayerDataChangedCommand(player, PlayerInfoField.ShipId, packet), 100);
        }

        private void SendObjectAdded(IGameObject obj)
        {
            Packet packet = new Packet();
            packetizer.Packetize(obj, packet);
            SendAll(new AddGameObjectCommand(packet, simulation.CurrentFrame), 100);
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo, PacketizerContext>)e;
            console.WriteLine(String.Format("SRV.NET: {0} left.", args.Player));

            // Player left the game, remove his ship.
            simulation.Remove(args.Player.Data.ShipUID);
            SendAll(new RemoveGameObjectCommand(args.Player.Data.ShipUID, simulation.CurrentFrame), 200);
        }

        #region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // Draw world elements.
            //spriteBatch.Begin();
            //foreach (var child in simulation.Children)
            //{
            //    child.Draw(null, Vector2.Zero, spriteBatch);
            //}
            //spriteBatch.End();

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
