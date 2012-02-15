using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Components;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Hud;

namespace Space.ScreenManagement.Screens.Ingame.Inventory
{
    class EquipmentItems<T> : AbstractDynamicItemList
        where T : SpaceItem
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
            return _client.GetPlayerShipInfo().EquipmentSlotCount<T>();
        }

        public override Item ItemAt(int id)
        {
            if (_client.GetPlayerShipInfo().EquipmentItemAt<T>(id) == null)
            {
                return null;
            }
            return _client.GetPlayerShipInfo().EquipmentItemAt<T>(id).GetComponent<Item>();
        }

    }
}
