using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// The background screen sits behind all the other menu screens.
    /// It draws a background image that remains fixed in place regardless
    /// of whatever transitions the screens on top of it may be doing.
    /// </summary>
    sealed class BackgroundScreen : GameScreen
    {
        #region Fields

        private Texture2D _backgroundTexture;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public BackgroundScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        /// <summary>
        /// Loads graphics content for this screen. The background texture is quite
        /// big, so we use our own local ContentManager to load it. This allows us
        /// to unload before going from the menus into the game itself, wheras if we
        /// used the shared ContentManager provided by the Game class, the content
        /// would remain loaded forever.
        /// </summary>
        public override void LoadContent()
        {
            _backgroundTexture = ScreenManager.Game.Content.Load<Texture2D>("Textures/background");
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the background screen. Unlike most screens, this should not
        /// transition off even if it has been covered by another screen: it is
        /// supposed to be covered, after all! This overload forces the
        /// coveredByOtherScreen parameter to false in order to stop the base
        /// Update method wanting to transition off.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);
        }

        /// <summary>
        /// Draws the background screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

            double heightRatio = viewport.Height / (double)_backgroundTexture.Height;
            double widthRatio = viewport.Width / (double)_backgroundTexture.Width;
            double ratio = System.Math.Max(heightRatio, widthRatio);

            int width = (int)Math.Ceiling(ratio * _backgroundTexture.Width);
            int height = (int)Math.Ceiling(ratio * _backgroundTexture.Height);

            Rectangle fullscreen = new Rectangle((viewport.Width - width) / 2, (viewport.Height - height) / 2, width, height);

            spriteBatch.Begin();
            spriteBatch.Draw(_backgroundTexture, fullscreen,
                             new Color(TransitionAlpha, TransitionAlpha, TransitionAlpha));

            spriteBatch.End();
        }

        #endregion
    }
}
