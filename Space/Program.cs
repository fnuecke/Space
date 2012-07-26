using System;
using System.Collections.Generic;
using System.Diagnostics;
using Awesomium.ScreenManagement;
using Engine.ComponentSystem.Common.Systems;
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

        private GameServer _server;
        private GameClient _client;

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

            // Update the audio engine if we have one (setting one up can cause
            // issues on some systems, so we have to check).
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

            // Draw world elements if we're in a game.
            if (_client != null)
            {
                _client.Controller.Draw();
            }

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

        private readonly DoubleSampling _fps = new DoubleSampling(30);
        private Engine.Graphics.Rectangle _indexRectangle;
        private ulong _indexGroupMask;
        private SpriteFont _debugFont;

        [Conditional("DEBUG")]
        private void DrawDebugInfo(GameTime gameTime)
        {
            if (_indexRectangle == null)
            {
                _indexRectangle = new Engine.Graphics.Rectangle(Content, GraphicsDevice) {Color = Color.LightGreen * 0.25f, Thickness = 2f};
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
                            sb.AppendFormat("Indexes: {0}, Total entries: {1}, Queries: {2}\n", index.NumIndexes, index.Count, index.NumQueriesSinceLastUpdate);
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

        #endregion
    }
}
