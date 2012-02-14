using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.ScreenManagement.Screens.Ingame.Hud;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.Data;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ScreenManagement.Screens.Ingame.Inventory
{
    class InventoryItems : AbstractDynamicItemList
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public InventoryItems(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager)
            : base(client, itemSelection, textureManager)
        {
        }

        public override int DataCount()
        {
            return _client.GetCharacterInfo().InventoryCount();
        }

        public override Item<AttributeType> ItemAt(int id)
        {
            if (_client.GetCharacterInfo().InventoryItemAt(id) == null)
            {
                return null;
            }
            return _client.GetCharacterInfo().InventoryItemAt(id).GetComponent<Item<AttributeType>>();
        }
    }
}
