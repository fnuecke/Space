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
using Space.ScreenManagement.Screens.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudSingleLabel : AHudElement
    {

        #region Constants

        /// <summary>
        /// The standard value for the width;
        /// </summary>
        private const int StandardWidth = 150;

        /// <summary>
        /// The standard value for the height;
        /// </summary>
        private const int StandardHeight = 17;

        #endregion

        #region Fields
        /// <summary>
        /// Holds the font information.
        /// </summary>
        private SpriteFont _font;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudSingleLabel(GameClient client)
            : base(client)
        {
            // set the standard values into the field.
            SetWidth(StandardWidth);
            SetHeight(StandardHeight);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            // load the font that is used for the labels
            _font = _content.Load<SpriteFont>("Fonts/strasua_11");
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD labels with the current values.
        /// </summary>
        public override void Draw()
        {
            _spriteBatch.Begin();

            // draw the first rectangle
            _basicForms.GradientRectangle(
                GetPosition().X,
                GetPosition().Y,
                GetWidth(),
                GetHeight(),
                new[] { HudColors.GreenDarkGradientLight * 0.85f, HudColors.GreenDarkGradientDark * 0.95f },
                new[] { 0.2f, 0.8f });

            // draw the title string
            String TextLabelLeft = "Lalala";
            _spriteBatch.DrawString(_font, TextLabelLeft, new Vector2(GetPosition().X + 5, GetPosition().Y + 2), HudColors.FontDark);

            _spriteBatch.End();
        }

        #endregion

    }
}
