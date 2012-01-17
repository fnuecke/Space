using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Gameplay
{
    class LifeEnergy
    {

        /// <summary>
        /// The values for the sections in the health bar.
        /// </summary>
        private int[] Ranges = { 50, 500, 2500};

        /// <summary>
        /// The minimum gap between two sections in the health bar.
        /// </summary>
        private int MinGap = 3;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Helper class for drawing basic forms.
        /// </summary>
        private BasicForms _basicForms;

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        public LifeEnergy(GameClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;
            _basicForms = new BasicForms(_spriteBatch);
        }

        public void Draw()
        {
            _spriteBatch.Begin();

            int width = 106;
            int height = 11;

            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            int positionX = (viewport.Width - width) / 2;
            int positionY = (viewport.Height - height) / 2 - 40;

            int maxLife = 100;
            int currentLife = 100;

            int border = 1;

            _basicForms.FillRectangle(positionX, positionY, width, height, Color.Black);
            _basicForms.FillRectangle(positionX + border, positionY + border, (int)((width - 2 * border) * (currentLife * 1.0 / maxLife)), height - 2 * border, new Color(142, 232, 63));

            // draw the standard pattern
            for (int i = 1; i * 1.0 / (width - 2 * border) < currentLife * 1.0 / maxLife; i += 2)
            {
                _basicForms.FillRectangle(positionX + border + i, positionY + border + 2, 1, height - 2 * border - 2, Color.White * 0.3f);
            }

            // draw the first separation
            for (int i = Ranges[0]; i < currentLife; i += Ranges[0])
            {
                int pos = (int)((i * 1.0) / maxLife * (width - 2 * border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(positionX + border + pos, positionY + border + 2, 1, height - 2 * border - 2 * 2, Color.Black * 0.25f);
            }

            // draw the second separation
            for (int i = Ranges[1]; i <= currentLife; i += Ranges[1])
            {
                int pos = (int)((i * 1.0) / maxLife * (width - 2 * border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(positionX + border + pos, positionY + border + 1, 1, height - 2 * border - 2 * 1, Color.Black);
            }

            // draw the third separation
            for (int i = Ranges[2]; i <= currentLife; i += Ranges[2])
            {
                int pos = (int)((i * 1.0) / maxLife * (width - 2 * border));
                if (pos < MinGap)
                {
                    break;
                }
                _basicForms.FillRectangle(positionX + border + pos, positionY, 2, height, Color.Black);
            }

            _spriteBatch.End();
        }
    }
}
