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
            return _client.GetPlayerShipInfo().InventoryCapacity;
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
