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
    class HudLabel : IHudElement
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

        private HudSingleLabel _label1;
        private HudSingleLabel _label2;

        private Point _position;

        private int _height;

        #endregion

        #region Properties

        /// <summary>
        /// The width of the element.
        /// </summary>
        public int Width { get; set; }

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

        /// <summary>
        /// Status whether the first element should be displayed or not.
        /// </summary>
        public Boolean Display1stElement { get; set; }

        /// <summary>
        /// Status whether the second element should be displayed or not.
        /// </summary>
        public Boolean Display2ndElement { get; set; }

        /// <summary>
        /// Text that is displayed in the left label
        /// </summary>
        public String TextLabelLeft { get; set; }

        /// <summary>
        /// Text that is displayed in the right label
        /// </summary>
        public String TextLabelRight { get; set; }

        #endregion

        #region Getter / Setter

        public void SetPosition(Point newPosition)
        {
            _position = newPosition;
            _label1.SetPosition(new Point(newPosition.X + BorderSide, newPosition.Y));
            _label2.SetPosition(new Point(newPosition.X + Width - BorderSide - Width2ndElement, newPosition.Y));
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
        public HudLabel(GameClient client)
        {
            _client = client;

            _label1 = new HudSingleLabel(_client);
            _label2 = new HudSingleLabel(_client);

            // set the standard values into the field.
            Width = StandardWidth;
            _height = StandardHeight;
            Width2ndElement = StandardWidth2ndElement;
            WidthGap = StandardWidthGap;
            BorderSide = StandardBorderSide;
            Display1stElement = StandardDisplay1stElement;
            Display2ndElement = StandardDisplay2ndElement;
            _position = new Point(0, 0);
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

            _label1.LoadContent(spriteBatch, content);
            _label1.SetPosition(new Point(_position.X + BorderSide, _position.Y));
            _label1.Width = Width - 2 * BorderSide - WidthGap - Width2ndElement;
            _label1.SetHeight(_height);

            _label2.LoadContent(spriteBatch, content);
            _label2.SetPosition(new Point(_position.X + Width - BorderSide - Width2ndElement, _position.Y));
            _label2.Width = Width2ndElement;
            _label2.SetHeight(_height);

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
            _label1.Draw();
            _label2.Draw();
        }

        #endregion
    }
}
