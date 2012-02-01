using System;
using Microsoft.Xna.Framework;
using Space.Control;
using Space.ScreenManagement.Screens.Gameplay;
using Space.Util;
using System.Collections.Generic;
using Space.ScreenManagement.Screens.Ingame.Interfaces;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// This screen implements the game's GUI.
    /// </summary>
    public sealed class IngameScreen : GameScreen
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
        /// The component responsible for post-processing effects.
        /// </summary>
        Postprocessing _postprocessing;

        /// <summary>
        /// Holds all GUI elements that can be displayed. Remember this list
        /// also holds the GUI elements that are invisible.
        /// </summary>
        List<AGuiElement> _elements;

        /// <summary>
        /// Holds the background.
        /// </summary>
        Background _background;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public IngameScreen(GameClient client)
        {
            _client = client;
            _input = new InputHandler(client, this);

            _elements = new List<AGuiElement>();
            _elements.Add(new Orbits(client));
            _elements.Add(new Radar(client));

            _background = new Background(client);

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            var game = ScreenManager.Game;

            _background.LoadContent(ScreenManager.SpriteBatch, game.Content);

            // loop the list of elements and load all of them
            foreach (AGuiElement e in _elements)
            {
                e.LoadContent(ScreenManager.SpriteBatch, game.Content);
            }

            _postprocessing = new Postprocessing(game);
            ScreenManager.Game.Components.Add(_postprocessing);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            ScreenManager.Game.Components.Remove(_postprocessing);

            if (_postprocessing != null)
            {
                _postprocessing.Dispose();
            }
        }

        #endregion

        #region Getter

        /// <summary>
        /// Returns the list holding all GUI elements.
        /// </summary>
        /// <returns>The list holding all GUI elements.</returns>
        public List<AGuiElement> GetGuiElements()
        {
            return _elements;
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

            // draw the background first
            _background.Draw();

            // loop the list of elements and load all of them
            foreach (AGuiElement e in _elements)
            {
                e.Draw();
            }

            // Draw world elements.
            _client.Controller.Draw(gameTime);

            // Finish up post processing, GUI should not be affected by it.
            if (Settings.Instance.PostProcessing)
            {
                _postprocessing.Draw();
            }

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
