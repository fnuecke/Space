using System;
using System.Linq;
using System.Net;
using System.Text;
using Engine.Input;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Space.Game;
using Space.Simulation;
using SpaceData;

namespace Space
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Spaaace : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        KeyboardInputManager keyboardInput;
        GameConsole console;

        ShipData[] ships;
        Ship player;

        TSS<GameState, IGameObject> simulation;
        UdpProtocol protocol;
        ISession session;
        IServerSession server;
        IClientSession client;
        bool isServer;

        Texture2D background;

        private Keys[] previouslyPressedKeys = new Keys[0];

        public Spaaace()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            keyboardInput = new KeyboardInputManager(this);
            console = new GameConsole(this);

            console.AddCommand("server", args =>
            {
                StartNewSession(true);
            },
                "Switch to server mode (host a new game).");
            console.AddCommand("client", args =>
            {
                StartNewSession(false);
            },
                "Switch to client mode.");
            console.AddCommand("search", args =>
            {
                client.Search();
            },
                "Search for games available on the local subnet.");
            console.AddCommand("connect", args =>
            {
                client.Join(new IPEndPoint(IPAddress.Parse(args[1]), ushort.Parse(args[2])), "test", null);
            },
                "Joins a game at the given host.",
                "connect <host> <port> - join the host with the given hostname or IP.");
            console.AddCommand("leave", args =>
            {
                client.Leave();
            },
                "Leave the current game.");

            console.AddCommand("send", args =>
            {
                int player = int.Parse(args[1]);
                string text = args[2].Trim();
                if (String.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                Packet packet = new Packet();
                packet.Write(text);
                console.WriteLine("Sending text: " + text);
                session.Send(player, packet);
            }, "Send a command to another player.",
               "send <player> <message>");

            console.LineWritten += new EventHandler(delegate(object sender, EventArgs e)
            {
                Console.WriteLine(((LineWrittenEventArgs)e).Message);
            });
            //*/

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

            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });

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

            console.SpriteBatch = spriteBatch;
            console.Font = Content.Load<SpriteFont>("Fonts/ConsoleFont");

            console.WriteLine("test text");
            console.WriteLine("test text that is long and should probably be wrapped somewhen about now... ok maybe not. some more text ole ole ole! now that should suffice.");

            // TODO: use this.Content to load your game content here

            ships = Content.Load<ShipData[]>("ships");

            background = Content.Load<Texture2D>("Textures/stars");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private void StartNewSession(bool asHost)
        {
            if (protocol == null)
            {
                protocol = new UdpProtocol(6223, Encoding.UTF8.GetBytes("space"));
            }

            if (server != null)
            {
                server.Dispose();
                server = null;
            }
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            session = null;

            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });

            isServer = asHost;
            if (asHost)
            {
                server = SessionFactory.StartServer(protocol, 8);
                session = server;

                simulation.Synchronize(new GameState());

                server.GameInfoRequested += HandleGameInfoRequested;

            }
            else
            {
                client = SessionFactory.StartClient(protocol);
                session = client;

                client.GameInfoReceived += HandleGameFound;
                client.JoinResponse += HandleJoinResponse;
            }

            session.PlayerJoined += HandlePlayerJoined;
            session.PlayerLeft += HandlePlayerLeft;
            session.PlayerData += HandlePlayerData;
        }

        private void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;
            console.WriteLine(string.Format("Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));
        }

        private void HandleGameInfoRequested(object sender, EventArgs e)
        {
            var args = (RequestEventArgs)e;
            args.Data.Write("testdata");
        }

        private void HandleGameFound(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;
            var info = args.Data.ReadString();
            console.WriteLine(String.Format("Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        private void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("{0} joined.", args.Player));
        }

        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("{0} left.", args.Player));
        }

        private void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        private bool StartedPressing(Keys key)
        {
            return Keyboard.GetState(PlayerIndex.One).GetPressedKeys().Contains(key) &&
                !previouslyPressedKeys.Contains(key);
        }

        private bool StoppedPressing(Keys key)
        {
            return !Keyboard.GetState(PlayerIndex.One).GetPressedKeys().Contains(key) &&
                   previouslyPressedKeys.Contains(key);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (!simulation.WaitingForSynchronization)
            {
                var pressedKeys = Keyboard.GetState(PlayerIndex.One).GetPressedKeys();

                if (StartedPressing(Keys.Up))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.Accelerate, simulation.CurrentFrame + 1));
                }
                else if (StartedPressing(Keys.Down))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.Decelerate, simulation.CurrentFrame + 1));
                }
                else if (StoppedPressing(Keys.Up) || StoppedPressing(Keys.Down))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopMovement, simulation.CurrentFrame + 1));
                }

                if (StartedPressing(Keys.Left))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.TurnLeft, simulation.CurrentFrame + 1));
                }
                else if (StartedPressing(Keys.Right))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.TurnRight, simulation.CurrentFrame + 1));
                }
                else if (StoppedPressing(Keys.Left) || StoppedPressing(Keys.Right))
                {
                    //protocol.Send(new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopRotation, simulation.CurrentFrame + 1));
                }

                previouslyPressedKeys = pressedKeys;
            }

/*
            foreach (var command in protocol.Receive())
            {
                switch (command.Type)
                {
                    case (uint)CommandType.PlayerJoined:
                        Console.WriteLine("Player " + ((PlayerJoinedCommand)command).PlayerID + " joined.");
                        ++numPlayers;
                        break;

                    case (uint)CommandType.PlayerLeft:
                        Console.WriteLine("Player " + ((PlayerLeftCommand)command).PlayerID + " left.");
                        --numPlayers;
                        break;

                    case (uint)CommandType.GameStateQuery:
                        break;

                    case (uint)CommandType.GameState:
                        break;

                    case (uint)GameCommandType.PlayerInput:
                        simulation.PushCommand((ISimulationCommand)command);
                        break;

                    default:
                        // Unknown command.
                        Console.WriteLine("Received unknown command of type " + command.Type);
                        break;
                }
            }
*/

            if (protocol != null)
            {
                protocol.Receive();
                protocol.Flush();
            }

/*
            if (simulation.WaitingForSynchronization)
            {
                if (protocol.State == UDPProtocol.ProtocolState.Client && !sentSyncRequest)
                {
                    sentSyncRequest = true;
                    protocol.Send(new GameStateQueryCommand(), 0, Priority.High);
                }
            }
            else
            {
                if (protocol.State == UDPProtocol.ProtocolState.Host && player == null)
                {
                    player = new Ship(Content, ships[0], 0);
                    simulation.Add(new Ship(Content, ships[0], 0));
                }

                simulation.Step();
            }
*/

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
