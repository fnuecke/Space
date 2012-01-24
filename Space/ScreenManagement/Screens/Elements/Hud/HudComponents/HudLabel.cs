using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudLabel : AHudElement
    {

        #region Constants

        /// <summary>
        /// The standard value for the width;
        /// </summary>
        private const int StandardWidth = 302;

        /// <summary>
        /// The standard value for the height;
        /// </summary>
        private const int StandardHeight = 18;

        /// <summary>
        /// The standard value for the width of the gap between the two boxes;
        /// </summary>
        private const int StandardWidthGap = 2;

        /// <summary>
        /// The standard value for the width of the second element;
        /// </summary>
        private const int StandardWidth2ndElement = 85;

        /// <summary>
        /// The standard value for the size of the border on the side.
        /// </summary>
        private const int StandardBorderSide = 1;

        /// <summary>
        /// The standard value for the status whether the first element should be displayed or not.
        /// </summary>
        private const Boolean StandardDisplay1stElement = true;

        /// <summary>
        /// The standard value for the status whether the second element should be displayed or not.
        /// </summary>
        private const Boolean StandardDisplay2ndElement = true;

        #endregion

        #region Fields

        private HudSingleLabel _label1;

        private HudSingleLabel _label2;

        #endregion

        #region Properties

        /// <summary>
        /// The width of the gap between the two boxes.
        /// </summary>
        public int WidthGap { get; set; }

        /// <summary>
        /// The width of the second box.
        /// </summary>
        public int Width2ndElement { get; set; }

        /// <summary>
        /// The size of the border on the sides.
        /// </summary>
        public int BorderSide { get; set; }

        /// <summary>
        /// Status whether the first element should be displayed or not.
        /// </summary>
        public Boolean Display1stElement { get; set; }

        /// <summary>
        /// Status whether the second element should be displayed or not.
        /// </summary>
        public Boolean Display2ndElement { get; set; }

        #endregion

        #region Getter / Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);
            _label1.SetPosition(new Point(position.X + BorderSide, position.Y));
            _label2.SetPosition(new Point(position.X + GetWidth() - BorderSide - Width2ndElement, position.Y));
        }

        /// <summary>
        /// Set a text into the left label.
        /// </summary>
        /// <param name="text">The text to display</param>
        public void SetTextLabelLeft(String text)
        {
            _label1.Text = text;
        }

        /// <summary>
        /// Set a text into the right label.
        /// </summary>
        /// <param name="text">The text to display</param>
        public void SetTextLabelRight(String text)
        {
            _label2.Text = text;
        }


        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudLabel(GameClient client)
            : base(client)
        {
            _label1 = new HudSingleLabel(_client);
            _label2 = new HudSingleLabel(_client);

            // set the standard values into the field.
            SetWidth(StandardWidth);
            SetHeight(StandardHeight);
            Width2ndElement = StandardWidth2ndElement;
            WidthGap = StandardWidthGap;
            BorderSide = StandardBorderSide;
            Display1stElement = StandardDisplay1stElement;
            Display2ndElement = StandardDisplay2ndElement;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            // set up the left label
            _label1.LoadContent(spriteBatch, content);
            _label1.SetPosition(new Point(GetPosition().X + BorderSide, GetPosition().Y));
            _label1.SetWidth(GetWidth() - 2 * BorderSide - WidthGap - Width2ndElement);
            _label1.SetHeight(GetHeight());
            _label1.ColorNorth = HudColors.GreenDarkGradientLight * 0.85f;
            _label1.ColorSouth = HudColors.GreenDarkGradientDark * 0.95f;
            _label1.ColorText = HudColors.FontDark;
            _label1.TextAlign = HudSingleLabel.Alignments.Left;

            // set up the right label
            _label2.LoadContent(spriteBatch, content);
            _label2.SetPosition(new Point(GetPosition().X + GetWidth() - BorderSide - Width2ndElement, GetPosition().Y));
            _label2.SetWidth(Width2ndElement);
            _label2.SetHeight(GetHeight());
            _label2.ColorNorth = HudColors.BlueGradientLight * 0.85f;
            _label2.ColorSouth = HudColors.BlueGradientDark * 0.95f;
            _label2.ColorText = HudColors.FontLight;
            _label2.TextAlign = HudSingleLabel.Alignments.Center;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD labels with the current values.
        /// </summary>
        public override void Draw()
        {
            _label1.Draw();
            _label2.Draw();
        }

        #endregion
    }
}
