using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// The health- and energy bar is (usually) displayed above the element of which the bar displays the values.
    /// 
    /// The green health bar displays the current, percental health value of the element. The more health points the
    /// element get, the more small sections are visible to show the user how many health points he has. If he
    /// has too many health points so that they are not able to displayed wisely some of the sections will be
    /// removed.
    /// 
    /// Furthermore the health bar will be colored from green over yellow to red if the element has low health.
    /// 
    /// The blue energy bar is similar to the health bar, but is has no sections and no changing colors.
    /// </summary>
    class HealthEnergyBar
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

        /// <summary>
        /// The standard color (green) for the health bar.
        /// </summary>
        private Color _colorHealthGreen = new Color(142, 232, 63);

        /// <summary>
        /// The yellow color for the health bar.
        /// </summary>
        private Color _colorHealthYellow = new Color(240, 255, 0);

        /// <summary>
        /// The red color for the health bar.
        /// </summary>
        private Color _colorHealthRed = new Color(255, 0, 0);

        /// <summary>
        /// The color for the energy bar.
        /// </summary>
        private Color _colorEnergy = Color.Blue;

        #endregion

        #region Fields

        /// <summary>
        /// The current content manager.
        /// </summary>
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
        /// The value for current number of health.
        /// </summary>
        private int _maxHealth = 100;

        /// <summary>
        /// The value for maximum number of health.
        /// </summary>
        private int _currentHealth = 10;

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

        public HealthEnergyBar(GameClient client)
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
            _basicForms = new BasicForms(_spriteBatch, _client);

            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            _position = new Point ((viewport.Width - _width) / 2, (viewport.Height - _height) / 2 - 40);
        }

        #endregion

        #region Getter & Setter

        /// <summary>
        /// Set all values of the health / energy bar.
        /// </summary>
        /// <param name="currentHealth">The new value for the current health value</param>
        /// <param name="maxHealth">The new value for the maximum health value</param>
        /// <param name="currentEnergy">The new value for the current energy value</param>
        /// <param name="maxEnergy">The new value for the maximum energy value</param>
        /// <param name="width">The new value for the width of the bar.</param>
        /// <param name="height">The new value for the height of the bar.</param>
        /// <param name="border">The new value for the size of the border of the bar.</param>
        /// <param name="positionX">The new value for the x position of the bar.</param>
        /// <param name="positionY">The new value for the y position of the bar.</param>
        public void SetValues(int currentHealth, int maxHealth, int currentEnergy, int maxEnergy, int width, int height, int border, int positionX, int positionY)
        {
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
            _currentEnergy = currentEnergy;
            _maxEnergy = maxEnergy;
            _width = width;
            _height = height;
            _border = border;
            _position.X = positionX;
            _position.Y = positionY;
        }

        /// <summary>
        /// Set a new value for the current health.
        /// </summary>
        /// <param name="currentHealth">The new value for the current health.</param>
        public void SetCurrentHealth(int currentHealth)
        {
            _currentHealth = currentHealth;
        }

        /// <summary>
        /// Set a new value for the maximum health.
        /// </summary>
        /// <param name="maxHealth">The new value for the maximum health.</param>
        public void SetMaximumHealth(int maxHealth)
        {
            _maxHealth = maxHealth;
        }

        /// <summary>
        /// Set a new value for the current energy.
        /// </summary>
        /// <param name="currentEnergy">The new value for the current energy.</param>
        public void SetCurrentEnergy(int currentEnergy)
        {
            _currentEnergy = currentEnergy;
        }

        /// <summary>
        /// Set a new value for the maximum energy.
        /// </summary>
        /// <param name="maxEnergy">The new value for the maximum energy.</param>
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

        /// <summary>
        /// Returns the current width of the bar.
        /// </summary>
        /// <returns>The current width of the bar.</returns>
        public int GetWidth()
        {
            return _width;
        }

        /// <summary>
        /// Returns the current height of the bar.
        /// </summary>
        /// <returns>The current height of the bar.</returns>
        public int GetHeight()
        {
            return _height;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render the current health / energy bar with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // display the current and maximum values in debug mode.
            #if DEBUG 
            SpriteFont Font1 = _content.Load<SpriteFont>("Fonts/ConsoleFont");
            _spriteBatch.DrawString(Font1, _currentHealth + "/" + _maxHealth, new Vector2(_position.X + _width + 3, _position.Y - 2), Color.White);
            _spriteBatch.DrawString(Font1, _currentEnergy + "/" + _maxEnergy, new Vector2(_position.X + _width + 3, _position.Y + _height - _border - 2), Color.White);
            #endif

            //////////////////////////////////////////////////////////////////////////
            /// health bar
            //////////////////////////////////////////////////////////////////////////

            // draw the black background
            _basicForms.FillRectangle(_position.X, _position.Y, _width, _height, Color.Black);
            // draw the gray background
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _border, _width - 2 * _border, _height - 2 * _border, new Color(40, 40, 40));

            // first get the color for the current health bar ...
            Color thisColor = _colorHealthGreen;
            float currentValuePercent = (_currentHealth * 1.0f) / _maxHealth;
            if (currentValuePercent < 0.5f && currentValuePercent >= 0.3f)
            {
                thisColor = Color.Lerp(_colorHealthGreen, _colorHealthYellow, 1-(currentValuePercent - 0.3f) / 0.2f);
            }
            else if (currentValuePercent < 0.3f && currentValuePercent >= 0.1f)
            {
                thisColor = Color.Lerp(_colorHealthYellow, _colorHealthRed, 1 - (currentValuePercent - 0.1f) / 0.2f);
            }
            else if (currentValuePercent < 0.1f)
            {
                thisColor = _colorHealthRed;
            }
            // ... then draw the current health value
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _border, (int)((_width - 2 * _border) * (_currentHealth * 1.0 / _maxHealth)), _height - 2 * _border, thisColor);

            // draw the standard pattern
            for (int i = 0; i * 1.0 / (_width - 2 * _border) < _currentHealth * 1.0 / _maxHealth; i += 2)
            {
                _basicForms.FillRectangle(_position.X + _border + i, _position.Y + _border + 2, 1, _height - 2 * _border - 2, Color.White * 0.3f);
            }

            // draw the first separation
            for (int i = Ranges[0]; i < _currentHealth; i += Ranges[0])
            {
                int pos = (int)((i * 1.0) / _maxHealth * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_position.X + _border + pos, _position.Y + _border + 2, 1, _height - 2 * _border - 2 * 2, Color.Black * 0.25f);
            }

            // draw the second separation
            for (int i = Ranges[1]; i <= _currentHealth; i += Ranges[1])
            {
                int pos = (int)((i * 1.0) / _maxHealth * (_width - 2 * _border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(_position.X + _border + pos, _position.Y + _border + 1, 1, _height - 2 * _border - 2 * 1, Color.Black);
            }

            // draw the third separation
            for (int i = Ranges[2]; i <= _currentHealth; i += Ranges[2])
            {
                int pos = (int)((i * 1.0) / _maxHealth * (_width - 2 * _border));
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
            _basicForms.FillRectangle(_position.X + _border, _position.Y + _height, (int)((_width - 2 * _border) * (_currentEnergy * 1.0 / _maxEnergy)), _height - 2 * _border, _colorEnergy);

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
