using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A HUD element that is used for displaying incoming radio messages.
    /// The radio element displays an image of the receiver and the message
    /// as a text.
    /// </summary>
    class HudRadio : AHudElement
    {

        #region Fields

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

        #endregion

        #region Properties

        /// <summary>
        /// The name of the reiceiver.
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// The description of the reiceiver.
        /// </summary>
        public String Description { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudRadio(GameClient client)
            : base(client)
        {
            _spacerTop = new HudSpacer(_client, HudSpacer.Mode.Top);
            _bgPortrait = new HudContent(_client, HudContent.Mode.Image, true, false);
            _bgText = new HudContent(_client, HudContent.Mode.BackgroundOnly, false, true);
            _spacerBottom = new HudSpacer(_client, HudSpacer.Mode.Bottom);
            _footer = new HudLabel(_client);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _spacerTop.LoadContent(spriteBatch, content);
            _bgPortrait.LoadContent(spriteBatch, content);
            _bgText.LoadContent(spriteBatch, content);
            _spacerBottom.LoadContent(spriteBatch, content);
            _footer.LoadContent(spriteBatch, content);

            _bgPortrait.setContentSize(69, 92);
            _bgText.setSize(_spacerTop.GetWidth() - _bgPortrait.getSize().X - 1, _bgPortrait.getSize().Y);
            _footer.SetTextLabelLeft("Guybrush");
            _footer.SetTextLabelRight("Pir");
        }

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);
            _spacerTop.SetPosition(_position);
            _bgPortrait.SetPosition(new Point(_spacerTop.GetPosition().X, _spacerTop.GetPosition().Y + _spacerTop.GetHeight()));
            _bgText.SetPosition(new Point(_bgPortrait.GetPosition().X + _bgPortrait.getSize().X + 1, _bgPortrait.GetPosition().Y));
            _spacerBottom.SetPosition(new Point(_bgPortrait.GetPosition().X, _bgPortrait.GetPosition().Y + _bgPortrait.getSize().Y));
            _footer.SetPosition(new Point(_spacerBottom.GetPosition().X, _spacerBottom.GetPosition().Y + _spacerBottom.GetHeight()));
        }

        public override int GetHeight()
        {
            int height = 0;
            height += _spacerTop.GetHeight();
            height += _bgPortrait.GetHeight();
            height += _spacerBottom.GetHeight();
            height += _footer.GetHeight();
            return height;
        }

        /// <summary>
        /// Set the name into the left text label.
        /// </summary>
        /// <param name="name">The name.</param>
        public void setName(String name)
        {
            _footer.SetTextLabelLeft(name);
        }

        /// <summary>
        /// Set the name into the left text label.
        /// </summary>
        /// <param name="name">The name.</param>
        public void setTitle(String title)
        {
            _footer.SetTextLabelRight(title);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
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
