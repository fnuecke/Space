using Engine.ComponentSystem.RPG.Components;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Hud;

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
            var info = _client.GetPlayerShipInfo();
            if (info != null)
            {
                return info.InventoryCapacity;
            }
            else
            {
                return 0;
            }
        }

        public override Item ItemAt(int id)
        {
            if (_client.GetPlayerShipInfo().InventoryItemAt(id) == null)
            {
                return null;
            }
            return _client.GetPlayerShipInfo().InventoryItemAt(id).GetComponent<Item>();
        }
    }
}
