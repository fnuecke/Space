using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using Engine.Input;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using NLog;
using Space.Control;
using Space.Data;
using Space.ScreenManagement;
using Space.ScreenManagement.Screens;
using Space.Session;
using Space.View;

namespace Space
{
    /// <summary>
    /// Main class, sets up services and basic components.
    /// </summary>
    public class Spaaace : Microsoft.Xna.Framework.Game
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// Relative path to the file we store user settings in.
        /// </summary>
        private const string SettingsFile = "config.xml";

        #endregion

        #region Properties

        /// <summary>
        /// The graphics device manager used in this game.
        /// </summary>
        public GraphicsDeviceManager GraphicsDeviceManager { get; set; }

        /// <summary>
        /// The game server currently running in this program.
        /// </summary>
        public GameServer Server { get; set; }

        /// <summary>
        /// The game client currently running in this program.
        /// </summary>
        public GameClient Client { get; set; }

        #endregion

        #region Fields

        private List<IGameComponent> _componentsToDispose = new List<IGameComponent>();

        private SpriteBatch _spriteBatch;
        private ScreenManager _screenManager;
        private GameConsole _console;

        private AudioEngine _audioEngine;
        private WaveBank _waveBank;
        private SoundBank _soundBank;

        private DoubleSampling _fps = new DoubleSampling(30);

        #endregion

        #region Constructor

        public Spaaace()
        {
            logger.Info("Starting up program...");

            // Load settings. Save on exit.
            Settings.Load(SettingsFile);
            Exiting += (object sender, EventArgs e) =>
            {
                logger.Info("Shutting down program...");
                Settings.Save(SettingsFile);
            };

            // Set up display.
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.PreferredBackBufferWidth = Settings.Instance.ScreenWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = Settings.Instance.ScreenHeight;
            GraphicsDeviceManager.IsFullScreen = Settings.Instance.Fullscreen;

            // We really want to do this, because it keeps the game from running at one billion
            // frames per second -- which sounds fun, but isn't, because game states won't update
            // properly anymore (because elapsed time since last step will always appear to be zero).
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true;

            // XNAs fixed time step implementation doesn't suit us, to be gentle.
            // So we let it be dynamic and adjust for it as necessary, leading
            // to almost no desyncs at all! Yay!
            IsFixedTimeStep = false;

            // Create our own, localized content manager.
            Content = new LocalizedContentManager(Services);

            // Remember to keep this in sync with the content project.
            Content.RootDirectory = "data";

            // Get locale for localized content.
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(Settings.Instance.Language);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.InvariantCulture;
                Settings.Instance.Language = culture.Name;
            }
            MenuStrings.Culture = culture;
            ((LocalizedContentManager)Content).Culture = culture;

            // Some window settings.
            Window.Title = "Space. The Game. Seriously.";
            IsMouseVisible = true;

            Components.ComponentRemoved += delegate(object sender, GameComponentCollectionEventArgs e)
            {
                _componentsToDispose.Add(e.GameComponent);
            };

            // Add some more utility components.
            Components.Add(new KeyboardInputManager(this));
            Components.Add(new MouseInputManager(this));

            _console = new GameConsole(this);
            Components.Add(_console);

            // Create the screen manager component.
            _screenManager = new ScreenManager(this);
            Components.Add(_screenManager);

            // Activate the first screens.
            _screenManager.AddScreen(new BackgroundScreen());
            _screenManager.AddScreen(new MainMenuScreen());

            // Add a logging target that'll write to our console.
            new GameConsoleTarget(this, LogLevel.Debug);

            // More console setup.
            _console.Hotkey = Settings.Instance.ConsoleKey;

            _console.AddCommand(new[] { "fullscreen", "fs" }, args =>
            {
                GraphicsDeviceManager.ToggleFullScreen();
            },
                "Toggles fullscreen mode.");

            _console.AddCommand("search", args =>
            {
                Client.Controller.Session.Search();
            },
                "Search for games available on the local subnet.");
            _console.AddCommand("connect", args =>
            {
                PlayerData playerData = new PlayerData();
                playerData.Ship = Content.Load<ShipData[]>("Data/ships")[0];
                Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), 7777), Settings.Instance.PlayerName, playerData);
            },
                "Joins a game at the given host.",
                "connect <host> - join the host with the given host name or IP.");
            _console.AddCommand("leave", args =>
            {
                DisposeClient();
            },
                "Leave the current game.");

            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            _console.LineWritten += delegate(object sender, EventArgs e)
            {
                Console.WriteLine(((LineWrittenEventArgs)e).Message);
            };
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(typeof(SpriteBatch), _spriteBatch);

            _console.SpriteBatch = _spriteBatch;
            _console.Font = Content.Load<SpriteFont>("Fonts/ConsoleFont");

            _console.WriteLine("Game Console. Type 'help' for available commands.");

            // Set up audio stuff.

            _audioEngine = new AudioEngine("data/Audio/SpaceAudio.xgs");
            _waveBank = new WaveBank(_audioEngine, "data/Audio/Wave Bank.xwb");
            _soundBank = new SoundBank(_audioEngine, "data/Audio/Sound Bank.xsb");

            _audioEngine.Update();

            Services.AddService(typeof(SoundBank), _soundBank);
        }

        #endregion

        #region Logic

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _audioEngine.Update();

            foreach (var component in _componentsToDispose)
            {
                if (component is IDisposable)
                {
                    ((IDisposable)component).Dispose();
                }
            }
            _componentsToDispose.Clear();
        }

        #endregion

        #region Server / Client

        /// <summary>
        /// Starts or restarts the game client.
        /// </summary>
        /// <param name="local">Whether to join locally, or not.</param>
        public void RestartClient(bool local = false)
        {
            DisposeClient();
            if (local)
            {
                Client = new GameClient(this, Server);
            }
            else
            {
                Client = new GameClient(this);
            }
            Components.Add(Client);
        }

        /// <summary>
        /// Starts or restarts the game server.
        /// </summary>
        public void RestartServer()
        {
            DisposeServer();
            Server = new GameServer(this);
            Components.Add(Server);
        }

        /// <summary>
        /// Kills the game client.
        /// </summary>
        public void DisposeClient()
        {
            if (Client != null)
            {
                Components.Remove(Client);
            }
            Client = null;
        }

        /// <summary>
        /// Kills the game server.
        /// </summary>
        public void DisposeServer()
        {
            if (Server != null)
            {
                Components.Remove(Server);
            }
            Server = null;
        }

        /// <summary>
        /// Kills the server and the client.
        /// </summary>
        public void DisposeControllers()
        {
            DisposeClient();
            DisposeServer();
        }

        #endregion

#if DEBUG

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            _fps.Put(1 / gameTime.ElapsedGameTime.TotalSeconds);

            _spriteBatch.Begin();

            string info = String.Format("FPS: {0:f}", System.Math.Ceiling(_fps.Mean()));
            var infoPosition = new Vector2(GraphicsDevice.Viewport.Width - 10 - _console.Font.MeasureString(info).X, 10);

            _spriteBatch.DrawString(_console.Font, info, infoPosition, Color.White);

            _spriteBatch.End();

            foreach (var component in Components)
            {
                if (component is GameClient)
                {
                    var client = (GameClient)component;

                    var session = client.Controller.Session;
                    var entityManager = client.Controller.Simulation.EntityManager;
                    var systemManager = entityManager.SystemManager;

                    StringBuilder sb = new System.Text.StringBuilder();

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 230, GraphicsDevice.Viewport.Height - 140);
                    var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 360,
                                                    GraphicsDevice.Viewport.Height - 140);

                    SessionInfo.Draw("Client", session, sessionOffset, _console.Font, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _console.Font, _spriteBatch);

                    // Draw planet arrows and stuff.
                    if (session.ConnectionState == Engine.Session.ClientState.Connected)
                    {
                        var avatar = systemManager.GetSystem<Engine.ComponentSystem.Systems.AvatarSystem>().GetAvatar(session.LocalPlayer.Number);
                        if (avatar != null)
                        {
                            _spriteBatch.Begin();

                            var x = avatar.GetComponent<Engine.ComponentSystem.Components.Transform>().Translation.X;
                            var y = avatar.GetComponent<Engine.ComponentSystem.Components.Transform>().Translation.Y;
                            var cellX = ((int)x) >> Space.ComponentSystem.Systems.CellSystem.CellSizeShiftAmount;
                            var cellY = ((int)y) >> Space.ComponentSystem.Systems.CellSystem.CellSizeShiftAmount;
                            sb.AppendFormat("Position: ({0:f}, {1:f}), Cell: ({2}, {3})\n", x, y, cellX, cellY);

                            var id = CoordinateIds.Combine(cellX, cellY);

                            var universe = systemManager.GetSystem<Space.ComponentSystem.Systems.UniversalSystem>();
                            if (universe != null)
                            {
                                sb.AppendFormat("Objects in system: {0}\n", universe.GetSystemList(id).Count);
                            }

                            sb.AppendFormat("Update load: {0:f}\n", client.Controller.CurrentLoad);

                            var index = systemManager.GetSystem<Engine.ComponentSystem.Systems.IndexSystem>();
                            if (index != null)
                            {
                                sb.AppendFormat("Indexes: {0}, Total entries: {1}\n", index.DEBUG_NumIndexes, index.DEBUG_Count);
                            }

                            var health = avatar.GetComponent<Space.ComponentSystem.Components.Health>();
                            var energy = avatar.GetComponent<Space.ComponentSystem.Components.Energy>();
                            if (health != null && energy != null)
                            {
                                sb.AppendFormat("Health: {0:f}, Energy: {1:f}\n", health.Value, energy.Value);
                            }

                            _spriteBatch.DrawString(_console.Font, sb.ToString(), new Vector2(20, 20), Color.White);

                            _spriteBatch.End();
                        }
                    }
                }
                else if (component is GameServer)
                {
                    var server = (GameServer)component;
                    var session = server.Controller.Session;

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(150, GraphicsDevice.Viewport.Height - 140);
                    var sessionOffset = new Vector2(10, GraphicsDevice.Viewport.Height - 140);

                    SessionInfo.Draw("Server", session, sessionOffset, _console.Font, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _console.Font, _spriteBatch);
                }
            }
        }

#endif
    }
}
