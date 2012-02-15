﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Helper;
using Space.ScreenManagement.Screens.Ingame.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudContent : AbstractHudElement
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
        /// Helper class for drawing game specific forms.
        /// </summary>
        private SpaceForms _spaceForms;

        /// <summary>
        /// The Texture2D image of the portrait that should be displayed.
        /// </summary>
        private Texture2D _image;

        /// <summary>
        /// The current mode.
        /// </summary>
        private Mode _currentMode;

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
        public void SetImage(String path)
        {
            _image = _client.Game.Content.Load<Texture2D>(path);
        }

        /// <summary>
        /// Commit the size of the content (without the borders!) and this method
        /// will calculate the real size of the element (with the borders)
        /// </summary>
        /// <param name="width">The new width of the content.</param>
        /// <param name="height">The new height of the content.</param>
        public void SetContentSize(int width, int height)
        {
            SetWidth(width + 2 * BorderAround + BorderLeft + BorderRight + 2 * GapBorderContent);
            SetHeight(height + 2 * BorderAround + 2 * GapBorderContent);
        }

        /// <summary>
        /// Returns the content size (without the borders!)
        /// </summary>
        /// <returns>The content size (without the borders).</returns>
        public Point GetContentSize()
        {
            return new Point(_width - 2 * BorderAround - BorderLeft - BorderRight - 2 * GapBorderContent, _height - 2 * BorderAround - 2 * GapBorderContent);
        }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudContent(GameClient client, Mode mode, Boolean addBorderToWest, Boolean addBorderToEast)
            : base(client)
        {
            _currentMode = mode;

            // set the standard values into the field.
            SetSize(new Point(StandardWidth, StandardHeight));
            BorderLeft = StandardBorderSide;
            BorderRight = StandardBorderSide;
            BorderAround = StandardBorderAround;
            GapBorderContent = StandardGapBorderContent;

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
        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _spaceForms = new SpaceForms(_spriteBatch);

            // load an default image as image (image should not be null)
            SetImage("Textures/Portraits/default");
        }

        #endregion

        #region Draw

        public override void Draw()
        {
            _spriteBatch.Begin();

            // draw the border around the box
            _spaceForms.DrawRectangleWithoutEdges(
                GetPosition().X + BorderLeft,
                GetPosition().Y,
                GetWidth() - BorderLeft - BorderRight,
                GetHeight(),
                4, 2, HudColors.Lines);

            if (_currentMode == Mode.BackgroundOnly)
            {
                // draw the background of the box
                _basicForms.FillRectangle(
                    GetPosition().X + BorderAround + BorderLeft + GapBorderContent,
                    GetPosition().Y + BorderAround + GapBorderContent,
                    GetWidth() - BorderLeft - BorderRight - 2 * BorderAround - 2 * GapBorderContent,
                    GetHeight() - 2 * BorderAround - 2 * GapBorderContent,
                    HudColors.BackgroundBox * 0.7f);
            }

            if (_currentMode == Mode.Image)
            {
                var posImage = new Rectangle(
                    GetPosition().X + BorderLeft + BorderAround + GapBorderContent,
                    GetPosition().Y + BorderAround + GapBorderContent,
                    _image.Width,
                    _image.Height);
                _spriteBatch.Draw(_image, posImage, Color.White * 0.7f);
            }

            _spriteBatch.End();
        }

        #endregion
    }
}