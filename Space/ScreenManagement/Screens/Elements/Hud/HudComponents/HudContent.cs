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
    class HudContent
    {

        private const int StandardWidth = 302;
        private const int StandardHeight = 100;
        private const int StandardBorderSide = 6;
        private const int StandardBorderAround = 1;

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

        public int Width { get; set; }
        public int Height { get; set; }
        public int BorderSide { get; set; }
        public int BorderAround { get; set; }
        public Point Position { get; set; }

        public HudContent(GameClient client)
        {
            _client = client;
            
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

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            _spaceForms.DrawRectangleWithoutEdges(
                Position.X + BorderSide,
                Position.Y,
                Width - 2 * BorderSide,
                Height,
                4, 2, HudColors.Lines);

            _basicForms.FillRectangle(
                Position.X + BorderAround + BorderSide,
                Position.Y + BorderAround,
                Width - 2 * BorderSide - 2 * BorderAround,
                Height - 2 * BorderAround,
                HudColors.BackgroundBox * 0.8f);

            _spriteBatch.End();

        }
    }
}
