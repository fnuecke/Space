using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.ScreenManagement.Screens.Interfaces;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;
using Microsoft.Xna.Framework;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Elements.Hud.HudComponents
{
    class HudBuffElement : AHudElement
    {

        /// <summary>
        /// The standard value of the padding of the label to the outer border.
        /// </summary>
        private const int StandardPadding = 2;

        private const int StandardHeight = 40;
        private const int StandardWidth = 40;

        /// <summary>
        /// Helper class for drawing game specific forms.
        /// </summary>
        private SpaceForms _spaceForms;

        private HudIcon _icon;
        private int _padding;

        public HudBuffElement(GameClient client)
            : base(client)
        {
            _icon = new HudIcon(client);           
        }

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);
            _icon.SetPosition(new Point(position.X + StandardPadding, position.Y + StandardPadding));
        }

        public override void SetSize(Point size)
        {
            base.SetSize(size);
            _icon.SetSize(new Point(size.X - 2 * _padding, size.Y - 2 * _padding));
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _spaceForms = new SpaceForms(_spriteBatch);


            _padding = StandardPadding;
            SetWidth(StandardWidth);
            SetHeight(StandardHeight);

            _icon.LoadContent(spriteBatch, content);
            _icon.CurrentMode = HudIcon.Mode.Buff;
        }

        public override void Draw()
        {

            _spriteBatch.Begin();

            _spaceForms.DrawRectangleWithoutEdges(
                GetPosition().X,
                GetPosition().Y,
                GetWidth(),
                GetHeight(),
                4, _padding - 1, HudColors.Lines);


            _spriteBatch.End();
            _icon.Draw();
        }

    }
}
