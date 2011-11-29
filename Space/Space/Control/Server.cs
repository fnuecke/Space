using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Network;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.Model;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class Server : AbstractUdpServer<PlayerInfo, GameCommandType>
    {
        #region Fields

        /// <summary>
        /// The static base information about the game world.
        /// </summary>
        private StaticWorld world;

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject, GameCommandType, PlayerInfo> simulation;

        #endregion

        public Server(Game game, int maxPlayers, byte worldSize, long worldSeed)
            : base(game, maxPlayers, 8442, "5p4c3!")
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation = new TSS<GameState, IGameObject, GameCommandType, PlayerInfo>(new[] { 50 }, new GameState(game, Session));
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
            var args = (RequestEventArgs)e;
            simulation.Packetize(args.Data);
        }

        protected override void HandleCommand(ICommand<GameCommandType, PlayerInfo> command)
        {
            command.IsTentative = false;
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    {
                        var simulationCommand = (ISimulationCommand<GameCommandType, PlayerInfo>)command;
                        if (simulationCommand.Frame > simulation.TrailingFrame)
                        {
                            // OK, in allowed timeframe, send it to all clients.
                            simulation.PushCommand(simulationCommand);
                            SendAll(command);
                        }
                        else
                        {
                            console.WriteLine("Got a command we couldn't use, " + simulationCommand.Frame + "<" + simulation.TrailingFrame);
                        }
                    }
                    break;
            }
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo>)e;
            console.WriteLine(String.Format("SRV.NET: {0} joined.", args.Player));

            var command = new AddPlayerCommand(args.Player, simulation.CurrentFrame + 1);
            command.IsTentative = false;
            simulation.PushCommand(command);
            SendAll(command, 50);
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs<PlayerInfo>)e;
            console.WriteLine(String.Format("SRV.NET: {0} left.", args.Player));
        }

        #region Debugging stuff

        private Texture2D pixelTexture;

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(Game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData(new[] { Color.White });
            }

            // Session information.
            SpriteFont font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");

            string sessionInfo = "Server (" + Session.NumPlayers + "/" + Session.MaxPlayers + ")";
            for (int i = 0; i < Session.NumPlayers; ++i)
            {
                var player = Session.GetPlayer(i);
                sessionInfo += "\n#" + player.Number + ": " + player.Name + " [" + player.Ping + "]";
            }

            var sessionInfoMeasure = font.MeasureString(sessionInfo);
            var sessionInfoPosition = new Vector2(10, Game.GraphicsDevice.Viewport.Height - sessionInfoMeasure.Y - 10);

            // Network graph.
            int minIncoming = int.MaxValue, maxIncoming = 0, avgIncoming = 0,
                minOutgoing = int.MaxValue, maxOutgoing = 0, avgOutgoing = 0;

            // Used to skip first entry, as that one's subject to change.
            int x = 0;
            foreach (var incoming in protocol.Information.IncomingTraffic)
            {
                if (x++ == 0)
                {
                    continue;
                }
                int val = incoming[TrafficType.Any];
                if (val < minIncoming)
                {
                    minIncoming = val;
                }
                if (val > maxIncoming)
                {
                    maxIncoming = val;
                }
                avgIncoming += val;
            }
            avgIncoming /= protocol.Information.IncomingTraffic.Count;

            x = 0;
            foreach (var outgoing in protocol.Information.OutgoingTraffic)
            {
                if (x++ == 0)
                {
                    continue;
                }
                int val = outgoing[TrafficType.Any];
                if (val < minOutgoing)
                {
                    minOutgoing = val;
                }
                if (val > maxOutgoing)
                {
                    maxOutgoing = val;
                }
                avgOutgoing += val;
            }
            avgOutgoing /= protocol.Information.OutgoingTraffic.Count;

            string netInfo = String.Format("in: {0}|{1}|{2} - {3:f}kB/s\nout: {4}|{5}|{6} - {7:f}kB/s", minIncoming, maxIncoming, avgIncoming, avgIncoming / 1024f, minOutgoing, maxOutgoing, avgOutgoing, avgOutgoing / 1024f);
            var netInfoMeasure = font.MeasureString(netInfo);

            int graphWidth = 180, graphHeight = 40;

            var netInfoPosition = new Vector2(sessionInfoPosition.X + sessionInfoMeasure.X + 10, Game.GraphicsDevice.Viewport.Height - graphHeight - netInfoMeasure.Y - 10);

            float graphNormX = graphWidth / (float)System.Math.Max(protocol.Information.IncomingTraffic.Count,
                                                           protocol.Information.OutgoingTraffic.Count);
            float graphNormY = graphHeight / (float)(System.Math.Max(maxIncoming, maxOutgoing) + 1);

            Vector2 graphPosition = new Vector2(netInfoPosition.X, Game.GraphicsDevice.Viewport.Height - graphHeight - 10);

            // Draw it.
            spriteBatch.Begin();
            spriteBatch.DrawString(font, sessionInfo, sessionInfoPosition, Color.White);
            spriteBatch.DrawString(font, netInfo, netInfoPosition, Color.White);


            var values = new Tuple<float, Color>[System.Math.Max(protocol.Information.IncomingTraffic.Count, protocol.Information.OutgoingTraffic.Count) - 1][];
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = new Tuple<float, Color>[6];
            }

            x = 0;
            foreach (var incoming in protocol.Information.IncomingTraffic)
            {
                if (x > 0)
                {
                    float yAny = incoming[TrafficType.Any] * graphNormY;
                    float yData = incoming[TrafficType.Data] * graphNormY;
                    float yProto = incoming[TrafficType.Protocol] * graphNormY;
                    values[x - 1][0] = Tuple.Create(yProto, Color.Yellow);
                    values[x - 1][1] = Tuple.Create(yData, Color.Orange);
                    values[x - 1][2] = Tuple.Create(yAny, Color.Red);
                }
                x++;
            }

            x = 0;
            foreach (var outgoing in protocol.Information.OutgoingTraffic)
            {
                if (x > 0)
                {
                    float yAny = outgoing[TrafficType.Any] * graphNormY;
                    float yData = outgoing[TrafficType.Data] * graphNormY;
                    float yProto = outgoing[TrafficType.Protocol] * graphNormY;
                    values[x - 1][3] = Tuple.Create(yProto, Color.Blue);
                    values[x - 1][4] = Tuple.Create(yData, Color.Turquoise);
                    values[x - 1][5] = Tuple.Create(yAny, Color.Green);
                }
                x++;
            }

            for (x = 0; x < values.Length - 1; ++x)
            {
                Array.Sort(values[x], (a, b) =>
                {
                    if (a.Equals(b))
                    {
                        return 0;
                    }
                    else if (a.Item1 < b.Item1)
                    {
                        return 1;
                    }
                    return -1;
                });
                float lastY = graphPosition.Y + graphHeight;
                foreach (var item in values[x])
                {
                    if (item.Item1 > 0)
                    {
                        var line = new Rectangle((int)(graphPosition.X + x * graphNormX),
                            (int)(graphPosition.Y + graphHeight - item.Item1),
                            (int)graphNormX, (int)(lastY - item.Item1));
                        spriteBatch.Draw(pixelTexture, line, item.Item2);
                        lastY = line.Y;
                    }
                }
            }

            // Draw player 0 ship.
            {
                var player = Session.GetPlayer(0);
                if (player != null)
                {
                    var ship = (Ship)simulation.Get(player.Data.ShipUID);
                    if (ship != null)
                    {
                        ship.Draw(null, new Vector2(), spriteBatch);
                    }
                }
            }

            spriteBatch.End();
        }

        internal long DEBUG_CurrentFrame { get { return simulation.CurrentFrame; } }

        #endregion
    }
}
