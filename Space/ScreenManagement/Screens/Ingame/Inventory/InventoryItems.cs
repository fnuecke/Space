using Engine.ComponentSystem.RPG.Components;
using Nuclex.Input;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Hud;
using Space.Simulation.Commands;

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
            Source = Source.Inventory;
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

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Right)
            {
                //if no item selected and right click on item use item
                if (!_itemSelection.ItemIsSelected)
                {
                    for (int i = 0; i < DataCount(); i++)
                    {
                        if (IsMousePositionOnIcon(i))
                        {
                           _client.Controller.PushLocalCommand(new UseCommand(i));
                           return true;
                        }
                        
                    }
                }
            }


            return base.DoHandleMousePressed(buttons);
        }
    }
}
