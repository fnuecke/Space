using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Gameplay;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// This screen implements the game's GUI.
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        private float _pauseAlpha;

        /// <summary>
        /// The game client that's active for this game.
        /// </summary>
        private GameClient _client;

        /// <summary>
        /// Grab player input when this screen is active.
        /// </summary>
        private InputHandler _input;

        /// <summary>
        /// Renderer for overall background.
        /// </summary>
        private Background _background;

        /// <summary>
        /// Renderer for planet orbits (which is logically a part of the
        /// radar system, but we want it to be behind planets.
        /// </summary>
        private Orbits _orbits;

        /// <summary>
        /// The render target we draw the game world into.
        /// </summary>
        private RenderTarget2D _world;

        /// <summary>
        /// Renderer for radar system.
        /// </summary>
        private Radar _radar;

        /// <summary>
        /// Renderer for the HUD elements.
        /// </summary>
        private Hud _hud;

        /// <summary>
        /// The component responsible for post-processing effects.
        /// </summary>
        Postprocessing _postprocessing;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen(GameClient client)
        {
            _client = client;

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            _input = new InputHandler(client);
            _background = new Background(client);
            _orbits = new Orbits(client);
            _radar = new Radar(client);
            _hud = new Hud(client);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            var game = ScreenManager.Game;

            _background.LoadContent(ScreenManager.SpriteBatch, game.Content);
            _radar.LoadContent(ScreenManager.SpriteBatch, game.Content);
            _hud.LoadContent(ScreenManager.SpriteBatch, game.Content);

            _orbits.LoadContent(ScreenManager.SpriteBatch, game.Content);

            _postprocessing = new Postprocessing(game);
            ScreenManager.Game.Components.Add(_postprocessing);

            PresentationParameters pp = game.GraphicsDevice.PresentationParameters;
            int width = pp.BackBufferWidth;
            int height = pp.BackBufferHeight;

            SurfaceFormat format = pp.BackBufferFormat;

            _world = new RenderTarget2D(game.GraphicsDevice, width, height, false, format, DepthFormat.None);

            // TODO preload any other ingame content we may need? (ship, planet etc textures)

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            ScreenManager.Game.Components.Remove(_postprocessing);

            _world.Dispose();
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Enable or disable our game input depending on whether we're the
            // active screen or not, and whether the game is running or not.
            if (_client.IsRunning())
            {
                _input.SetEnabled(!otherScreenHasFocus);

                // Only update our input if we have focus.
                if (!otherScreenHasFocus)
                {
                    _input.Update();
                }

                // Always update the HUD.
                _hud.Update();
            }
            else
            {
                _input.SetEnabled(false);
            }

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
            {
                _pauseAlpha = Math.Min(_pauseAlpha + 1f / 32, 1);
            }
            else
            {
                _pauseAlpha = Math.Max(_pauseAlpha - 1f / 32, 0);
            }
        }

        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input.KeyPause)
            {
                ScreenManager.AddScreen(new PauseMenuScreen());
            }
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            var graphicsDevice = ScreenManager.Game.GraphicsDevice;

            // Draw overall background (stars).
            _background.Draw();

            // Draw the actual game world into a temporary target, because the
            // draw updates textures for objects, so we need that prior to
            // rendering the orbits -- but the orbits have to go behind the
            // game world objects, so we need to save this to then paint it
            // over the orbits.

            // Save main render targets.
            RenderTargetBinding[] previousRenderTargets = graphicsDevice.GetRenderTargets();

            // Set world render target.
            graphicsDevice.SetRenderTarget(_world);
            graphicsDevice.Clear(Color.Transparent);

            // Draw world elements.
            _client.Controller.Draw(gameTime);

            // Restore render targets.
            graphicsDevice.SetRenderTargets(previousRenderTargets);

            // Render the orbits.
            _orbits.Draw();

            // Paint the world content over the orbits.
            ScreenManager.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            ScreenManager.SpriteBatch.Draw(_world, _world.Bounds, Color.White);
            ScreenManager.SpriteBatch.End();

            // Finish up post processing, GUI should not be affected by it.
            if (Settings.Instance.PostProcessing)
            {
                _postprocessing.Draw();
            }

            // Render the radar.
            _radar.Draw();

            // Render the life- and energy display.
            _hud.Draw();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || _pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, _pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }
        }

        #endregion
    }
}
