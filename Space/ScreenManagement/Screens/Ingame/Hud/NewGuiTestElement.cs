using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Nuclex.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.ScreenManagement.Screens.Helper;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{
    class NewGuiTestElement : AbstractGuiElement
    {

        bool position1 = true;
        bool position2 = false;
        bool isSelected = false;
        int lastSelected = -1;
        Vector2 mousePos;

        Texture2D _image;

        public NewGuiTestElement(GameClient client)
            : base(client)
        {

        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _image = _client.Game.Content.Load<Texture2D>("Textures/Icons/Buffs/default");


            mousePos = new Vector2(0, 0);
            base.Enabled = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin();
            var basicForms = new BasicForms(_spriteBatch, _client);
            basicForms.DrawRectangle(300, 300, 50, 50, Color.White);
            basicForms.DrawRectangle(300, 400, 50, 50, Color.White);

            if (position1)
            {
                var posImage = new Rectangle(300, 300, 50, 50);
                _spriteBatch.Draw(_image, posImage, Color.White);
            }

            if (position2)
            {
                var posImage = new Rectangle(300, 400, 50, 50);
                _spriteBatch.Draw(_image, posImage, Color.White);
            }

            if (isSelected)
            {
                var posImage = new Rectangle((int)mousePos.X, (int)mousePos.Y, 50, 50);
                _spriteBatch.Draw(_image, posImage, Color.White);
            }

            var _font = _content.Load<SpriteFont>("Fonts/strasua_11");
            _spriteBatch.DrawString(_font, "Move meeeee!", new Vector2(300, 369), Color.White);

            _spriteBatch.End();
        }

        public override void DoHandleMousePressed(MouseButtons buttons)
        {
            if ((mousePos.X >= 300 && mousePos.X <= 350) && (mousePos.Y >= 300 && mousePos.Y <= 350))
            {
                if (position1) {
                    isSelected = true;
                    position1 = false;
                    lastSelected = 1;
                    return;
                }
            }
            if ((mousePos.X >= 300 && mousePos.X <= 350) && (mousePos.Y >= 400 && mousePos.Y <= 450))
            {
                if (position2)
                {
                    isSelected = true;
                    position2 = false;
                    lastSelected = 2;
                    return;
                }
            }
            lastSelected = -1;
            isSelected = false;
            
        }

        public override void DoHandleMouseReleased(MouseButtons buttons)
        {
            isSelected = false;

            if (lastSelected == -1)
            {
                return;
            }

            if ((mousePos.X >= 300 && mousePos.X <= 350) && (mousePos.Y >= 300 && mousePos.Y <= 350))
            {
                position1 = true;
                lastSelected = 1;
                return;
            }
            if ((mousePos.X >= 300 && mousePos.X <= 350) && (mousePos.Y >= 400 && mousePos.Y <= 450))
            {
                position2 = true;
                lastSelected = 2;
                return;
            }

            if (lastSelected == 1)
            {
                position1 = true;
            }
            if (lastSelected == 2)
            {
                position2 = true;
            }
        }

        public override void DoHandleMouseMoved(float x, float y)
        {
            mousePos.X = x;
            mousePos.Y = y;
        }
    }
}
