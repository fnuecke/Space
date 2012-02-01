using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Ingame.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A sample file that can be used as a kind of template to create a new
    /// object for a new HUD element.
    /// 
    /// It is NOT intended to be used in the hud!
    /// </summary>
    class HudBuffBar : AbstractHudElement
    {

        #region Fields

        private const int StandardGap = 6;

        private HudBuffElement _stabilisator;
        private HudBuffElement _acceleration;
        private HudBuffElement _test;

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
            _test = new HudBuffElement(client);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _stabilisator.LoadContent(spriteBatch, content);
            _stabilisator.SetSize(new Point(44, 44));
            _stabilisator.SetImage("Textures/Icons/Buffs/stabilisator");
            _stabilisator.SetMode(HudIcon.Mode.Buff);

            _acceleration.LoadContent(spriteBatch, content);
            _acceleration.SetSize(new Point(44, 44));
            _acceleration.SetImage("Textures/Icons/Buffs/acceleration");
            _acceleration.SetMode(HudIcon.Mode.Neutral);

            _test.LoadContent(spriteBatch, content);
            _test.SetSize(new Point(44, 44));
            _test.SetMode(HudIcon.Mode.Debuff);

            _gap = StandardGap;
            SetPosition(GetPosition());
        }

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);

            var info = _client.GetPlayerShipInfo();

            if (info != null) {
                var currentX = position.X;
                var currentY = position.Y;

                if (info.IsStabilizing)
                {
                    _stabilisator.SetPosition(new Point(currentX, currentY));
                    currentX += _stabilisator.GetWidth() + _gap;
                }

                if (info.IsAccelerating)
                {
                    _acceleration.SetPosition(new Point(currentX, currentY));
                    currentX += _acceleration.GetWidth() + _gap;
                }

                _test.SetPosition(new Point(currentX, currentY));
                currentX += _test.GetWidth() + _gap;
            }
        }

        public override int GetHeight()
        {
            return _stabilisator.GetHeight();
        }

        public override int GetWidth()
        {
            var width = 0;
            var count = 0;
            if (_client.GetPlayerShipInfo().IsStabilizing)
            {
                width += _stabilisator.GetWidth();
                count++;
            }

            if (_client.GetPlayerShipInfo().IsAccelerating)
            {
                width += _acceleration.GetWidth();
                count++;
            }

            width += _test.GetWidth();
            count++;


            width += (count - 1) * _gap;

            if (width < 0)
            {
                width = 0;
            }

            return width;
        }

        #endregion

        #region Update & Draw


        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
        {
            var info = _client.GetPlayerShipInfo();
            if (info == null)
            {
                return;
            }

            if (info.IsStabilizing)
            {
                _stabilisator.Draw();
            }

            if (info.IsAccelerating)
            {
                _acceleration.Draw();
            }

            _test.Draw();
        }

        #endregion
    }
}
