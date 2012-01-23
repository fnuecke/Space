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

        /// <summary>
        /// The Texture2D image of the portrait that should be displayed.
        /// </summary>
        private Texture2D _image;

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
            SetImage("Textures/Icons/Buffs/default");
        }

        public override void Draw()
        {
            _spriteBatch.Begin();

            _basicForms.GradientRectangle(GetPosition().X, GetPosition().Y, GetWidth(), GetHeight(), Color.Red, Color.Blue);

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
