using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;
using Space.ScreenManagement.Screens.Interfaces;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A sample file that can be used as a kind of template to create a new
    /// object for a new HUD element.
    /// 
    /// It is NOT intended to be used in the hud!
    /// </summary>
    class HudBuffBar : AHudElement
    {

        #region Fields

        private const int StandardGap = 6;

        private HudBuffElement _stabilisator;
        private HudBuffElement _acceleration;

        private int _gap; 

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudBuffBar(GameClient client)
            : base(client)
        {
            _stabilisator = new HudBuffElement(client);
            _acceleration = new HudBuffElement(client);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _stabilisator.LoadContent(spriteBatch, content);
            _acceleration.LoadContent(spriteBatch, content);

            _gap = StandardGap;
            SetPosition(GetPosition());
        }

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);

            _stabilisator.SetSize(new Point(50, 50));
            _stabilisator.SetPosition(position);
            _acceleration.SetSize(new Point(50, 50));
            _acceleration.SetPosition(new Point(_stabilisator.GetPosition().X + _acceleration.GetWidth() + _gap, _stabilisator.GetPosition().Y));
        }

        public override int GetHeight()
        {
            return _stabilisator.GetHeight();
        }

        public override int GetWidth()
        {
            var width = 0;
            width += _stabilisator.GetWidth();
            width += _acceleration.GetWidth() + _gap;
            return width;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
        {
            _stabilisator.Draw();
            _acceleration.Draw();
        }

        #endregion
    }
}
