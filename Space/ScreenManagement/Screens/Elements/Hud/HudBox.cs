using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudBox
    {
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

        private HudHeader _header;

        public HudBox(GameClient client)
        {
            _client = client;
            _header = new HudHeader(_client);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;

            _header.LoadContent(spriteBatch, content);
        }

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public void Draw()
        {
            _header.Draw();
        }
    }
}
