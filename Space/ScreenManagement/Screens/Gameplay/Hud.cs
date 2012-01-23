using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;

namespace Space.ScreenManagement.Screens.Gameplay
{
    class Hud
    {

        #region Fields

        /// <summary>
        /// The current content manager.
        /// </summary>
        private ContentManager _content;

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The life energy bar.
        /// </summary>
        private HealthEnergyBar _healthEnergyBar;

        private HudRadio _hudRadioBox;
        private HudPlayerList _hudPlayerList;
        private HudBuffElement _hudIconBar;

        private int _gap = 20;

        #endregion

        #region Constructor

        public Hud(GameClient client)
        {
            _client = client;
            _healthEnergyBar = new HealthEnergyBar(_client);
            _hudRadioBox = new HudRadio(_client);
            _hudPlayerList = new HudPlayerList(_client);
            _hudIconBar = new HudBuffElement(_client);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;

            // init the health & energy bar
            _healthEnergyBar.LoadContent(spriteBatch, content);
            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            _healthEnergyBar.SetPosition(new Point((viewport.Width - _healthEnergyBar.GetWidth()) / 2, (viewport.Height - _healthEnergyBar.GetHeight()) / 2 - 40));

            // init the radio box
            _hudRadioBox.LoadContent(spriteBatch, content);
            _hudRadioBox.SetPosition(new Point(60, 155));
            _hudRadioBox.setName("Guybrush Threepwood");
            _hudRadioBox.setTitle("Pirate");

            // init the player box
            _hudPlayerList.LoadContent(spriteBatch, content);
            _hudPlayerList.SetPosition(new Point(viewport.Width - _hudPlayerList.GetWidth() - 60, 60));

            _hudIconBar.LoadContent(spriteBatch, content);
            _hudIconBar.SetSize(new Point(50, 50));
        }

        #endregion

        #region Update & Drawing

        /// <summary>
        /// Updates the data of the HUD elements
        /// </summary>
        public void Update()
        {
            var viewport = _spriteBatch.GraphicsDevice.Viewport;

            var info = _client.GetPlayerShipInfo();
            if (info == null)
            {
                return;
            }

            _healthEnergyBar.SetMaximumEnergy((int) info.MaxEnergy);
            _healthEnergyBar.SetCurrentEnergy((int) info.Energy);
            _healthEnergyBar.SetMaximumHealth((int) info.MaxHealth);
            _healthEnergyBar.SetCurrentHealth((int) info.Health);

            _hudPlayerList.Update();

            _hudIconBar.SetPosition(new Point(viewport.Width - _hudIconBar.GetWidth() - 60, _hudPlayerList.GetPosition().Y + _hudPlayerList.GetHeight() + _gap));
        }

        /// <summary>
        /// Render the current health / energy bar with the current values.
        /// </summary>
        public void Draw()
        {
            // draw the health & energy bar
            var info = _client.GetPlayerShipInfo();
            if (info != null && info.IsAlive)
            {
                var offset = _client.GetPlayerShipInfo().Position - _client.GetCameraPosition();
                var viewport = _spriteBatch.GraphicsDevice.Viewport;
                _healthEnergyBar.SetPosition(new Point((viewport.Width - _healthEnergyBar.GetWidth()) / 2 + (int)offset.X, (viewport.Height - _healthEnergyBar.GetHeight()) / 2 - 40 + (int)offset.Y));
                _healthEnergyBar.Draw();
            }
            _hudRadioBox.Draw();
            _hudPlayerList.Draw();

            _hudIconBar.Draw();
        }

        #endregion

    }
}
