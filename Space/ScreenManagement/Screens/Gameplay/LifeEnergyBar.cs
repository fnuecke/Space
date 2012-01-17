using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Gameplay
{
    class LifeEnergyBar
    {

        #region Constants

        /// <summary>
        /// The values for the sections in the health bar.
        /// </summary>
        private int[] Ranges = { 50, 500, 2500};

        /// <summary>
        /// The minimum gap between two sections in the health bar.
        /// </summary>
        private int MinGap = 3;

        #endregion

        #region Fields

        /// <summary>
        /// The width of the bar in pixel.
        /// </summary>
        private int _width = 106;

        /// <summary>
        /// The height of the bar in pixel.
        /// </summary>
        private int _height = 11;

        /// <summary>
        /// The outer border of the bar in pixel.
        /// </summary>
        private int _border = 1;

        /// <summary>
        /// The value for current number of life.
        /// </summary>
        private int _maxLife = 100;

        /// <summary>
        /// The value for maximum number of life.
        /// </summary>
        private int _currentLife = 100;

        /// <summary>
        /// The value for maximum number of life.
        /// </summary>
        private int _positionX;

        /// <summary>
        /// The value for maximum number of life.
        /// </summary>
        private int _positionY;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Helper class for drawing basic forms.
        /// </summary>
        private BasicForms _basicForms;

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        #endregion

        #region Constructor

        public LifeEnergyBar(GameClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch);

            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            _positionX = (viewport.Width - _width) / 2;
            _positionY = (viewport.Height - _height) / 2 - 40;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render the current life / energy bar with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // draw the black background
            _basicForms.FillRectangle(_positionX, _positionY, _width, _height, Color.Black);
            // draw the gray background
            _basicForms.FillRectangle(_positionX + _border, _positionY + _border, _width - 2 * _border, _height - 2 * _border, new Color(40, 40, 40));
            // draw the current life value
            _basicForms.FillRectangle(_positionX + _border, _positionY + _border, (int)((_width - 2 * _border) * (_currentLife * 1.0 / _maxLife)), _height - 2 * _border, new Color(142, 232, 63));

            // draw the standard pattern
            for (int i = 1; i * 1.0 / (_width - 2 * _border) < _currentLife * 1.0 / _maxLife; i += 2)
            {
                _basicForms.FillRectangle(_positionX + _border + i, _positionY + _border + 2, 1, _height - 2 * _border - 2, Color.White * 0.3f);
            }

            // draw the first separation
            for (int i = Ranges[0]; i < _currentLife; i += Ranges[0])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_positionX + _border + pos, _positionY + _border + 2, 1, _height - 2 * _border - 2 * 2, Color.Black * 0.25f);
            }

            // draw the second separation
            for (int i = Ranges[1]; i <= _currentLife; i += Ranges[1])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_positionX + _border + pos, _positionY + _border + 1, 1, _height - 2 * _border - 2 * 1, Color.Black);
            }

            // draw the third separation
            for (int i = Ranges[2]; i <= _currentLife; i += Ranges[2])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_positionX + _border + pos, _positionY, 2, _height, Color.Black);
            }

            _spriteBatch.End();
        }

        #endregion

        #region Setter

        /// <summary>
        /// Set all values of the life / energy bar.
        /// </summary>
        /// <param name="currentLife">The new value for the current life value</param>
        /// <param name="maxLife">The new value for the maximum life value</param>
        /// <param name="width">The new value for the width of the bar.</param>
        /// <param name="height">The new value for the height of the bar.</param>
        /// <param name="border">The new value for the size of the border of the bar.</param>
        public void SetValues(int currentLife, int maxLife, int width, int height, int border, int positionX, int positionY)
        {
            _currentLife = currentLife;
            _maxLife = maxLife;
            _width = width;
            _height = height;
            _border = border;
            _positionX = positionX;
            _positionY = positionY;
        }

        /// <summary>
        /// Set new values for the current and maximum life value.
        /// The other values (width, height, border, position) won't be changed.
        /// Only use this method if the bar is not used by multiple elements.
        /// </summary>
        /// <param name="currentLife">The new value for the current life value</param>
        /// <param name="maxLife">The new value for the maximum life value</param>
        public void SetValues(int currentLife, int maxLife)
        {
            _currentLife = currentLife;
            _maxLife = maxLife;
        }

        /// <summary>
        /// Set new values for the current and maximum life and the position value.
        /// The other values (width, height, border) won't be changed.
        /// Only use this method if the bar is not used by multiple elements.
        /// </summary>
        /// <param name="currentLife">The new value for the current life value</param>
        /// <param name="maxLife">The new value for the maximum life value</param>
        public void SetValues(int currentLife, int maxLife, int positionX, int positionY)
        {
            _currentLife = currentLife;
            _maxLife = maxLife;
            _positionX = positionX;
            _positionY = positionY;
        }

        #endregion
    }
}
