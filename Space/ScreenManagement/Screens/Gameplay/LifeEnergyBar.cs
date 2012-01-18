using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;
using Engine.Util;

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

        private ContentManager _content;

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
        /// The value for current number of energy.
        /// </summary>
        private int _maxEnergy = 100;

        /// <summary>
        /// The value for maximum number of energy.
        /// </summary>
        private int _currentEnergy = 40;

        /// <summary>
        /// The position (top-right) of the var.
        /// </summary>
        private Point _position;

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
            _content = content;
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch);

            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            _position = new Point ((viewport.Width - _width) / 2, (viewport.Height - _height) / 2 - 40);
        }

        #endregion

        #region Setter

        /// <summary>
        /// Set all values of the life / energy bar.
        /// </summary>
        /// <param name="currentLife">The new value for the current life value</param>
        /// <param name="maxLife">The new value for the maximum life value</param>
        /// <param name="currentEnergy">The new value for the current energy value</param>
        /// <param name="maxEnergy">The new value for the maximum energy value</param>
        /// <param name="width">The new value for the width of the bar.</param>
        /// <param name="height">The new value for the height of the bar.</param>
        /// <param name="border">The new value for the size of the border of the bar.</param>
        /// <param name="positionX">The new value for the x position of the bar.</param>
        /// <param name="positionY">The new value for the y position of the bar.</param>
        public void SetValues(int currentLife, int maxLife, int currentEnergy, int maxEnergy, int width, int height, int border, int positionX, int positionY)
        {
            _currentLife = currentLife;
            _maxLife = maxLife;
            _currentEnergy = currentEnergy;
            _maxEnergy = maxEnergy;
            _width = width;
            _height = height;
            _border = border;
            _position.X = positionX;
            _position.Y = positionY;
        }

        /// <summary>
        /// Set a new value for the current life.
        /// </summary>
        /// <param name="currentLife">The new value for the current life.</param>
        public void SetCurrentLife(int currentLife)
        {
            _currentLife = currentLife;
        }

        /// <summary>
        /// Set a new value for the maximum life.
        /// </summary>
        /// <param name="maxLife">The new value for the maximum life.</param>
        public void SetMaximumLife(int maxLife)
        {
            _maxLife = maxLife;
        }

        /// <summary>
        /// Set a new value for the current energy.
        /// </summary>
        /// <param name="currentLife">The new value for the current energy.</param>
        public void SetCurrentEnergy(int currentEnergy)
        {
            _currentEnergy = currentEnergy;
        }

        /// <summary>
        /// Set a new value for the maximum life.
        /// </summary>
        /// <param name="maxLife">The new value for the maximum life.</param>
        public void SetMaximumEnergy(int maxEnergy)
        {
            _maxEnergy = maxEnergy;
        }

        /// <summary>
        /// Set a new value for the bar width.
        /// </summary>
        /// <param name="width">The new value for the bar width.</param>
        public void SetWidth(int width)
        {
            _width = width;
        }

        /// <summary>
        /// Set a new value for the bar height.
        /// </summary>
        /// <param name="height">The new value for the bar height.</param>
        public void SetHeight(int height)
        {
            _height = height;
        }

        /// <summary>
        /// Set a new value for the border thickness.
        /// </summary>
        /// <param name="border">The new value for the border thickness.</param>
        public void SetBorderThickness(int border)
        {
            _border = border;
        }

        /// <summary>
        /// Set a new value for the x position.
        /// </summary>
        /// <param name="positionX">The new value for the x position.</param>
        public void SetPositionX(int positionX)
        {
            _position.X = positionX;
        }

        /// <summary>
        /// Set a new value for the x position.
        /// </summary>
        /// <param name="positionY">The new value for the y position.</param>
        public void SetPositionY(int positionY)
        {
            _position.Y = positionY;
        }

        /// <summary>
        /// Set a new value for the position.
        /// </summary>
        /// <param name="position">The new value for the position.</param>
        public void SetPosition(Point position)
        {
            _position = position;
        }



        #endregion

        #region Drawing

        /// <summary>
        /// Render the current life / energy bar with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // display the current and maximum values in debug mode.
            #if DEBUG 
            SpriteFont Font1 = _content.Load<SpriteFont>("Fonts/ConsoleFont");
            _spriteBatch.DrawString(Font1, _currentLife + "/" + _maxLife, new Vector2(_position.X + _width + 3, _position.Y - 2), Color.White);
            _spriteBatch.DrawString(Font1, _currentEnergy + "/" + _maxEnergy, new Vector2(_position.X + _width + 3, _position.Y + _height - _border - 2), Color.White);
            #endif

            //////////////////////////////////////////////////////////////////////////
            /// life bar
            //////////////////////////////////////////////////////////////////////////

            // draw the black background
            _basicForms.FillRectangle(_position.X, _position.Y, _width, _height, Color.Black);
            // draw the gray background
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _border, _width - 2 * _border, _height - 2 * _border, new Color(40, 40, 40));
            // draw the current life value
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _border, (int)((_width - 2 * _border) * (_currentLife * 1.0 / _maxLife)), _height - 2 * _border, new Color(142, 232, 63));

            // draw the standard pattern
            for (int i = 0; i * 1.0 / (_width - 2 * _border) < _currentLife * 1.0 / _maxLife; i += 2)
            {
                _basicForms.FillRectangle(_position.X + _border + i, _position.Y + _border + 2, 1, _height - 2 * _border - 2, Color.White * 0.3f);
            }

            // draw the first separation
            for (int i = Ranges[0]; i < _currentLife; i += Ranges[0])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_position.X + _border + pos, _position.Y + _border + 2, 1, _height - 2 * _border - 2 * 2, Color.Black * 0.25f);
            }

            // draw the second separation
            for (int i = Ranges[1]; i <= _currentLife; i += Ranges[1])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_position.X + _border + pos, _position.Y + _border + 1, 1, _height - 2 * _border - 2 * 1, Color.Black);
            }

            // draw the third separation
            for (int i = Ranges[2]; i <= _currentLife; i += Ranges[2])
            {
                int pos = (int)((i * 1.0) / _maxLife * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_position.X + _border + pos, _position.Y, 2, _height, Color.Black);
            }

            //////////////////////////////////////////////////////////////////////////
            /// energy bar
            //////////////////////////////////////////////////////////////////////////

            // draw the black background
            _basicForms.FillRectangle(_position.X, _position.Y + _height - _border, _width, _height, Color.Black);
            // draw the gray background
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _height, _width - 2 * _border, _height - 2 * _border, new Color(40, 40, 40));
            // draw the energy background
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _height, (int)((_width - 2 * _border) * (_currentEnergy * 1.0 / _maxEnergy)), _height - 2 * _border, Color.Blue);

            // draw the standard pattern
            for (int i = 0; i * 1.0 / (_width - 2 * _border) < _currentEnergy * 1.0 / _maxEnergy; i += 2)
            {
                _basicForms.FillRectangle(_position.X + _border + i, _position.Y + _height, 1, _height - 2 * _border - 2, Color.White * 0.3f);
            }

            _spriteBatch.End();
        }



        #endregion

    }
}
