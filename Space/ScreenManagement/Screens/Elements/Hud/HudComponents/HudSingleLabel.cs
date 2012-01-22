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
using Space.ScreenManagement.Screens.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudSingleLabel : IHudElement
    {

        #region Constants

        /// <summary>
        /// The standard value for the width;
        /// </summary>
        private const int StandardWidth = 150;

        /// <summary>
        /// The standard value for the height;
        /// </summary>
        private const int StandardHeight = 17;

        #endregion

        #region Fields

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

        /// <summary>
        /// Holds the font information.
        /// </summary>
        private SpriteFont _font;

        /// <summary>
        /// The position.
        /// </summary>
        private Point _position;

        private int _height;

        #endregion

        #region Properties

        /// <summary>
        /// The width of the element.
        /// </summary>
        public int Width { get; set; }

        #endregion

        #region Getter / Setter

        public void SetPosition(Point newPosition)
        {
            _position = newPosition;
        }

        public Point GetPosition()
        {
            return _position;
        }

        public int GetHeight()
        {
            return _height;
        }

        public void SetHeight(int height)
        {
            _height = height;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudSingleLabel(GameClient client)
        {
            _client = client;

            // set the standard values into the field.
            Width = StandardWidth;
            _height = StandardHeight;
            _position = new Point(0, 0);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch, _client);

            // load the font that is used for the labels
            _font = _content.Load<SpriteFont>("Fonts/strasua_11");
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD labels with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // draw the first rectangle
            _basicForms.GradientRectangle(
                _position.X,
                _position.Y,
                Width,
                _height,
                new[] { HudColors.GreenDarkGradientLight * 0.85f, HudColors.GreenDarkGradientDark * 0.95f },
                new[] { 0.2f, 0.8f });

            // draw the title string
            String TextLabelLeft = "Lalala";
            _spriteBatch.DrawString(_font, TextLabelLeft, new Vector2(_position.X + 5, _position.Y + 2), HudColors.FontDark);

            _spriteBatch.End();
        }

        #endregion

    }
}
