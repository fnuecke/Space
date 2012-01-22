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
    class HudContent : IHudElement
    {

        #region Constants

        /// <summary>
        /// Enums for the different Modes that are possible within this class.
        /// </summary>
        public enum Mode
        {
            BackgroundOnly, Image
        }

        /// <summary>
        /// The standard value for the width.
        /// </summary>
        private const int StandardWidth = 302;

        /// <summary>
        /// The standard value for the height.
        /// </summary>
        private const int StandardHeight = 100;

        /// <summary>
        /// The standard value for the size of the border on the sides.
        /// </summary>
        private const int StandardBorderSide = 6;

        /// <summary>
        /// The standard value for the size of the border around the box.
        /// </summary>
        private const int StandardBorderAround = 1;

        /// <summary>
        /// The standard value for the gap between the border and the content.
        /// </summary>
        private const int StandardGapBorderContent = 1;

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
        /// Helper class for drawing game specific forms.
        /// </summary>
        private SpaceForms _spaceForms;

        /// <summary>
        /// The width of the element.
        /// </summary>
        public int _width;

        /// <summary>
        /// The height of the element.
        /// </summary>
        public int _height;

        /// <summary>
        /// The Texture2D image of the portrait that should be displayed.
        /// </summary>
        private Texture2D _image;

        /// <summary>
        /// The current mode.
        /// </summary>
        private Mode _currentMode;

        /// <summary>
        /// The position.
        /// </summary>
        private Point _position;


        #endregion

        #region Properties

        /// <summary>
        /// The size of the border on the left side side.
        /// </summary>
        public int BorderLeft { get; set; }

        /// <summary>
        /// The size of the border on the right side side.
        /// </summary>
        public int BorderRight { get; set; }

        /// <summary>
        /// The size of the border around the box.
        /// </summary>
        public int BorderAround { get; set; }

        /// <summary>
        /// The gap between the border and the content.
        /// </summary>
        public int GapBorderContent { get; set; }

        #endregion

        #region Getter & Setter

        /// <summary>
        /// Set a new image as a content portrait.
        /// Please remember to reset the size if the image size has changed.
        /// </summary>
        /// <param name="path"></param>
        public void setImage(String path)
        {
            _image = _client.Game.Content.Load<Texture2D>(path);
        }

        /// <summary>
        /// Commit the size of the content (without the borders!) and this method
        /// will calculate the real size of the element (with the borders)
        /// </summary>
        /// <param name="width">The new width of the content.</param>
        /// <param name="height">The new height of the content.</param>
        public void setContentSize(int width, int height)
        {
            _width = width + 2 * BorderAround + BorderLeft + BorderRight + 2 * GapBorderContent;
            _height = height + 2 * BorderAround + 2 * GapBorderContent;
        }

        /// <summary>
        /// Returns the content size (without the borders!)
        /// </summary>
        /// <returns>The content size (without the borders).</returns>
        public Point getContentSize()
        {
            return new Point(_width - 2 * BorderAround - BorderLeft - BorderRight - 2 * GapBorderContent, _height - 2 * BorderAround - 2 * GapBorderContent);
        }

        /// <summary>
        /// Returns the size of the element (with borders).
        /// </summary>
        /// <returns>The size of the element (with borders)</returns>
        public Point getSize()
        {
            return new Point(_width, _height);
        }

        /// <summary>
        /// Commit the size of the element (with the borders!)
        /// </summary>
        /// <param name="width">The new width of the element.</param>
        /// <param name="height">The new height of the element.</param>
        public void setSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

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

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudContent(GameClient client, Mode mode, Boolean addBorderToWest, Boolean addBorderToEast)
        {
            _client = client;
            _currentMode = mode;

            // set the standard values into the field.
            setSize(StandardWidth, StandardHeight);
            BorderLeft = StandardBorderSide;
            BorderRight = StandardBorderSide;
            BorderAround = StandardBorderAround;
            GapBorderContent = StandardGapBorderContent;
            _position = new Point(100, 100);

            if (!addBorderToWest)
            {
                BorderLeft = 0;
            }
            if (!addBorderToEast)
            {
                BorderRight = 0;
            }

        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch, _client);
            _spaceForms = new SpaceForms(_spriteBatch);

            // load an default image as image (image should not be null)
            setImage("Textures/Portraits/default");
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // draw the border around the box
            _spaceForms.DrawRectangleWithoutEdges(
                _position.X + BorderLeft,
                _position.Y,
                _width - BorderLeft - BorderRight,
                _height,
                4, 2, HudColors.Lines);

            if (_currentMode == Mode.BackgroundOnly)
            {
                // draw the background of the box
                _basicForms.FillRectangle(
                    _position.X + BorderAround + BorderLeft + GapBorderContent,
                    _position.Y + BorderAround + GapBorderContent,
                    _width - BorderLeft - BorderRight - 2 * BorderAround - 2 * GapBorderContent,
                    _height - 2 * BorderAround - 2 * GapBorderContent,
                    HudColors.BackgroundBox * 0.7f);
            }

            if (_currentMode == Mode.Image)
            {
                var posImage = new Rectangle(
                    _position.X + BorderLeft + BorderAround + GapBorderContent,
                    _position.Y + BorderAround + GapBorderContent,
                    _image.Width,
                    _image.Height);
                _spriteBatch.Draw(_image, posImage, Color.White * 0.7f);
            }

            _spriteBatch.End();
        }

        #endregion
    }
}
