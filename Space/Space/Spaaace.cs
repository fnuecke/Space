using System;
using System.Net;
using Engine.Input;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.Control;
using Space.Model;

namespace Space
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Spaaace : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameConsole console;
        Server server;
        Client client;

        static Spaaace()
        {
        }

        public Spaaace()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            PacketizerContext context = new PacketizerContext();
            context.game = this;
            Packetizer<PacketizerContext> packetizer = new Packetizer<PacketizerContext>(context);

            Services.AddService(typeof(IPacketizer<PacketizerContext>), packetizer);

            packetizer.Register<Ship>();
            packetizer.Register<AddPlayerCommand>();
            packetizer.Register<GameStateRequestCommand>();
            packetizer.Register<GameStateResponseCommand>();
            packetizer.Register<PlayerInputCommand>();
            packetizer.Register<RemovePlayerCommand>();
            packetizer.Register<SynchronizeCommand>();

            Components.Add(new GameObjectFactory(this));
            Components.Add(new KeyboardInputManager(this));
            console = new GameConsole(this);
            Components.Add(console);

            console.AddCommand("server", args =>
            {
                if (server != null)
                {
                    server.Dispose();
                }
                server = new Server(this, 8, 10, 0);
            },
                "Switch to server mode (host a new game).");
            console.AddCommand("client", args =>
            {
                if (client != null)
                {
                    client.Dispose();
                }
                client = new Client(this);
            },
                "Switch to client mode.");
            console.AddCommand("search", args =>
            {
                client.Session.Search();
            },
                "Search for games available on the local subnet.");
            console.AddCommand("connect", args =>
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipName = "Sparrow";
                client.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), ushort.Parse(args[2])), args[3], info);
            },
                "Joins a game at the given host.",
                "connect <host> <port> - join the host with the given hostname or IP.");
            console.AddCommand("leave", args =>
            {
                client.Session.Leave();
            },
                "Leave the current game.");

            console.LineWritten += delegate(object sender, EventArgs e)
            {
                Console.WriteLine(((LineWrittenEventArgs)e).Message);
            };
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Services.AddService(typeof(SpriteBatch), spriteBatch);

            console.SpriteBatch = spriteBatch;
            console.Font = Content.Load<SpriteFont>("Fonts/ConsoleFont");

            console.WriteLine("Game Console. Type 'help' for available commands.");
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (server == null)
            {
                server = new Server(this, 8, 10, 0);
                client = new Client(this);

                Components.Add(server);
                Components.Add(client);

                //PlayerInfo info = new PlayerInfo();
                //info.ShipName = "Sparrow";
                //client.Session.Join(new IPEndPoint(IPAddress.Parse("10.74.254.202"), 8442), "player", info);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            if (server != null)
            {
                server.DEBUG_DrawInfo(spriteBatch);
            }

            if (client != null)
            {
                client.DEBUG_DrawInfo(spriteBatch);
            }

            spriteBatch.Begin();

            string info = String.Format("FPS: {0:f} | Slow: {1}\nServerframe: {2}\nClientframe: {3}",
                System.Math.Ceiling(1 / (float)gameTime.ElapsedGameTime.TotalSeconds),
                gameTime.IsRunningSlowly,
                (server == null) ? 0 : server.DEBUG_CurrentFrame,
                (client == null) ? 0 : client.DEBUG_CurrentFrame);

            spriteBatch.DrawString(console.Font, info, new Vector2(10, 10), Color.White);

            spriteBatch.End();

/*

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);

            var ship = simulation.LeadingState.GetPlayerShip(protocol.ClientId);
            Vector2 translation = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            if (ship != null)
            {
                translation.X -= (float)ship.Position.X.DoubleValue;
                translation.Y -= (float)ship.Position.Y.DoubleValue;
            }

            spriteBatch.Draw(background, Vector2.Zero, new Rectangle(-(int)translation.X, -(int)translation.Y, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            spriteBatch.End();

            spriteBatch.Begin();

            for (var iter = simulation.Children; iter.MoveNext();)
            {
                iter.Current.Draw(gameTime, translation, spriteBatch);
            }

            string status = "Status: ";
            switch (protocol.State)
            {
                case UDPProtocol.ProtocolState.Unconnected:
                    status += "unconnected";
                    break;
                case UDPProtocol.ProtocolState.Host:
                    status += "host";
                    break;
                case UDPProtocol.ProtocolState.Joining:
                    status += "joining";
                    break;
                case UDPProtocol.ProtocolState.Client:
                    status += "client";
                    break;
                default:
                    break;
            }
            status += "\nPlayers: " + numPlayers;
            status += "\nRequest: " + gotRequest;
            spriteBatch.DrawString(digitalFont, status, Vector2.Zero, Color.YellowGreen);

            spriteBatch.End();
*/

            base.Draw(gameTime);
        }
    }
}
