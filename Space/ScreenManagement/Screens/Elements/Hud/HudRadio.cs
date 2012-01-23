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
    /// <summary>
    /// A HUD element that is used for displaying incoming radio messages.
    /// The radio element displays an image of the receiver and the message
    /// as a text.
    /// </summary>
    class HudRadio : IHudElement
    {

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
        /// The top spacer.
        /// </summary>
        private HudSpacer _spacerTop;

        /// <summary>
        /// The portrait.
        /// </summary>
        private HudContent _bgPortrait;

        /// <summary>
        /// The text.
        /// </summary>
        private HudContent _bgText;

        /// <summary>
        /// The bottom spacer.
        /// </summary>
        private HudSpacer _spacerBottom;

        /// <summary>
        /// The footer label.
        /// </summary>
        private HudLabel _footer;

        /// <summary>
        /// The current top-left position of the parent element.
        /// </summary>
        private Point _position = new Point(0, 0);

        private int _height;

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudRadio(GameClient client)
        {
            _client = client;
            _spacerTop = new HudSpacer(_client, HudSpacer.Mode.Top);
            _bgPortrait = new HudContent(_client, HudContent.Mode.Image, true, false);
            _bgPortrait.setContentSize(69, 92);
            _bgText = new HudContent(_client, HudContent.Mode.BackgroundOnly, false, true);
            _bgText.setSize(_spacerTop.Width - _bgPortrait.getSize().X - 1, _bgPortrait.getSize().Y);
            _spacerBottom = new HudSpacer(_client, HudSpacer.Mode.Bottom);
            _footer = new HudLabel(_client);
        }

        /// <summary>
        /// Load all elements of this HUD element.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;

            _spacerTop.LoadContent(spriteBatch, content);
            _bgPortrait.LoadContent(spriteBatch, content);
            _bgText.LoadContent(spriteBatch, content);
            _spacerBottom.LoadContent(spriteBatch, content);
            _footer.LoadContent(spriteBatch, content);

            // set standard values for the fields
            SetPosition(new Point(0, 0));
        }

        #endregion

        #region Getter & Setter

        // Implementation of IHudParentElement interface
        public void SetPosition(Point newPosition)
        {
            _position = newPosition;
            _spacerTop.SetPosition(_position);
            _bgPortrait.SetPosition(new Point(_spacerTop.GetPosition().X, _spacerTop.GetPosition().Y + _spacerTop.GetHeight()));
            _bgText.SetPosition(new Point(_bgPortrait.GetPosition().X + _bgPortrait.getSize().X + 1, _bgPortrait.GetPosition().Y));
            _spacerBottom.SetPosition(new Point(_bgPortrait.GetPosition().X, _bgPortrait.GetPosition().Y + _bgPortrait.getSize().Y));
            _footer.SetPosition(new Point(_spacerBottom.GetPosition().X, _spacerBottom.GetPosition().Y + _spacerBottom.GetHeight()));
        }

        public Point GetPosition()
        {
            return _position;
        }

        // Implementation of IHudParentElement interface
        public int GetHeight() {
            int height = 0;
            height += _spacerTop.GetHeight();
            height += _bgPortrait.GetHeight();
            height += _spacerBottom.GetHeight();
            height += _footer.GetHeight();
            return height;
        }

        public void SetHeight(int height)
        {
            _height = height;
        }

        /// <summary>
        /// Set the name into the left text label.
        /// </summary>
        /// <param name="name">The name.</param>
        public void setName(String name)
        {
            _footer.TextLabelLeft = name;
        }

        /// <summary>
        /// Set the name into the left text label.
        /// </summary>
        /// <param name="name">The name.</param>
        public void setTitle(String title)
        {
            _footer.TextLabelRight = title;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public void Draw()
        {
            _spacerTop.Draw();
            _bgPortrait.Draw();
            _bgText.Draw();
            _spacerBottom.Draw();
            _footer.Draw();
        }

        #endregion
    }
}
