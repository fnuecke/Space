using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;
using Engine.Util;

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

        #endregion

        #region Constructor

        public Hud(GameClient client)
        {
            _client = client;
            _healthEnergyBar = new HealthEnergyBar(_client);

        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _content = content;
            _spriteBatch = spriteBatch;

            _healthEnergyBar.LoadContent(spriteBatch, content);

            // initialize the health & energy with standard values
            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            _healthEnergyBar.SetPosition(new Point((viewport.Width - _healthEnergyBar.GetWidth()) / 2, (viewport.Height - _healthEnergyBar.GetHeight()) / 2 - 40));
        }

        #endregion

        #region Update & Drawing

        /// <summary>
        /// Updates the data of the HUD elements
        /// </summary>
        public void Update()
        {
            var info = _client.GetPlayerShipInfo();
            _healthEnergyBar.SetMaximumEnergy((int) info.MaxEnergy);
            _healthEnergyBar.SetCurrentEnergy((int) info.Energy);
            _healthEnergyBar.SetMaximumHealth((int) info.MaxHealth);
            _healthEnergyBar.SetCurrentHealth((int) info.Health);
        }
        /// <summary>
        /// Render the current health / energy bar with the current values.
        /// </summary>
        public void Draw()
        {
            // draw the health & energy bar
            _healthEnergyBar.Draw();
        }

        #endregion

    }
}
