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
    class HudContent : IHudChildElement
    {

        #region Constants

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
        /// The size of the border of the side.
        /// </summary>
        public int BorderSide { get; set; }

        /// <summary>
        /// The size of the border around the box.
        /// </summary>
        public int BorderAround { get; set; }

        // Implementation of IHudChildElement interface
        public Point Position { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudContent(GameClient client)
        {
            _client = client;

            // set the standard values into the field.
            Width = StandardWidth;
            Height = StandardHeight;
            BorderSide = StandardBorderSide;
            BorderAround = StandardBorderAround;
            Position = new Point(100, 100);

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
                Position.X + BorderSide,
                Position.Y,
                Width - 2 * BorderSide,
                Height,
                4, 2, HudColors.Lines);

            // draw the background of the box
            _basicForms.FillRectangle(
                Position.X + BorderAround + BorderSide,
                Position.Y + BorderAround,
                Width - 2 * BorderSide - 2 * BorderAround,
                Height - 2 * BorderAround,
                HudColors.BackgroundBox);

            _spriteBatch.End();
        }

        #endregion
    }
}
