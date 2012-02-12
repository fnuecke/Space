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
        public InventoryItems(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager, Modes mode)
            : base(client, itemSelection, textureManager, mode)
        {
        }

        public override Item<AttributeType> ItemAt(int id)
        {
            if (_client.GetInventory()[id] == null)
            {
                return null;
            }
            return _client.GetInventory()[id].GetComponent<Item<AttributeType>>();
        }
    }
}
