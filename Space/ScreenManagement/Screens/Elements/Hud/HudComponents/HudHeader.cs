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

        private const int StandardWidth = 302;
        private const int StandardHeight = 18;
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

        public int Width { get; set; }
        public int Height { get; set; }
        public int WidthGap { get; set; }
        public int Width2ndElement { get; set; }
        public int BorderSide { get; set; }
        public Point Position { get; set; }

        private SpriteFont font;


        public HudHeader(GameClient client)
        {
            _client = client;

            Width = StandardWidth;
            Height = StandardHeight;
            Width2ndElement = StandardWidth2ndElement;
            WidthGap = StandardWidthGap;
            BorderSide = StandardBorderSide;
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

            font = _content.Load<SpriteFont>("Fonts/strasua_13");
        }

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // draw the first rectangle
            _basicForms.FillRectangle(
                Position.X + BorderSide,
                Position.Y,
                Width - 2 * BorderSide - WidthGap - Width2ndElement,
                Height,
                HudColors.GreenDarkGradientDark);

            // draw the second rectangle
            _basicForms.FillRectangle(
                Position.X + Width - BorderSide - Width2ndElement,
                Position.Y,
                Width2ndElement,
                Height,
                HudColors.GreenDarkGradientDark);

            // draw the title string
            _spriteBatch.DrawString(font, "Guybrush Threepwood", new Vector2(Position.X + BorderSide + 2, Position.Y + 3), HudColors.FontDark);

            _basicForms.GradientRectangle(Position.X + 50, Position.Y + 100, 100, 50, new[] { Color.Red, Color.Blue }, new[] {0.4f, 0.6f});

            _spriteBatch.End();
        }
    }
}
