using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>
    /// Handles removing entities from equipment slots when they are removed from
    /// the system, as well as removing items from the system in equipment slots
    /// when an equipment component is removed.
    /// </summary>
    public sealed class ItemSlotSystem : AbstractComponentSystem<ItemSlot>
    {
        #region Logic
        
        /// <summary>
        /// Check for removed entities to remove them from inventories.
        /// </summary>
        /// <param name="component">The removed component.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            if (component is Item)
            {
                // An item was removed, unequip it everywhere.
                foreach (var slot in Components)
                {
                    if (slot.Item == component.Entity)
                    {
                        slot.Item = 0;
                    }
                }
            }
            else if (component is ItemSlot)
            {
                // An equipment was removed, remove its item. This will
                // also recursively remove the sub tree (as the removed
                // item will remove its item slots, etc.)
                var item = ((ItemSlot)component).Item;
                if (item > 0)
                {
                    Manager.RemoveEntity(item);
                }
            }
        }

        #endregion
    }
}
