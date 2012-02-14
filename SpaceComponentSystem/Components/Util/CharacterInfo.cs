using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Space.Data;
using Engine.ComponentSystem.Entities;

namespace Space.ComponentSystem.Components.Util
{
    public class CharacterInfo : AbstractComponent
    {

        public int InventoryCount()
        {
            var inventory = Entity.GetComponent<SpaceInventory>();
            if (inventory != null)
            {
                return inventory.Count();
            }
            return 0;
        }
        public int EquipmentCount<Item>()
            where Item:Item<AttributeType>
        {
            var equipment = Entity.GetComponent<Equipment>();
            if (equipment != null)
            {
                return equipment.GetSlotCount<Item,AttributeType>();
            }
            return 0;
        }

        public Entity InventoryItemAt(int index){
             var inventory = Entity.GetComponent<SpaceInventory>();
            if (inventory != null)
            {
                return inventory[index];
            }
            return null;
        }
        public Entity EquipmentItemAt<Item>(int index)
            where Item:Item<AttributeType>
        {
            var equipment = Entity.GetComponent<Equipment>();
            if (equipment != null)
            {
                return equipment.GetItem<Item,AttributeType>(index);
            }
            return null;
        }
    }
}
