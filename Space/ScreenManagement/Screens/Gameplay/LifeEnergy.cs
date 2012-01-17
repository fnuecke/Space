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
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Ship image (for testing only).
        /// </summary>
        private Texture2D _ship;

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
        }

        public void Draw()
        {
            _spriteBatch.Begin();

            int width = 106;
            int height = 11;

            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            int positionX = (viewport.Width - width) / 2;
            int positionY = (viewport.Height - height) / 2 - 40;

            int maxLife = 560;
            int currentLife = 460;

            BasicForms.FillRectangle(_spriteBatch, positionX, positionY, width, height, Color.Black);
            BasicForms.FillRectangle(_spriteBatch, positionX + 1, positionY + 1, (int) ((width - 2) * (currentLife * 1.0/maxLife)), height - 2, new Color(142, 232, 63));

            for (int i = 25; i <= currentLife; i += 25)
            {
                int pos = (int) ((i * 1.0) / maxLife * width);
                BasicForms.FillRectangle(_spriteBatch, positionX + 1 + pos, positionY + 1, 1, height - 2, Color.Red);
            }

            for (int i = 100; i <= currentLife; i += 100)
            {
                int pos = (int) ((i * 1.0) / maxLife * width);
                BasicForms.FillRectangle(_spriteBatch, positionX + 1 + pos, positionY + 1, 2, height - 2, Color.Red);
            }


            _spriteBatch.End();
        }

    }
}
