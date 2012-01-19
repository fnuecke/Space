using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.ScreenManagement.Screens.Helper;
using Microsoft.Xna.Framework;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudHeader
    {

        private const int StandardWidth = 301;
        private const int StandardHeight = 19;
        private const int StandardWidthGap = 2;
        private const int StandardWidth2ndElement = 85;
        private const int StandardBorderSide = 1;


        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        /// <summary>
        /// The current content manager.
        /// </summary>
        private ContentManager _content;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Helper class for drawing basic forms.
        /// </summary>
        private BasicForms _basicForms;

        private int _width;
        private int _height;
        private int _widthGap;
        private int _width2ndElement;
        private int _borderSide;

        private Point _position;
        private SpriteFont font;


        public HudHeader(GameClient client)
        {
            _client = client;

            _width = StandardWidth;
            _height = StandardHeight;
            _width2ndElement = StandardWidth2ndElement;
            _widthGap = StandardWidthGap;
            _borderSide = StandardBorderSide;
            _position = new Point(300, 300);
        }

        #region Getter / Setter

        public int getHeight()
        {
            return _height;
        }

        public int getWidth()
        {
            return _width;
        }

        public void setHeight(int height)
        {
            _height = height;
        }

        public void setWidth(int width)
        {
            _width = width;
        }

        #endregion

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch);

            font = _content.Load<SpriteFont>("Fonts/strasua_16");
        }

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // draw the first rectangle
            _basicForms.FillRectangle(
                _position.X + _borderSide,
                _position.Y,
                _width - 2 * _borderSide - _widthGap - _width2ndElement,
                _height,
                Color.Green);

            // draw the second rectangle
            _basicForms.FillRectangle(
                _position.X + _width - _borderSide - _width2ndElement,
                _position.Y,
                _width2ndElement,
                _height,
                Color.Green);

            // draw the title string
            _spriteBatch.DrawString(font, "<title>", new Vector2(_position.X + _borderSide + 4, _position.Y + 2), HudColors.FontDark);

            _spriteBatch.End();

        }
    }
}
