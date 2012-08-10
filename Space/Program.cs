using System;
using System.Collections.Generic;
using System.Diagnostics;
using Awesomium.ScreenManagement;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.Graphics;
using Engine.Math;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Nuclex.Input;
using Space.ComponentSystem.Systems;
using Space.Control;
using Space.Util;
using Space.View;

namespace Space
{
    /// <summary>
    /// Main class, sets up services and basic components.
    /// </summary>
    internal sealed partial class Program : Game
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

        #region Events

        public event EventHandler<ClientInitializedEventArgs> ClientInitialized;

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

        /// <summary>
        /// Whether to draw graphs with debug / game information.
        /// </summary>
        public bool GraphsVisible { get; set; }

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

        private GameServer _server;
        private GameClient _client;

        private readonly Stopwatch _watch = new Stopwatch();

        private readonly FloatSampling _fpsHistory = new FloatSampling(600);
        private Graph _fpsGraph;

        private readonly FloatSampling _updateHistory = new FloatSampling(600);
        private Graph _updateGraph;

        private readonly FloatSampling _drawHistory = new FloatSampling(600);
        private Graph _drawGraph;

        private readonly FloatSampling _memoryHistory = new FloatSampling(600);
        private Graph _memoryGraph;

        private readonly FloatSampling _componentsHistory = new FloatSampling(600);
        private Graph _componentGraph;

        private readonly FloatSampling _indexQueryHistory = new FloatSampling(600);
        private Graph _indexQueryGraph;

        private readonly FloatSampling _gameSpeedHistory = new FloatSampling(600);
        private Graph _gameSpeedGraph;

        private readonly FloatSampling _gameLoadHistory = new FloatSampling(600);
        private Graph _gameLoadGraph;

        #endregion

        #region Constructor

        private Program()
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
                Logger.Info("Saving settings.");
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
            Content = new LocalizedContentManager(Services) {RootDirectory = "data"};
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

        #endregion

        #region Logic

        /// <summary>
        /// Updates whatever needs updating.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Update.</param>
        protected override void Update(GameTime gameTime)
        {
            // For graph data.
            _watch.Restart();

            // Update the rest of the game.
            base.Update(gameTime);

            // Get ingame stats, if a game is running.
            IManager manager = null;
            if (_server != null)
            {
                manager = _server.Controller.Simulation.Manager;
                _gameSpeedHistory.Put(_server.Controller.ActualSpeed);
                _gameLoadHistory.Put(_server.Controller.CurrentLoad);
            }
            else if (_client != null && _client.Controller.Session.ConnectionState == ClientState.Connected)
            {
                manager = _client.Controller.Simulation.Manager;
                _gameSpeedHistory.Put(_client.Controller.ActualSpeed);
                _gameLoadHistory.Put(_client.Controller.CurrentLoad);
            }
            if (manager != null)
            {
                _componentsHistory.Put(manager.ComponentCount);
                var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
                _indexQueryHistory.Put(index.NumQueriesSinceLastUpdate);
            }

            // Update the audio engine if we have one (setting one up can cause
            // issues on some systems, so we have to check, because in case we
            // failed it will be null).
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

            // Grab actual graph data.
            _watch.Stop();
            _updateHistory.Put(_watch.ElapsedMilliseconds);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // For graph data.
            _watch.Restart();

            // Set our custom render target to render everything into an
            // off-screen texture, first.
            GraphicsDevice.SetRenderTarget(_scene);
            GraphicsDevice.Clear(Color.DarkSlateGray);

            // Draw world elements if we're in a game.
            if (_client != null)
            {
                _client.Controller.Draw((float)gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            // Draw other stuff (GUI for example).
            base.Draw(gameTime);

            // Collect graph data.
            _fpsHistory.Put(1 / (float)gameTime.ElapsedGameTime.TotalSeconds);
            _memoryHistory.Put(GC.GetTotalMemory(false));

            // Draw some debug info on top of everything.
            DrawDebugInfo();

            // Reset our graphics device (pop our off-screen render target).
            GraphicsDevice.SetRenderTarget(null);

            // Dump everything we rendered into our buffer to the screen.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _spriteBatch.Draw(_scene, GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _spriteBatch.End();

            // Grab actual graph data.
            _watch.Stop();
            _drawHistory.Put(_watch.ElapsedMilliseconds);

            // Draw graphs after everything else, to avoid filters on the
            // other screen data to affect us.
            if (GraphsVisible)
            {
                _fpsGraph.Draw();
                _updateGraph.Draw();
                _drawGraph.Draw();
                _memoryGraph.Draw();
                _componentGraph.Draw();
                _indexQueryGraph.Draw();
                _gameSpeedGraph.Draw();
                _gameLoadGraph.Draw();
            }
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

            if (ClientInitialized != null)
            {
                ClientInitialized(this, new ClientInitializedEventArgs(_client));
            }
        }

        /// <summary>
        /// Starts or restarts the game server.
        /// </summary>
        /// <param name="purelyLocal">Whether to create a purely local game (single player).</param>
        public void RestartServer(bool purelyLocal = false)
        {
            DisposeServer();
            // Update after screen manager and client to get input commands.
            _server = new GameServer(this, purelyLocal) {UpdateOrder = 50};
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

        #region Debug stuff

        private Engine.Graphics.Rectangle _indexRectangle;
        private ulong _indexGroupMask;
        private SpriteFont _debugFont;

        [Conditional("DEBUG")]
        private void DrawDebugInfo()
        {
            if (_debugFont == null)
            {
                _debugFont = Content.Load<SpriteFont>("Fonts/ConsoleFont");
            }

            if (_client != null)
            {
                var client = _client;
                var session = client.Controller.Session;

                if (GraphsVisible)
                {
                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 240, GraphicsDevice.Viewport.Height - 180);
                    var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 370,
                                                    GraphicsDevice.Viewport.Height - 180);

                    SessionInfo.Draw("Client", session, sessionOffset, _debugFont, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _debugFont, _spriteBatch);
                }

                // Render index.
                if (session.ConnectionState == ClientState.Connected)
                {
                    var manager = client.Controller.Simulation.Manager;
                    var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
                    var camera = (CameraSystem)manager.GetSystem(CameraSystem.TypeId);
                    if (_indexGroupMask > 0)
                    {
                        if (_indexRectangle == null)
                        {
                            _indexRectangle = new Engine.Graphics.Rectangle(Content, GraphicsDevice) { Color = Color.LightGreen * 0.25f, Thickness = 2f };
                        }
                        _indexRectangle.SetTransform(camera.Transform.Matrix);
                        index.DrawIndex(_indexGroupMask, _indexRectangle, camera.Transform.Translation);
                    }
                }
            }

            if (_server != null)
            {
                var server = _server;
                var session = server.Controller.Session;

                if (GraphsVisible)
                {
                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(180, GraphicsDevice.Viewport.Height - 180);
                    var sessionOffset = new Vector2(60, GraphicsDevice.Viewport.Height - 180);

                    SessionInfo.Draw("Server", session, sessionOffset, _debugFont, _spriteBatch);
                    NetGraph.Draw(session.Information, ngOffset, _debugFont, _spriteBatch);
                }
            }
        }

        #endregion
    }
}
