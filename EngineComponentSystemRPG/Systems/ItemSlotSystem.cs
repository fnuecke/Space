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
                // Remove items that were equipped inside this item. Do this first,
                // to keep the hierarchy alive as long as possible (due to this
                // recursing this will delete bottom up). This is necessary to allow
                // other systems to check the root node (e.g. thruster effect tsystem).
                Component slot;
                while ((slot = Manager.GetComponent(component.Entity, ItemSlot.TypeId)) != null)
                {
                    Manager.RemoveComponent(slot);
                }
            }
            else if (component is ItemSlot)
            {
                // An equipment was removed, remove its item. This will
                // also recursively remove the sub tree (as the removed
                // item will remove its item slots, etc.)
                var slot = (ItemSlot)component;
                if (slot.Item > 0)
                {
                    Manager.RemoveEntity(slot.Item);
                }
            }
        }

        /// <summary>
        /// Unequips items that were removed from the simulation.
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        public override void OnEntityRemoved(int entity)
        {
            base.OnEntityRemoved(entity);

            // An item was removed, unequip it everywhere.
            foreach (var slot in Components)
            {
                if (slot.Item == entity)
                {
                    slot.Item = 0;
                }
            }
        }

        #endregion
    }
}
