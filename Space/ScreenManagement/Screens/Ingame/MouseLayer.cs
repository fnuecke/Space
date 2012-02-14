using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Space.ScreenManagement.Screens.Ingame
{
    class MouseLayer : AbstractGuiElement
    {

        /// <summary>
        /// The basic item manager object.
        /// </summary>
        private ItemSelectionManager _itemManager;

        private TextureManager _textureManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="itemManager">The basic item manager.</param>
        public MouseLayer(GameClient client, ItemSelectionManager itemManager, TextureManager textureManager)
            : base(client)
        {
            _itemManager = itemManager;
            _textureManager = textureManager;
        }

        public override void Draw()
        {
            _spriteBatch.Begin();

            if (_itemManager.SelectedIcon != null)
            {
                _spriteBatch.Draw(_textureManager.Get(_itemManager.SelectedIcon), new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 35, 35), Color.White);
            }

            _spriteBatch.End();
        }
    }
}
