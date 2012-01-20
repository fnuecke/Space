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
    class HudSpacer : IHudChildElement
    {

        #region Constants

        /// <summary>
        /// Enums for the different Modes that are possible within this class:
        /// </summary>
        public enum Mode
        {
            Top, Center, Bottom
        }

        /// <summary>
        /// Standard value for the width.
        /// </summary>
        private const int StandardWidth = 302;

        /// <summary>
        /// Standard value for the thickness of the spacer.
        /// </summary>
        private const int StandardThicknessSpacer = 2;

        /// <summary>
        /// Standard value for the thickness of the line on the side.
        /// </summary>
        private const int StandardThicknessLineSide = 1;

        /// <summary>
        /// Standard value for the length of the line on the side.
        /// </summary>
        private const int StandardLengthLineSide = 8;

        /// <summary>
        /// Standard value for the size of the outter border.
        /// </summary>
        private const int StandardOuterBorder = 1;

        /// <summary>
        /// Standard value for the size of the inner border.
        /// </summary>
        private const int StandardInnerBorder = 3;

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
        /// The height of the object (addition of thickness spacer + outter border + inner border)
        /// </summary>
        private int _height;

        /// <summary>
        /// The current mode.
        /// </summary>
        private Mode _mode;

        #endregion

        #region Properties

        /// <summary>
        /// The width of the element.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The thickness of the spacer.
        /// </summary>
        public int ThicknessSpacer { get; set; }

        /// <summary>
        /// The thickness of the lines on the side.
        /// </summary>
        public int ThicknessLineSide { get; set; }

        /// <summary>
        /// The length of the lines on the side.
        /// </summary>
        public int LengthLineSide { get; set; }

        /// <summary>
        /// The size of the outer border
        /// </summary>
        public int OuterBorder { get; set; }

        /// <summary>
        /// The size of the inner border
        /// </summary>
        public int InnerBorder { get; set; }

        // Implementation of IHudChildElement interface
        public Point Position { get; set; }

        #endregion

        #region Getter

        /// <summary>
        /// Calculates the height of the element and returns the value.
        /// The height is an addition of the outer border, the thickniss of the
        /// spacer and the inner border.
        /// The lines on the side will NOT be added to the height.
        /// </summary>
        /// <returns>The height of this element.</returns>
        public int GetHeight()
        {
            return OuterBorder + ThicknessSpacer + InnerBorder;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        /// <param name="mode">The mode that this object should use.</param>
        public HudSpacer(GameClient client, Mode mode)
        {
            _client = client;
            _mode = mode;

            // set the standard values into the field.
            Width = StandardWidth;
            _height = ThicknessSpacer + StandardOuterBorder + StandardInnerBorder;
            OuterBorder = StandardOuterBorder;
            InnerBorder = StandardInnerBorder;
            ThicknessSpacer = StandardThicknessSpacer;
            ThicknessLineSide = StandardThicknessLineSide;
            LengthLineSide = StandardLengthLineSide;
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
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // prepare some values which are important to get the correct draw
            // position independent of the selected mode.
            int yMove = ThicknessSpacer;
            int borderTop = OuterBorder;
            if (_mode == Mode.Bottom)
            {
                yMove = -LengthLineSide;
                borderTop = InnerBorder;
            }
            else if (_mode == Mode.Center)
            {
                borderTop = (InnerBorder + OuterBorder) / 2;
            }

            // draw the horizontal line
            _basicForms.FillRectangle(
                Position.X,
                Position.Y + borderTop,
                Width,
                ThicknessSpacer,
                HudColors.Lines * 0.9f);

            // draw the side lines
            if (_mode != Mode.Center)
            {
                _basicForms.FillRectangle(
                    Position.X,
                    Position.Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines * 0.9f);

                _basicForms.FillRectangle(
                    Position.X + Width - ThicknessLineSide,
                    Position.Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines * 0.9f);
            }

            _spriteBatch.End();

        }

        #endregion

    }
}
