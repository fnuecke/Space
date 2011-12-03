using System;
using System.Net;
using System.Text;
using Engine.Input;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using Space.Control;
using Space.Model;
using Space.View;
using SpaceData;

namespace Space
{
    /// <summary>
    /// Main class, sets up services and basic components.
    /// </summary>
    public class Spaaace : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        PacketizerContext context;
        SpriteBatch spriteBatch;
        GameConsole console;

        UdpProtocol serverProtocol;
        UdpProtocol clientProtocol;
        IServerSession<PlayerInfo, PacketizerContext> serverSession;
        IClientSession<PlayerInfo, PacketizerContext> clientSession;
        Server serverController;
        Client clientController;

        public Spaaace()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Window.Title = "Spaaaaaace. Space. Spaaace. So much space!";
            IsMouseVisible = true;

            // Suspect for better sync, but not enough experimenting done, yet.
            //this.TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / 67f);
            //graphics.SynchronizeWithVerticalRetrace = true;

            // XNAs fixed timestep implementation doesn't suit us, to be gentle.
            // So we let it be dynamic and adjust for it as necessary, leading
            // to almost no desyncs at all! Yay!
            this.IsFixedTimeStep = false;

            // Create our object instantiation context. This must contain
            // everything a game object might need to rebuild it self from
            // its serialized data (for game states being sent).
            context = new PacketizerContext();
            context.game = this;
            Packetizer<PlayerInfo, PacketizerContext> packetizer = new Packetizer<PlayerInfo, PacketizerContext>(context);
            // Make the packetizer available for all game components.
            Services.AddService(typeof(IPacketizer<PlayerInfo, PacketizerContext>), packetizer);

            // Make some class available through it. The classes registered here
            // can be deserialized without the code triggering the deserialization
            // to actually know what it'll get. This is used in game states, e.g.
            // where the state only knows it has ISteppables, but not what the
            // actual implementations are.
            packetizer.Register<Ship>();
            packetizer.Register<PlayerInputCommand>();

            // Add some more utility components.
            Components.Add(new KeyboardInputManager(this));
            Components.Add(new MouseInputManager(this));
            console = new GameConsole(this);
            Components.Add(console);

            console.DrawOrder = 10;

            // Register some commands for our console, making debugging that much easier ;)
            console.AddCommand("server", args =>
            {
                RestartServer();
            },
                "Restart server logic.");
            console.AddCommand("client", args =>
            {
                RestartClient();
            },
                "Restart client logic.");
            console.AddCommand("search", args =>
            {
                clientController.Session.Search();
            },
                "Search for games available on the local subnet.");
            console.AddCommand("connect", args =>
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipType = "Sparrow";
                clientController.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), ushort.Parse(args[2])), args[3], info);
            },
                "Joins a game at the given host.",
                "connect <host> <port> - join the host with the given hostname or IP.");
            console.AddCommand("leave", args =>
            {
                clientController.Session.Leave();
            },
                "Leave the current game.");
            console.AddCommand(new[] { "fullscreen", "fs" }, args =>
            {
                graphics.ToggleFullScreen();
            },
                "Toggles fullscreen mode.");
            console.AddCommand("invalidate", args =>
            {
                clientController.DEBUG_InvalidateSimulation();
            },
                "Invalidates the client game state, requesting a snapshot from the server.");

            // Just for me, joining default testing server.
            console.AddCommand("joinfn", args =>
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipType = "Sparrow";
                clientController.Session.Join(new IPEndPoint(IPAddress.Parse("10.74.254.202"), 50100), "player", info);
            },
                "autojoin fn");
            // Just for me, joining default testing server.
            console.AddCommand("joinlh", args =>
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipType = "Sparrow";
                clientController.Session.Join(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50100), "player", info);
            },
                "autojoin localhost");

            // Copy everything written to our gameconsole to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
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

            var shipdata = Content.Load<ShipData[]>("Data/ships");
            foreach (var ship in shipdata)
            {
                context.shipData[ship.Name] = ship;
                context.shipTextures[ship.Name] = Content.Load<Texture2D>(ship.Texture);
            }

            context.game = this;

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
            //if (server == null)
            //{
            //    RestartServer();
            //}
            if (clientController == null)
            {
                RestartClient();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);

            if (serverController != null)
            {
                // Draw debug stuff.
                var ngOffset = new Vector2(150, GraphicsDevice.Viewport.Height - 100);
                var sessionOffset = new Vector2(10, GraphicsDevice.Viewport.Height - 100);

                SessionInfo.Draw("Server", serverSession, sessionOffset, console.Font, spriteBatch);
                NetGraph.Draw(serverProtocol.Information, ngOffset, console.Font, spriteBatch);
            }

            if (clientController != null)
            {
                // Draw debug stuff.
                var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 200, GraphicsDevice.Viewport.Height - 100);
                var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 340, GraphicsDevice.Viewport.Height - 100);

                SessionInfo.Draw("Client", clientSession, sessionOffset, console.Font, spriteBatch);
                NetGraph.Draw(clientProtocol.Information, ngOffset, console.Font, spriteBatch);
            }

            spriteBatch.Begin();

            string info = String.Format("FPS: {0:f} | Slow: {1}",
                System.Math.Ceiling(1 / (float)gameTime.ElapsedGameTime.TotalSeconds), gameTime.IsRunningSlowly);
            if (serverController != null)
            {
                info += String.Format("\nServerframe: {0}", serverController.DEBUG_CurrentFrame);
            }
            if (clientController != null)
            {
                info += String.Format("\nClientframe: {0}", clientController.DEBUG_CurrentFrame);
            }
            var infoPosition = new Vector2(GraphicsDevice.Viewport.Width - 10 - console.Font.MeasureString(info).X, 10);

            spriteBatch.DrawString(console.Font, info, infoPosition, Color.White);

            spriteBatch.End();
        }

        private void RestartServer()
        {
            if (serverController != null)
            {
                serverController.Dispose();
                Components.Remove(serverController);
            }
            if (serverSession != null)
            {
                serverSession.Dispose();
            }
            if (serverProtocol != null)
            {
                serverProtocol.Dispose();
                Components.Remove(serverProtocol);
            }
            serverProtocol = new UdpProtocol(this, 50100, Encoding.ASCII.GetBytes("Space"));
            serverSession = new ServerSession<PlayerInfo, PacketizerContext>(this, serverProtocol, 8);
            serverController = new Server(this, serverSession, 10, 0);
            serverController.UpdateOrder = 10;

            Components.Add(serverProtocol);
            Components.Add(serverController);
        }

        private void RestartClient()
        {
            if (clientController != null)
            {
                clientController.Dispose();
                Components.Remove(clientController);
            }
            if (clientSession != null)
            {
                clientSession.Dispose();
            }
            if (clientProtocol != null)
            {
                clientProtocol.Dispose();
                Components.Remove(clientProtocol);
            }
            clientProtocol = new UdpProtocol(this, 50101, Encoding.ASCII.GetBytes("Space"));
            clientSession = new ClientSession<PlayerInfo, PacketizerContext>(this, clientProtocol);
            clientController = new Client(this, clientSession);
            clientController.UpdateOrder = 10;
            clientController.AddEmitter(new InputCommandEmitter(this, clientController.Session));

            Components.Add(clientProtocol);
            Components.Add(clientController);
        }
    }
}
