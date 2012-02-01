using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Nuclex.Input;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    class HudInputHandlerTest : AHudElement
    {

        public int toggled = 0;
        private IngameScreen _gameplayScreen;

        #region Fields

        private const int StandardGap = 6;

        private HudBuffElement _test;

        private int _gap; 

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudInputHandlerTest(GameClient client, IngameScreen gameplayScreen)
            : base(client)
        {
            _test = new HudBuffElement(client);
            _gameplayScreen = gameplayScreen;
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

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

                _test.SetPosition(new Point(currentX, currentY));
                currentX += _test.GetWidth() + _gap;
            }
        }

        public override int GetHeight()
        {
            return _test.GetHeight();
        }

        public override int GetWidth()
        {
            var width = 0;
            var count = 0;

            width += _test.GetWidth();
            count++;

            width += (count - 1) * _gap;

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

            _test.Draw();
        }

        #endregion

        #region Methods

        public void toggleIt()
        {
            toggled++;
            if (toggled > 2)
            {
                toggled = 0;
            }

            if (toggled == 0)
            {
                _test.SetMode(HudIcon.Mode.Buff);
            }
            else if (toggled == 1)
            {
                _test.SetMode(HudIcon.Mode.Neutral);
            }
            else // if (toggled == 2)
            {
                _test.SetMode(HudIcon.Mode.Debuff);
            }

        }

        #endregion
    }
}
