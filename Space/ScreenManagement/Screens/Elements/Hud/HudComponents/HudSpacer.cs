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
    class HudSpacer : AHudElement
    {

        #region Constants

        /// <summary>
        /// Enums for the different Modes that are possible within this class.
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
        /// Standard value for the size of the outer border.
        /// </summary>
        private const int StandardOuterBorder = 1;

        /// <summary>
        /// Standard value for the size of the inner border.
        /// </summary>
        private const int StandardInnerBorder = 3;

        #endregion

        #region Fields

        /// <summary>
        /// The current mode.
        /// </summary>
        private Mode _mode;

        #endregion

        #region Properties

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

        #endregion

        #region Getter

        public override int GetHeight()
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
            : base(client)
        {
            _mode = mode;

            // set the standard values into the field.
            SetWidth(StandardWidth);
            SetHeight(ThicknessSpacer + StandardOuterBorder + StandardInnerBorder);
            OuterBorder = StandardOuterBorder;
            InnerBorder = StandardInnerBorder;
            ThicknessSpacer = StandardThicknessSpacer;
            ThicknessLineSide = StandardThicknessLineSide;
            LengthLineSide = StandardLengthLineSide;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public override void Draw()
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
                GetPosition().X,
                GetPosition().Y + borderTop,
                GetWidth(),
                ThicknessSpacer,
                HudColors.Lines * 0.9f);

            // draw the side lines
            if (_mode != Mode.Center)
            {
                _basicForms.FillRectangle(
                    GetPosition().X,
                    GetPosition().Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines * 0.9f);

                _basicForms.FillRectangle(
                    GetPosition().X + GetWidth() - ThicknessLineSide,
                    GetPosition().Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines * 0.9f);
            }

            _spriteBatch.End();

        }

        #endregion

    }
}
