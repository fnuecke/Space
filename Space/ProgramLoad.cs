﻿using System;
using System.Linq;
using Awesomium.ScreenManagement;
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
    }
}