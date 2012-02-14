using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.ScreenManagement.Screens.Ingame.Hud;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.Data;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Entities;

namespace Space.ScreenManagement.Screens.Ingame.Inventory
{
    class EquipmentItems<T> : AbstractDynamicItemList
        where T : Item<AttributeType>
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public EquipmentItems(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager)
            : base(client, itemSelection, textureManager)
        {
        }

        public override int DataCount()
        {
            return _client.GetCharacterInfo().EquipmentCount<T>();
        }

        public override Item<AttributeType> ItemAt(int id)
        {
            if (_client.GetCharacterInfo().InventoryItemAt(id).GetComponent<Item<AttributeType>>() == null)
            {
                return null;
            }
            return _client.GetCharacterInfo().InventoryItemAt(id).GetComponent<Item<AttributeType>>();
        }

    }
}
