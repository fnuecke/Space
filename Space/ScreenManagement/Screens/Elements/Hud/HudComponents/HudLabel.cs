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
    class HudHeader : IHudChildElement
    {

        #region Constants

        /// <summary>
        /// The standard value for the width;
        /// </summary>
        private const int StandardWidth = 302;

        /// <summary>
        /// The standard value for the height;
        /// </summary>
        private const int StandardHeight = 18;

        /// <summary>
        /// The standard value for the width of the gap between the two boxes;
        /// </summary>
        private const int StandardWidthGap = 2;

        /// <summary>
        /// The standard value for the width of the second element;
        /// </summary>
        private const int StandardWidth2ndElement = 85;

        /// <summary>
        /// The standard value for the size of the border on the side.
        /// </summary>
        private const int StandardBorderSide = 1;

        /// <summary>
        /// The standard value for the status whether the first element should be displayed or not.
        /// </summary>
        private const Boolean StandardDisplay1stElement = true;

        /// <summary>
        /// The standard value for the status whether the second element should be displayed or not.
        /// </summary>
        private const Boolean StandardDisplay2ndElement = true;

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

        #endregion

        #region Properties

        /// <summary>
        /// The width of the element.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the element.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The width of the gap between the two boxes.
        /// </summary>
        public int WidthGap { get; set; }

        /// <summary>
        /// The width of the second box.
        /// </summary>
        public int Width2ndElement { get; set; }

        /// <summary>
        /// The size of the border on the sides.
        /// </summary>
        public int BorderSide { get; set; }

        // Implementation of IHudChildElement interface
        public Point Position { get; set; }

        /// <summary>
        /// Status whether the first element should be displayed or not.
        /// </summary>
        public Boolean Display1stElement { get; set; }

        /// <summary>
        /// Status whether the second element should be displayed or not.
        /// </summary>
        public Boolean Display2ndElement { get; set; }

        public String TextLabelLeft { get; set; }
        public String TextLabelRight { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudHeader(GameClient client)
        {
            _client = client;

            // set the standard values into the field.
            Width = StandardWidth;
            Height = StandardHeight;
            Width2ndElement = StandardWidth2ndElement;
            WidthGap = StandardWidthGap;
            BorderSide = StandardBorderSide;
            Display1stElement = StandardDisplay1stElement;
            Display2ndElement = StandardDisplay2ndElement;
            Position = new Point(0, 0);
            TextLabelLeft = "";
            TextLabelRight = "";
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
            _font = _content.Load<SpriteFont>("Fonts/strasua_13");
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD labels with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            if (Display1stElement)
            {
                // draw the first rectangle
                _basicForms.GradientRectangle(
                    Position.X + BorderSide,
                    Position.Y,
                    Width - 2 * BorderSide - WidthGap - Width2ndElement,
                    Height,
                    new[] { HudColors.GreenDarkGradientLight * 0.85f, HudColors.GreenDarkGradientDark * 0.95f },
                    new[] { 0.2f, 0.8f });

                // draw the title string
                _spriteBatch.DrawString(_font, "Guybrush Threepwood", new Vector2(Position.X + BorderSide + 5, Position.Y + 3), HudColors.FontDark);
            }

            // draw the second rectangle
            if (Display2ndElement)
            {
                _basicForms.GradientRectangle(
                    Position.X + Width - BorderSide - Width2ndElement,
                    Position.Y,
                    Width2ndElement,
                    Height,
                    new[] { HudColors.BlueGradientLight * 0.5f, HudColors.BlueGradientDark * 0.7f },
                    new[] { 0.2f, 0.8f });

                // draw the sub string
                var size = _font.MeasureString("Pirate");
                _spriteBatch.DrawString(_font, "Pirate", new Vector2(Position.X + Width - BorderSide - Width2ndElement / 2 - size.X / 2, Position.Y + 3), HudColors.FontLight);
            }

            _spriteBatch.End();
        }

        #endregion
    }
}
