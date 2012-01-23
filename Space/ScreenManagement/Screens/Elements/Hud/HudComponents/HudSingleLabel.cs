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

        public enum Alignments
        {
            Right, Center, Left
        }

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

        #region Properties

        /// <summary>
        /// The text that is displayed in the label.
        /// </summary>
        public String Text { get; set; }

        /// <summary>
        /// The color that is used to the top gradient color.
        /// </summary>
        public Color ColorNorth { get; set; }

        /// <summary>
        /// The color that is used to the bottom gradient color.
        /// </summary>
        public Color ColorSouth { get; set; }

        /// <summary>
        /// The text color.
        /// </summary>
        public Color ColorText { get; set; }

        public Alignments TextAlign { get; set; }

        #endregion

        #region Getter / Setter

        /// <summary>
        /// Remove the text from the label.
        /// </summary>
        public void RemoveText()
        {
            Text = "";
        }

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

            // set some standard values
            Text = "";
            ColorNorth = Color.Green;
            ColorSouth = Color.Red;
            ColorText = Color.Black;
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
                new[] { ColorNorth, ColorSouth },
                new[] { 0.2f, 0.8f });

            // draw the title string
            int xPos = GetPosition().X + 5;
            if (TextAlign == Alignments.Center)
            {
                xPos = (int) ( GetPosition().X + (GetWidth() - _font.MeasureString(Text).X) / 2);
            }
            else if (TextAlign == Alignments.Right)
            {
                xPos = (int) (GetPosition().X + GetWidth() - 2 - _font.MeasureString(Text).X - 5);
            }
            _spriteBatch.DrawString(_font, Text, new Vector2(xPos, GetPosition().Y + 2), ColorText);

            _spriteBatch.End();
        }

        #endregion

    }
}
