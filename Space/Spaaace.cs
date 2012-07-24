using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using Awesomium.ScreenManagement;
using Engine.ComponentSystem.Common.Systems;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.Control;
using Space.Session;
using Space.Simulation.Commands;
using Space.Util;
using Space.View;

namespace Space
{
    /// <summary>
    /// Main class, sets up services and basic components.
    /// </summary>
    public sealed class Spaaace : Game
    {
        #region Program entry

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Spaaace())
            {
                Logger.Info("Starting up program...");
                game.Run();
                Logger.Info("Shutting down program...");
            }
        }

        #endregion

        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// Relative path to the file we store user settings in.
        /// </summary>
        private const string SettingsFile = "config.xml";

        #endregion

        #region Events

        public event EventHandler<ClientInitializedEventArgs> ClientInitialized;

        public event EventHandler<EventArgs> ClientDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The graphics device manager used in this game.
        /// </summary>
        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        /// <summary>
        /// The currently active game client.
        /// </summary>
        public GameClient Client
        {
            get { return _client; }
        }

        /// <summary>
        /// The game console we use.
        /// </summary>
        public IGameConsole GameConsole
        {
            get { return _console; }
        }

        /// <summary>
        /// The input manager in use.
        /// </summary>
        public InputManager InputManager
        {
            get { return _inputManager; }
        }

        /// <summary>
        /// The screen manager used for rendering the GUI.
        /// </summary>
        public ScreenManager ScreenManager
        {
            get { return _screenManager; }
        }

        #endregion

        #region Fields

        private readonly List<IGameComponent> _pendingComponents = new List<IGameComponent>();

        /// <summary>
        /// The input manager used throughout this game.
        /// </summary>
        private InputManager _inputManager;

        private RenderTarget2D _scene;
        private SpriteBatch _spriteBatch;
        private ScreenManager _screenManager;
        private GameConsole _console;
        private GameConsoleTarget _consoleLoggerTarget;

        private AudioEngine _audioEngine;
        private WaveBank _waveBank;
        private SoundBank _soundBank;

        private InputHandler _input;
        private Background _background;
        private Radar _radar;
        private Orbits _orbits;

        private GameServer _server;
        private GameClient _client;

        #endregion

        #region Constructor

        private Spaaace()
        {
            // Some window settings.
            Window.Title = "Space. The Game. Seriously.";
            IsMouseVisible = true;

            // XNAs fixed time step implementation doesn't suit us, to be gentle.
            // So we let it be dynamic and adjust for it as necessary, leading
            // to almost no desyncs at all! Yay!
            IsFixedTimeStep = false;

            // We use this to dispose game components that are disposable and
            // were removed from the list of active components. We don't want
            // to dispose them during an update loop, because they will still
            // be updated if they had not been updated before the removal,
            // leading to object disposed exceptions.
            Components.ComponentRemoved += (sender, e) => _pendingComponents.Add(e.GameComponent);

            // Load settings. Save on exit.
            Settings.Load(SettingsFile);
            Exiting += (sender, e) =>
            {
                Logger.Info("Shutting down program...");
                Settings.Save(SettingsFile);
            };

            // Set up display.
            GraphicsDeviceManager = new GraphicsDeviceManager(this)
                                    {
                                        PreferredBackBufferWidth = Settings.Instance.ScreenResolution.X,
                                        PreferredBackBufferHeight = Settings.Instance.ScreenResolution.Y,
                                        IsFullScreen = Settings.Instance.Fullscreen,
                                        SynchronizeWithVerticalRetrace = true
                                    };

            // Create our own, localized content manager.
            Content = new LocalizedContentManager(Services) { RootDirectory = "data" };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _screenManager.Dispose();
                _console.Dispose();
                _consoleLoggerTarget.Dispose();
                _inputManager.Dispose();

                if (_spriteBatch != null)
                {
                    _spriteBatch.Dispose();
                }
                if (_audioEngine != null)
                {
                    _audioEngine.Dispose();
                }
                if (_waveBank != null)
                {
                    _waveBank.Dispose();
                }
                if (_soundBank != null)
                {
                    _soundBank.Dispose();
                }

                if (_scene != null)
                {
                    _scene.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            // Initialize the console as soon as possible.
            InitializeConsole();

            // Initialize localization. Anything after this loaded via the content
            // manager will be localized.
            InitializeLocalization();

            // Set up input to allow interaction with the game.
            InitializeInput();

            base.Initialize();
        }

        /// <summary>
        /// Initialize the localization by figuring out which to use, either by getting
        /// it from the settings, or by falling back to the default one instead.
        /// </summary>
        private void InitializeLocalization()
        {
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

            // Set up resources.
            GuiStrings.Culture = culture;
            AttributeNames.Culture = culture;
            AttributePrefixes.Culture = culture;
            ItemDescriptions.Culture = culture;
            ItemNames.Culture = culture;
            QualityNames.Culture = culture;

            // Set up content loader.
            ((LocalizedContentManager)Content).Culture = culture;
        }

        /// <summary>
        /// Initialize input logic.
        /// </summary>
        private void InitializeInput()
        {
            // Initialize input.
            _inputManager = new InputManager(Services, Window.Handle);
            Components.Add(_inputManager);
            Services.AddService(typeof(InputManager), _inputManager);

            // Create the input handler that converts input to ingame commands.
            _input = new InputHandler(this);
            Components.Add(_input);
        }

        /// <summary>
        /// Initialize the console, adding commands and making the logger write to it.
        /// </summary>
        private void InitializeConsole()
        {
            // Create the console and add it as a component.
            _console = new GameConsole(this);
            Components.Add(_console);

            // We do this in the input handler.
            _console.Hotkey = Keys.None;

            // Add a logging target that'll write to our console.
            _consoleLoggerTarget = new GameConsoleTarget(this, NLog.LogLevel.Debug);

            _console.AddCommand(new[] { "fullscreen", "fs" },
                args => GraphicsDeviceManager.ToggleFullScreen(),
                "Toggles fullscreen mode.");

            _console.AddCommand("search",
                args => _client.Controller.Session.Search(),
                "Search for games available on the local subnet.");
            _console.AddCommand("connect",
                args => _client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), 7777), Settings.Instance.PlayerName, Settings.Instance.CurrentProfile),
                "Joins a game at the given host.",
                "connect <host> - join the host with the given host name or IP.");
            _console.AddCommand("leave",
                args => DisposeClient(),
                "Leave the current game.");

            // Register debug commands.
            InitializeConsoleForDebug();

            // Say hi.
            _console.WriteLine("Console initialized. Type 'help' for available commands.");
        }

        /// <summary>
        /// Debug commands for the console, that won't be available in release builds.
        /// </summary>
        [Conditional("DEBUG")]
        private void InitializeConsoleForDebug()
        {
            // Default handler to interpret everything that is not a command
            // as a script.
            _console.SetDefaultCommandHandler(command =>
                                              {
                                                  if (_client != null)
                                                  {
                                                      _client.Controller.PushLocalCommand(new ScriptCommand(command));
                                                  }
                                                  else
                                                  {
                                                      _console.WriteLine("Unknown command.");
                                                  }
                                              });

            _console.AddCommand("d_renderindex",
                args =>
                {
                    int index;
                    if (!int.TryParse(args[1], out index))
                    {
                        switch (args[1])
                        {
                            case "c":
                            case "collision":
                            case "collidable":
                            case "collidables":
                                _indexGroupMask = CollisionSystem.IndexGroupMask;
                                break;
                            case "d":
                            case "detector":
                            case "detectable":
                            case "detectables":
                                _indexGroupMask = DetectableSystem.IndexGroupMask;
                                break;
                            case "g":
                            case "grav":
                            case "gravitation":
                                _indexGroupMask = GravitationSystem.IndexGroupMask;
                                break;
                            case "s":
                            case "sound":
                            case "sounds":
                                _indexGroupMask = SoundSystem.IndexGroupMask;
                                break;
                        }
                    }
                    else if (index > 64)
                    {
                        _console.WriteLine("Invalid index, must be smaller or equal to 64.");
                    }
                    else
                    {
                        _indexGroupMask = 1ul << index;
                    }
                },
                "Enables rendering of the index with the given index.",
                "d_renderindex <index> - render the cells of the specified index.");

            _console.AddCommand("d_check_serialization",
                args =>
                {
                    try
                    {
                        ((Engine.Controller.AbstractTssController<IServerSession>)_server.Controller).ValidateSerialization();
                    }
                    catch (InvalidProgramException ex)
                    {
                        _console.WriteLine("Serialization broken, " + ex.Message);
                    }
                },
                "Verifies the simulation's serialization works by creating a",
                "snapshot and deserializing it again, then compares the hash",
                "values of the two simulations.");

            _console.AddCommand("d_check_rollback",
                args =>
                {
                    try
                    {
                        ((Engine.Controller.AbstractTssController<IServerSession>)_server.Controller).ValidateRollback();
                    }
                    catch (InvalidProgramException ex)
                    {
                        _console.WriteLine("Serialization broken, " + ex.Message);
                    }
                },
                "Verifies the simulation's serialization works by creating a",
                "snapshot and deserializing it again, then compares the hash",
                "values of the two simulations.");

            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            _console.LineWritten += (sender, e) => Console.WriteLine(((LineWrittenEventArgs)e).Message);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(typeof(SpriteBatch), _spriteBatch);

            // Tell the console how to render itself.
            LoadConsole();

            // Initialize scripting environment for debugging.
            SpaceCommandHandler.InitializeScriptEnvironment(Content);

            // Load generator constraints.
            FactoryLibrary.Initialize(Content);

            // Create the profile implementation.
            Settings.Instance.CurrentProfile = new Profile();

            // Load / create profile.
            if (Settings.Instance.CurrentProfile.Profiles.Contains(Settings.Instance.CurrentProfileName))
            {
                Settings.Instance.CurrentProfile.Load(Settings.Instance.CurrentProfileName);
            }
            else
            {
                // TODO: create profile selection screen, show it if no or an invalid profile is active.
                Settings.Instance.CurrentProfile.Create("Default", Data.PlayerClassType.Default);
                Settings.Instance.CurrentProfileName = "Default";
                Settings.Instance.CurrentProfile.Save();
            }

            // Set up the render target into which we'll draw everything (to
            // allow switching to and from it for certain effects).
            var pp = GraphicsDevice.PresentationParameters;
            _scene = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // Set up audio data (load wave/sound bank).
            LoadAudio();

            // Set up graphical user interface.
            LoadGui();
        }

        /// <summary>
        /// Tell our console how to render itself.
        /// </summary>
        private void LoadConsole()
        {
            _console.SpriteBatch = _spriteBatch;
            _console.Font = Content.Load<SpriteFont>("Fonts/ConsoleFont");
        }

        /// <summary>
        /// Set up audio by loading the XACT generated files.
        /// </summary>
        private void LoadAudio()
        {
            // Set up audio stuff by loading our XACT project files.
            try
            {
                // Load data.
                _audioEngine = new AudioEngine("data/Audio/SpaceAudio.xgs");
                _waveBank = new WaveBank(_audioEngine, "data/Audio/Wave Bank.xwb");
                _soundBank = new SoundBank(_audioEngine, "data/Audio/Sound Bank.xsb");

                // Do a first update, as recommended in the documentation.
                _audioEngine.Update();

                // Make the sound and wave bank available as a service.
                Services.AddService(typeof(SoundBank), _soundBank);
                Services.AddService(typeof(WaveBank), _waveBank);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed initializing AudioEngine.", ex);
            }
        }

        /// <summary>
        /// Initialize the gui by creating our screen manager an pushing the main
        /// menu screen to it.
        /// </summary>
        private void LoadGui()
        {
            // Create the screen manager.
            _screenManager = new ScreenManager(this, _spriteBatch, _inputManager);
            Components.Add(_screenManager);

            // Initialize our scripting API for the GUI.
            JSCallbacks.Initialize(this);

            // Push the main menu.
            _screenManager.PushScreen("MainMenu/MainMenu");

            // Create ingame graphics stuff.
            // TODO make it so this is rendered inside the simulation (e.g. own render systems)
            _background = new Background(this, _spriteBatch);
            _orbits = new Orbits(this, _spriteBatch);
            _radar = new Radar(this, _spriteBatch);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates whatever needs updating.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Update.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update the rest of the game.
            base.Update(gameTime);

            // Update the audio engine if we have one (setting one up can bug
            // out on some systems).
            if (_audioEngine != null)
            {
                _audioEngine.Update();
            }

            // Post-update disposable of game components that were removed
            // from our the components list.
            foreach (var component in _pendingComponents)
            {
                var disposable = component as IDisposable;
                if (disposable != null)
                {
                    (disposable).Dispose();
                }
            }
            _pendingComponents.Clear();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Set our custom render target to render everything into an
            // off-screen texture, first.
            GraphicsDevice.SetRenderTarget(_scene);
            GraphicsDevice.Clear(Color.DarkSlateGray);

            // Draw the overall space background.
            if (_client != null)
            {
                _background.Scale = _client.GetCameraZoom();
            }
            _background.Draw();

            // Draw the orbit lines behind ingame objects.
            _orbits.Draw();

            // Draw world elements if we're in a game.
            if (_client != null && _client.Controller.Session.ConnectionState == ClientState.Connected)
            {
                _client.Controller.Draw();
            }

            // Draw other stuff (GUI for example).
            base.Draw(gameTime);

            // Draw radar in foreground.
            _radar.Draw();

            // Draw some debug info on top of everything.
#if DEBUG
            DrawDebugInfo(gameTime);
#endif

            // Reset our graphics device (pop our off-screen render target).
            GraphicsDevice.SetRenderTarget(null);

            // Dump everything we rendered into our buffer to the screen.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _spriteBatch.Draw(_scene, GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _spriteBatch.End();
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
            _client = local ? new GameClient(this, _server) : new GameClient(this);
            // Update after screen manager (input) but before server (logic).
            _client.UpdateOrder = 25;
            Components.Add(_client);

            _background.Client = _client;
            _radar.Client = _client;
            _orbits.Client = _client;

            if (ClientInitialized != null)
            {
                ClientInitialized(this, new ClientInitializedEventArgs(_client));
            }
        }

        /// <summary>
        /// Starts or restarts the game server.
        /// </summary>
        public void RestartServer()
        {
            DisposeServer();
            _server = new GameServer(this);
            // Update after screen manager and client to get input commands.
            _server.UpdateOrder = 50;
            Components.Add(_server);
        }

        /// <summary>
        /// Kills the game client.
        /// </summary>
        public void DisposeClient()
        {
            if (_client != null)
            {
                if (_client.Controller.Session.ConnectionState == ClientState.Connected)
                {
                    _client.Controller.Session.Leave();
                }
                Components.Remove(_client);
            }
            _client = null;

            _background.Client = null;
            _radar.Client = null;
            _orbits.Client = null;

            if (ClientDisposed != null)
            {
                ClientDisposed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Kills the game server.
        /// </summary>
        public void DisposeServer()
        {
            if (_server != null)
            {
                Components.Remove(_server);
            }
            _server = null;
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
        private readonly DoubleSampling _fps = new DoubleSampling(30);
        private Engine.Graphics.Rectangle _indexRectangle;
        private ulong _indexGroupMask;
        private SpriteFont _debugFont;

        private void DrawDebugInfo(GameTime gameTime)
        {
            if (_indexRectangle == null)
            {
                _indexRectangle = new Engine.Graphics.Rectangle(this) {Color = Color.LightGreen * 0.25f, Thickness = 2f};
                _debugFont = Content.Load<SpriteFont>("Fonts/ConsoleFont");
            }

            _fps.Put(1 / gameTime.ElapsedGameTime.TotalSeconds);

            _spriteBatch.Begin();

            var fps = String.Format("FPS: {0:f}", Math.Ceiling(_fps.Mean()));
            var infoPosition = new Vector2(GraphicsDevice.Viewport.Width - 10 - _debugFont.MeasureString(fps).X, 10);

            _spriteBatch.DrawString(_debugFont, fps, infoPosition, Color.White);

            _spriteBatch.End();

            foreach (var component in Components)
            {
                if (component is GameClient)
                {
                    var client = (GameClient)component;
                    var info = client.GetPlayerShipInfo();
                    if (info == null)
                    {
                        continue;
                    }

                    var session = client.Controller.Session;
                    var manager = client.Controller.Simulation.Manager;

                    var sb = new System.Text.StringBuilder();

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 240, GraphicsDevice.Viewport.Height - 180);
                    var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 370,
                                                    GraphicsDevice.Viewport.Height - 180);

                    SessionInfo.Draw("Client", session, sessionOffset, _debugFont, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _debugFont, _spriteBatch);

                    // Draw planet arrows and stuff.
                    if (session.ConnectionState == ClientState.Connected)
                    {
                        _spriteBatch.Begin();

                        var position = info.Position;
                        var cellX = ((int)position.X) >> CellSystem.CellSizeShiftAmount;
                        var cellY = ((int)position.Y) >> CellSystem.CellSizeShiftAmount;
                        sb.AppendFormat("Position: ({0:f}, {1:f}), Cell: ({2}, {3})\n", position.X, position.Y, cellX, cellY);

                        sb.AppendFormat("Update load: {0:f}, Speed: {1:f}\n", client.Controller.CurrentLoad, client.Controller.ActualSpeed);

                        var index = manager.GetSystem<IndexSystem>();
                        var camera = manager.GetSystem<CameraSystem>();
                        if (index != null)
                        {
                            if (_indexGroupMask >= 0)
                            {
                                _indexRectangle.Scale = camera.Zoom;
                                var translation = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f) - camera.CameraPositon;
                                index.DrawIndex(_indexGroupMask, _indexRectangle, translation);
                            }
                            sb.AppendFormat("Indexes: {0}, Total entries: {1}, Queries: {2}\n", index.NumIndexes, index.Count, index.NumQueriesLastUpdate);
                        }

                        sb.AppendFormat("Speed: {0:f}/{1:f}, Maximum acceleration: {2:f}\n", info.Speed, info.MaxSpeed, info.MaxAcceleration);
                        sb.AppendFormat("Mass: {0:f}", info.Mass);

                        _spriteBatch.DrawString(_debugFont, sb.ToString(), new Vector2(60, 60), Color.White);

                        _spriteBatch.End();
                    }
                }
                else if (component is GameServer)
                {
                    var server = (GameServer)component;
                    var session = server.Controller.Session;

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(180, GraphicsDevice.Viewport.Height - 180);
                    var sessionOffset = new Vector2(60, GraphicsDevice.Viewport.Height - 180);

                    SessionInfo.Draw("Server", session, sessionOffset, _debugFont, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _debugFont, _spriteBatch);
                }
            }
        }
#endif
    }
}
