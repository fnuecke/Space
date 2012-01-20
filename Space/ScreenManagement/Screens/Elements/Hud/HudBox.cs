using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;
using Space.ScreenManagement.Screens.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudBox : IHudParentElement
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
        private HudContent _bgContent;
        private HudSpacer _spacerBottom;
        private HudHeader _footer;

        /// <summary>
        /// The current top-left position of the parent element.
        /// </summary>
        private Point _position = new Point(0, 0);

        public HudBox(GameClient client)
        {
            _client = client;
            _header = new HudHeader(_client);
            _spacerTop = new HudSpacer(_client, HudSpacer.Mode.Top);
            _bgContent = new HudContent(_client);
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
            _spacerTop.LoadContent(spriteBatch, content);
            _bgContent.LoadContent(spriteBatch, content);
            _spacerBottom.LoadContent(spriteBatch, content);
            _footer.LoadContent(spriteBatch, content);

            SetPosition(new Point(100, 100));
        }

        // Implementation of IHudParentElement interface
        public void SetPosition(Point newPosition)
        {
            _position = newPosition;
            _header.Position = _position;
            _spacerTop.Position = new Point(_header.Position.X, _header.Position.Y + _header.Height);
            _bgContent.Position = new Point(_spacerTop.Position.X, _spacerTop.Position.Y + _spacerTop.GetHeight());
            _spacerBottom.Position = new Point(_bgContent.Position.X, _bgContent.Position.Y + _bgContent.Height);
            _footer.Position = new Point(_spacerBottom.Position.X, _spacerBottom.Position.Y + _spacerBottom.GetHeight());
        }

        // Implementation of IHudParentElement interface
        public int GetHeight() {
            int height = 0;
            height += _header.Height;
            height += _spacerTop.GetHeight();
            height += _bgContent.Height;
            height += _spacerBottom.GetHeight();
            height += _footer.Height;
            return height;
        }


        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public void Draw()
        {
            _header.Draw();
            _spacerTop.Draw();
            _bgContent.Draw();
            _spacerBottom.Draw();
            _footer.Draw();
        }
    }
}
