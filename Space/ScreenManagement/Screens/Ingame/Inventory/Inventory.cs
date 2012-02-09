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
using Microsoft.Xna.Framework.Input;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{

    class Inventory : AbstractGuiElement
    {
        /// <summary>
        /// The global item selection manager.
        /// </summary>
        ItemSelectionManager _itemSelection;

        /// <summary>
        /// The dynamic item list object.
        /// </summary>
        DynamicItemList _list;

        /// <summary>
        /// Constructor
        /// </summary>
        public Inventory(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager)
            : base(client)
        {
            _itemSelection = itemSelection;

            _list = new DynamicItemList(client, itemSelection, textureManager, DynamicItemList.Modes.Inventory);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;

            _list.LoadContent(spriteBatch, content);
        }

        public override void Draw()
        {
            if (Visible) {
                _spriteBatch.Begin();
                _basicForms.FillRectangle((int)GetPosition().X, (int)GetPosition().Y, (int)GetWidth(), (int)GetHeight(), Color.Black * 0.6f);
                _spriteBatch.End();

                _list.Draw();
            }
        }

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);
            _list.SetPosition(x + 5, y + 5);
        }

        #region Listener

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            if (!IsMouseClickedInElement())
            {
                return false;
            }
            _list.DoHandleMousePressed(buttons);
            return true;
        }

        public override bool DoHandleMouseReleased(MouseButtons buttons)
        {
            if (!IsMouseClickedInElement())
            {
                return false;
            }
            _list.DoHandleMouseReleased(buttons);
            return true;
        }

        #endregion

    }
}
