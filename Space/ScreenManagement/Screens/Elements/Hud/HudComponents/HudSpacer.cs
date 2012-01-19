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
    class HudSpacer
    {

        public enum Mode
        {
            Top, Center, Bottom
        }

        private const int StandardWidth = 302;
        private const int StandardThicknessSpacer = 2;
        private const int StandardThicknessLineSide = 1;
        private const int StandardLengthLineSide = 8;
        private const int StandardOutterBorder = 1;
        private const int StandardInnerBorder = 3;


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

        private int _height;
        private Mode _mode;

        public int Width { get; set; }
        public int ThicknessSpacer { get; set; }
        public int ThicknessLineSide { get; set; }
        public int LengthLineSide { get; set; }
        public Point Position { get; set; }
        public int OutterBorder { get; set; }
        public int InnerBorder { get; set; }

        public int GetHeight()
        {
            return OutterBorder + ThicknessSpacer + InnerBorder;
        }


        public HudSpacer(GameClient client, Mode mode)
        {
            _client = client;
            _mode = mode;

            Width = StandardWidth;
            _height = ThicknessSpacer + StandardOutterBorder + StandardInnerBorder;
            OutterBorder = StandardOutterBorder;
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
            _basicForms = new BasicForms(_spriteBatch);
        }

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // prepare some values which are important to get the correct draw
            // position independent of the selected mode.
            int yMove = ThicknessSpacer;
            int borderTop = OutterBorder;
            if (_mode == Mode.Bottom)
            {
                yMove = -LengthLineSide;
                borderTop = InnerBorder;
            }
            else if (_mode == Mode.Center)
            {
                borderTop = (InnerBorder + OutterBorder) / 2;
            }

            // draw the horizontal line
            _basicForms.FillRectangle(
                Position.X,
                Position.Y,
                Width,
                GetHeight(),
                Color.AliceBlue * 0.3f);

            // draw the horizontal line
            _basicForms.FillRectangle(
                Position.X,
                Position.Y + borderTop,
                Width,
                ThicknessSpacer,
                HudColors.Lines);

            // draw the side lines
            if (_mode != Mode.Center)
            {
                _basicForms.FillRectangle(
                    Position.X,
                    Position.Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines);

                _basicForms.FillRectangle(
                    Position.X + Width - ThicknessLineSide,
                    Position.Y + borderTop + yMove,
                    ThicknessLineSide,
                    LengthLineSide,
                    HudColors.Lines);
            }

            _spriteBatch.End();

        }
    }
}
