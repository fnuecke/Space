#region File Description
//-----------------------------------------------------------------------------
// ScreenManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ScreenManagement
{
    /// <summary>
    /// The screen manager is a component which manages one or more GameScreen
    /// instances. It maintains a stack of screens, calls their Update and Draw
    /// methods at the appropriate times, and automatically routes input to the
    /// topmost active screen.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        #region Properties

        /// <summary>
        /// Texture used for rendering the background.
        /// </summary>
        public Texture2D PixelTexture { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// All the screens we track.
        /// </summary>
        private readonly List<GameScreen> _screens = new List<GameScreen>();

        /// <summary>
        /// The screens we're currently updating.
        /// </summary>
        private readonly List<GameScreen> _screensToUpdate = new List<GameScreen>();

        /// <summary>
        /// Input state used for the menu screens.
        /// </summary>
        private readonly InputState _input;

        private Texture2D _blankTexture;

        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// A default SpriteBatch shared by all the screens. This saves
        /// each screen having to bother creating their own local instance.
        /// </summary>
        public SpriteBatch SpriteBatch { get; private set; }

        /// <summary>
        /// A default font shared by all the screens. This saves
        /// each screen having to bother loading their own local copy.
        /// </summary>
        public SpriteFont Font { get; private set; }

        /// <summary>
        /// If true, the manager prints out a list of all the screens
        /// each time it is updated. This can be useful for making sure
        /// everything is being added and removed at the right times.
        /// </summary>
        public bool TraceEnabled { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructs a new screen manager component.
        /// </summary>
        public ScreenManager(Game game)
            : base(game)
        {
            _input = new InputState(game);
        }

        /// <summary>
        /// Initializes the screen manager component.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            PixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            PixelTexture.SetData(new[] { Color.White });

            _isInitialized = true;
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load content belonging to the screen manager.
            ContentManager content = Game.Content;

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = content.Load<SpriteFont>("Fonts/showcard_gothic");
            _blankTexture = content.Load<Texture2D>("Textures/blank");

            // Tell each of the screens to load their content.
            foreach (GameScreen screen in _screens)
            {
                screen.LoadContent();
            }
        }

        /// <summary>
        /// Unload your graphics content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Tell each of the screens to unload their content.
            foreach (GameScreen screen in _screens)
            {
                screen.UnloadContent();
            }
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows each screen to run logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Make a copy of the master screen list, to avoid confusion if
            // the process of updating one screen adds or removes others.
            _screensToUpdate.Clear();
            _screensToUpdate.AddRange(_screens);

            bool otherScreenHasFocus = !Game.IsActive;
            bool coveredByOtherScreen = false;

            // Loop as long as there are screens waiting to be updated.
            while (_screensToUpdate.Count > 0)
            {
                // Pop the topmost screen off the waiting list.
                GameScreen screen = _screensToUpdate[_screensToUpdate.Count - 1];

                _screensToUpdate.RemoveAt(_screensToUpdate.Count - 1);

                // Update the screen.
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    // If this is the first active screen we came across,
                    // give it a chance to handle input.
                    if (!otherScreenHasFocus)
                    {
                        screen.HandleInput(_input);
                        _input.Update();

                        otherScreenHasFocus = true;
                    }

                    // If this is an active non-popup, inform any subsequent
                    // screens that they are covered by it.
                    if (!screen.IsPopup)
                    {
                        coveredByOtherScreen = true;
                    }
                }
            }

            // Print debug trace?
            if (TraceEnabled)
            {
                TraceScreens();
            }
        }

        /// <summary>
        /// Prints a list of all the screens, for debugging.
        /// </summary>
        void TraceScreens()
        {
            List<string> screenNames = new List<string>();

            foreach (GameScreen screen in _screens)
            {
                screenNames.Add(screen.GetType().Name);
            }

            Debug.WriteLine(string.Join(", ", screenNames.ToArray()));
        }

        /// <summary>
        /// Tells each screen to draw itself.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            foreach (GameScreen screen in _screens)
            {
                if (screen.ScreenState == ScreenState.Hidden)
                {
                    continue;
                }

                screen.Draw(gameTime);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new screen to the screen manager.
        /// </summary>
        public void AddScreen(GameScreen screen)
        {
            screen.ScreenManager = this;
            screen.IsExiting = false;

            // If we have a graphics device, tell the screen to load content.
            if (_isInitialized)
            {
                screen.LoadContent();
            }

            _screens.Add(screen);
        }

        /// <summary>
        /// Removes a screen from the screen manager. You should normally
        /// use GameScreen.ExitScreen instead of calling this directly, so
        /// the screen can gradually transition off rather than just being
        /// instantly removed.
        /// </summary>
        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload content.
            if (_isInitialized)
            {
                screen.UnloadContent();
            }

            _screens.Remove(screen);
            _screensToUpdate.Remove(screen);
        }

        /// <summary>
        /// Expose an array holding all the screens. We return a copy rather
        /// than the real master list, because screens should only ever be added
        /// or removed using the AddScreen and RemoveScreen methods.
        /// </summary>
        public GameScreen[] GetScreens()
        {
            return _screens.ToArray();
        }

        /// <summary>
        /// Helper draws a translucent black fullscreen sprite, used for fading
        /// screens in and out, and for darkening the background behind popups.
        /// </summary>
        public void FadeBackBufferToBlack(float alpha)
        {
            Viewport viewport = GraphicsDevice.Viewport;

            SpriteBatch.Begin();

            SpriteBatch.Draw(_blankTexture,
                             new Rectangle(0, 0, viewport.Width, viewport.Height),
                             Color.Black * alpha);

            SpriteBatch.End();
        }

        #endregion
    }
}
