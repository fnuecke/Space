using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
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
        private HudSpacer _spacerTop;
        private HudSpacer _spacerBottom;
        private HudHeader _footer;

        public HudBox(GameClient client)
        {
            _client = client;
            _header = new HudHeader(_client);
            _spacerTop = new HudSpacer(_client, HudSpacer.Mode.Top);
            _spacerBottom = new HudSpacer(_client, HudSpacer.Mode.Bottom);
            _footer = new HudHeader(_client);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;

            _header.LoadContent(spriteBatch, content);
            _header.Position = new Point(100, 100);
            _spacerTop.LoadContent(spriteBatch, content);
            _spacerTop.Position = new Point(_header.Position.X, _header.Position.Y + _header.Height);
            _spacerBottom.LoadContent(spriteBatch, content);
            _spacerBottom.Position = new Point(_spacerTop.Position.X, _spacerTop.Position.Y + _spacerTop.GetHeight() + 200);
            _footer.LoadContent(spriteBatch, content);
            _footer.Position = new Point(_spacerBottom.Position.X, _spacerBottom.Position.Y + _spacerBottom.GetHeight());
        }

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public void Draw()
        {
            _header.Draw();
            _spacerTop.Draw();
            _spacerBottom.Draw();
            _footer.Draw();
        }
    }
}
