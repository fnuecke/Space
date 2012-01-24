using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.ScreenManagement.Screens.Interfaces;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Elements.Hud.HudComponents
{
    class HudIcon : AHudElement
    {

        public enum Mode {
            Buff, Debuff, Neutral
        }

        /// <summary>
        /// The Texture2D image of the portrait that should be displayed.
        /// </summary>
        private Texture2D _image;

        public Mode CurrentMode { get; set; }

        /// <summary>
        /// Set a new image as a content portrait.
        /// Please remember to reset the size if the image size has changed.
        /// </summary>
        /// <param name="path"></param>
        public void SetImage(String path)
        {
            _image = _client.Game.Content.Load<Texture2D>(path);
        }

        public HudIcon(GameClient client)
            : base(client)
        {
            
        }

        public override void SetSize(Point size)
        {
            base.SetSize(size);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            // set some standard Values
            SetSize(new Point(40, 40));
            CurrentMode = Mode.Neutral;
            SetImage("Textures/Icons/Buffs/default");
        }

        public override void Draw()
        {
            _spriteBatch.Begin();

            // set the colors of the background dependant on the current mode
            Color col1 = HudColors.GreenDarkGradientLight;
            Color col2 = HudColors.GreenDarkGradientDark;
            if (CurrentMode == Mode.Debuff)
            {
                col1 = HudColors.RedGradientLight;
                col2 = HudColors.RedGradientDark;
            }
            else if (CurrentMode == Mode.Neutral)
            {
                col1 = HudColors.OrangeGradientLight;
                col2 = HudColors.OrangeGradientDark;
            }
            _basicForms.GradientRectangle(GetPosition().X, GetPosition().Y, GetWidth(), GetHeight(), col1, col2);

            var posImage = new Rectangle(
                GetPosition().X + ((GetWidth() - _image.Width) / 2),
                GetPosition().Y + ((GetHeight() - _image.Height) / 2),
                _image.Width,
                _image.Height);
            _spriteBatch.Draw(_image, posImage, Color.White);

            _spriteBatch.End();
        }
    }
}
