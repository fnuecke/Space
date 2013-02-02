using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>
    ///     Handles removing entities from equipment slots when they are removed from the system, as well as removing
    ///     items from the system in equipment slots when an equipment component is removed.
    /// </summary>
    public sealed class ItemSlotSystem : AbstractComponentSystem<ItemSlot>
    {
        #region Logic

        /// <summary>Check for removed entities to remove them from inventories.</summary>
        /// <param name="message"></param>
        public override void OnComponentRemoved(ComponentRemoved message)
        {
            base.OnComponentRemoved(message);

            var item = message.Component as Item;
            if (item != null)
            {
                // Remove items that were equipped inside this item. Do this first,
                // to keep the hierarchy alive as long as possible (due to this
                // recursing this will delete bottom up). This is necessary to allow
                // other systems to check the root node (e.g. thruster effect system).
                Component slot;
                while ((slot = Manager.GetComponent(item.Entity, ItemSlot.TypeId)) != null)
                {
                    Manager.RemoveComponent(slot);
                }
            }
            else
            {
                var itemSlot = message.Component as ItemSlot;
                if (itemSlot != null)
                {
                    // An equipment was removed, remove its item. This will
                    // also recursively remove the sub tree (as the removed
                    // item will remove its item slots, etc.)
                    var slot = itemSlot;
                    if (slot.Item > 0)
                    {
                        Manager.RemoveEntity(slot.Item);
                    }
                }
            }
        }

        /// <summary>Unequips items that were removed from the simulation.</summary>
        /// <param name="message"></param>
        [MessageCallback]
        public void OnEntityRemoved(EntityRemoved message)
        {
            // An item was removed, unequip it everywhere.
            foreach (var slot in Components)
            {
                if (slot.Item == message.Entity)
                {
                    slot.Item = 0;
                }
            }
        }

        #endregion
    }
}