using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Awesomium.Core;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.ComponentSystem.Factories;
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
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        /// <summary>
        /// The game server currently running in this program.
        /// </summary>
        public GameServer Server { get; private set; }

        /// <summary>
        /// The game client currently running in this program.
        /// </summary>
        public GameClient Client { get; private set; }

        /// <summary>
        /// The render target that is pushed at the beginning of each draw
        /// cycle.
        /// </summary>
        public Texture2D SceneTarget { get { return _scene; } }

        #endregion

        #region Fields

        private readonly List<IGameComponent> _componentsToDispose = new List<IGameComponent>();

        /// <summary>
        /// The input manager used throughout this game.
        /// </summary>
        private readonly InputManager _inputManager;

        private RenderTarget2D _scene;
        private SpriteBatch _spriteBatch;
        private Awesomium.ScreenManagement.ScreenManager _screenManager;
        private readonly GameConsole _console;
        private readonly GameConsoleTarget _consoleLoggerTarget;

        private AudioEngine _audioEngine;
        private WaveBank _waveBank;
        private SoundBank _soundBank;

        private InputHandler _input;
        private Background _background;
        private Radar _radar;
        private Orbits _orbits;

        private readonly DoubleSampling _fps = new DoubleSampling(30);

        #endregion

        #region Constructor

        public Spaaace()
        {
            Logger.Info("Starting up program...");

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
                                        PreferredBackBufferWidth = Settings.Instance.ScreenWidth,
                                        PreferredBackBufferHeight = Settings.Instance.ScreenHeight,
                                        IsFullScreen = Settings.Instance.Fullscreen,
                                        SynchronizeWithVerticalRetrace = true
                                    };


            // XNAs fixed time step implementation doesn't suit us, to be gentle.
            // So we let it be dynamic and adjust for it as necessary, leading
            // to almost no desyncs at all! Yay!
            IsFixedTimeStep = false;

            // Create our own, localized content manager.
            Content = new LocalizedContentManager(Services)
                      {
                          RootDirectory = "data"
                      };

            // Remember to keep this in sync with the content project.

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

            Components.ComponentRemoved += (sender, e) => _componentsToDispose.Add(e.GameComponent);

            // Initialize input.
            _inputManager = new InputManager(Services, Window.Handle)
                            {
                                UpdateOrder = 0
                            };
            Components.Add(_inputManager);

            // Get our input devices.
            foreach (var keyboard in _inputManager.Keyboards)
            {
                if (keyboard.IsAttached)
                {
                    Services.AddService(typeof(IKeyboard), keyboard);
                    break;
                }
            }
            foreach (var mouse in _inputManager.Mice)
            {
                if (mouse.IsAttached)
                {
                    Services.AddService(typeof(IMouse), mouse);
                    break;
                }
            }
            foreach (var gamepad in _inputManager.GamePads)
            {
                if (gamepad.IsAttached)
                {
                    Services.AddService(typeof(IGamePad), gamepad);
                    break;
                }
            }

            // Add some more utility components.
            _console = new GameConsole(this);
            Components.Add(_console);

            // Add a logging target that'll write to our console.
            _consoleLoggerTarget = new GameConsoleTarget(this, NLog.LogLevel.Debug);

            // More console setup. Only one console key is supported.
            _console.Hotkey = Settings.Instance.MenuBindings.First(binding => binding.Value == Settings.MenuCommand.Console).Key;

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
                Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), 7777), Settings.Instance.PlayerName, (Profile)Settings.Instance.CurrentProfile);
            },
                "Joins a game at the given host.",
                "connect <host> - join the host with the given host name or IP.");
            _console.AddCommand("leave", args =>
            {
                DisposeClient();
            },
                "Leave the current game.");

            #region Debug commands
            #if DEBUG

            // Default handler to interpret everything that is not a command
            // as a script.
            _console.SetDefaultCommandHandler(command =>
            {
                Client.Controller.PushLocalCommand(new ScriptCommand(command));
            });

            _console.AddCommand("d_renderindex", args =>
            {
                int index = int.Parse(args[1]);
                if (index > 64)
                {
                    _console.WriteLine("Invalid index, must be smaller or equal to 64.");
                }
                else
                {
                    _indexGroup = index;
                }
            },
                "Enables rendering of the index with the given index.",
                "d_renderindex <index> - render the cells of the specified index.");

            #endif
            #endregion

            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            _console.LineWritten += (sender, e) => Console.WriteLine(((LineWrittenEventArgs)e).Message);
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

            // Set up audio stuff.
            try
            {
                _audioEngine = new AudioEngine("data/Audio/SpaceAudio.xgs");
                _waveBank = new WaveBank(_audioEngine, "data/Audio/Wave Bank.xwb");
                _soundBank = new SoundBank(_audioEngine, "data/Audio/Sound Bank.xsb");

                _audioEngine.Update();

                Services.AddService(typeof(SoundBank), _soundBank);
            }
            catch (InvalidOperationException ex)
            {
                Logger.ErrorException("Failed initializing AudioEngine.", ex);
            }

            // Set up the render target into which we'll draw everything (to
            // allow switching to and from it for certain effects).
            var pp = GraphicsDevice.PresentationParameters;
            int width = pp.BackBufferWidth;
            int height = pp.BackBufferHeight;
            var format = pp.BackBufferFormat;
            _scene = new RenderTarget2D(GraphicsDevice, width, height, false, format, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            _screenManager = new Awesomium.ScreenManagement.ScreenManager(this, _spriteBatch, _inputManager);
            _screenManager.AddCallback("Space", "host", JSHost);
            _screenManager.PushScreen("Screens/MainMenu");
            Components.Add(_screenManager);

            _input = new InputHandler();
            _background = new Background(this, _spriteBatch);
            _orbits = new Orbits(this, _spriteBatch);
            _radar = new Radar(this, _spriteBatch);
        }

        private void JSHost(object sender, JSCallbackEventArgs e)
        {
            RestartServer();
            RestartClient(true);
        }

        #endregion

        #region Logic

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _input.Update();

            if (_audioEngine != null)
            {
                _audioEngine.Update();
            }

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
            // Update after screen manager (input) but before server (logic).
            Client.UpdateOrder = 25;
            Components.Add(Client);

            _input.Client = Client;
            _background.Client = Client;
            _radar.Client = Client;
            _orbits.Client = Client;
        }

        /// <summary>
        /// Starts or restarts the game server.
        /// </summary>
        public void RestartServer()
        {
            DisposeServer();
            Server = new GameServer(this);
            // Update after screen manager and client to get input commands.
            Server.UpdateOrder = 50;
            Components.Add(Server);
        }

        /// <summary>
        /// Kills the game client.
        /// </summary>
        public void DisposeClient()
        {
            if (Client != null)
            {
                Client.Controller.Session.Leave();
                Components.Remove(Client);
            }
            Client = null;

            _input.Client = null;
            _background.Client = null;
            _radar.Client = null;
            _orbits.Client = null;
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
            _background.Draw();

            // Draw the orbit lines behind ingame objects.
            _orbits.Draw();

            // Draw world elements if we're in a game.
            if (Client != null && Client.Controller.Session.ConnectionState == ClientState.Connected)
            {
                Client.Controller.Draw(gameTime);
            }

            // Draw radar in foreground.
            _radar.Draw();

            // Draw other stuff (GUI for example).
            base.Draw(gameTime);

            // Draw some debug info on top of everything.
            DrawDebugInfo(gameTime);

            // Reset our graphics device (pop our off-screen render target).
            GraphicsDevice.SetRenderTarget(null);

            // Dump everything we rendered into our buffer to the screen.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _spriteBatch.Draw(_scene, GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _spriteBatch.End();
        }

#if DEBUG
        private Engine.Graphics.Rectangle _indexRectangle;
        private int _indexGroup = -1;
#endif

        private void DrawDebugInfo(GameTime gameTime)
        {
#if DEBUG
            if (_indexRectangle == null)
            {
                _indexRectangle = new Engine.Graphics.Rectangle(this);
                _indexRectangle.SetColor(Color.LightGreen * 0.25f);
            }

            _fps.Put(1 / gameTime.ElapsedGameTime.TotalSeconds);

            _spriteBatch.Begin();

            string fps = String.Format("FPS: {0:f}", System.Math.Ceiling(_fps.Mean()));
            var infoPosition = new Vector2(GraphicsDevice.Viewport.Width - 10 - _console.Font.MeasureString(fps).X, 10);

            _spriteBatch.DrawString(_console.Font, fps, infoPosition, Color.White);

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

                    SessionInfo.Draw("Client", session, sessionOffset, _console.Font, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _console.Font, _spriteBatch);

                    // Draw planet arrows and stuff.
                    if (session.ConnectionState == Engine.Session.ClientState.Connected)
                    {
                        _spriteBatch.Begin();

                        var position = info.Position;
                        var cellX = ((int)position.X) >> Space.ComponentSystem.Systems.CellSystem.CellSizeShiftAmount;
                        var cellY = ((int)position.Y) >> Space.ComponentSystem.Systems.CellSystem.CellSizeShiftAmount;
                        sb.AppendFormat("Position: ({0:f}, {1:f}), Cell: ({2}, {3})\n", position.X, position.Y, cellX, cellY);

                        sb.AppendFormat("Update load: {0:f}, Speed: {1:f}\n", client.Controller.CurrentLoad, client.Controller.ActualSpeed);

                        var index = manager.GetSystem<Engine.ComponentSystem.Systems.IndexSystem>();
                        var camera = manager.GetSystem<ComponentSystem.Systems.CameraSystem>();
                        if (index != null)
                        {
                            if (_indexGroup >= 0)
                            {
                                index.DEBUG_DrawIndex(1ul << _indexGroup, _indexRectangle, new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f) - camera.CameraPositon);
                            }
                            sb.AppendFormat("Indexes: {0}, Total entries: {1}\n", index.DEBUG_NumIndexes, index.DEBUG_Count);
                        }

                        sb.AppendFormat("Speed: {0:f}/{1:f}, Maximum acceleration: {2:f}\n", info.Speed, info.MaxSpeed, info.MaxAcceleration);
                        sb.AppendFormat("Mass: {0:f}", info.Mass);

                        _spriteBatch.DrawString(_console.Font, sb.ToString(), new Vector2(60, 60), Color.White);

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

                    SessionInfo.Draw("Server", session, sessionOffset, _console.Font, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _console.Font, _spriteBatch);
                }
            }
#endif
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Spaaace())
            {
                game.Run();
            }
        }
    }
}
