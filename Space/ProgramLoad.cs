using System;
using System.Linq;
using Awesomium.ScreenManagement;
using Engine.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;
using Space.Session;
using Space.Simulation.Commands;
using Space.Util;
using Space.View;

namespace Space
{
    /// <summary>
    /// Loading of all globally used assets.
    /// </summary>
    partial class Program
    {
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

            // Set up graphs.
            LoadGraphs();
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
        }

        /// <summary>
        /// Load graphs we use to render some game statistics (debug stuff).
        /// </summary>
        private void LoadGraphs()
        {
            _fpsGraph = new Graph(Content, GraphicsDevice)
                        {
                            FixedMaximum = 70,
                            Smoothing = 30,
                            Unit = "Hz",
                            Title = "FPS",
                            Data = new[] {_fpsHistory}
                        };

            _updateGraph = new Graph(Content, GraphicsDevice)
                           {
                               Smoothing = 30,
                               FixedMaximum = 30,
                               Unit = "ms",
                               Title = "Update time",
                               Data = new[] {_updateHistory}
                           };

            _memoryGraph = new Graph(Content, GraphicsDevice)
                           {
                               Smoothing = 10,
                               Unit = "B",
                               Title = "RAM",
                               UnitPrefix = Graph.UnitPrefixes.IEC,
                               Data = new[] {_memoryHistory}
                           };

            _componentGraph = new Graph(Content, GraphicsDevice)
                              {
                                  Title = "Components",
                                  Smoothing = 10,
                                  Data = new[] {_componentsHistory}
                              };

            _indexQueryGraph = new Graph(Content, GraphicsDevice)
                               {
                                   Title = "Index queries",
                                   Smoothing = 10,
                                   Data = new[] {_indexQueryHistory}
                               };

            _gameSpeedGraph = new Graph(Content, GraphicsDevice)
                              {
                                  Title = "Game speed",
                                  Smoothing = 10,
                                  Data = new[] {_gameSpeedHistory}
                              };

            _gameLoadGraph = new Graph(Content, GraphicsDevice)
                             {
                                 Title = "Game load",
                                 Smoothing = 10,
                                 Data = new[] {_gameLoadHistory}
                             };

            // Distribute them over the screen.
            Microsoft.Xna.Framework.Rectangle bounds;
            bounds.X = 50;
            bounds.Y = 50;
            bounds.Width = 250;
            bounds.Height = 150;
            foreach (var graph in new[]
                                  {
                                      _fpsGraph,
                                      _updateGraph,
                                      _memoryGraph,
                                      _componentGraph,
                                      _indexQueryGraph,
                                      _gameSpeedGraph,
                                      _gameLoadGraph
                                  })
            {
                if (bounds.X + bounds.Width >= GraphicsDevice.Viewport.Width)
                {
                    bounds.Y += bounds.Height + 10;
                    bounds.X = 50;
                }
                graph.Bounds = bounds;
                bounds.X += bounds.Width + 10;
            }
        }
    }
}
