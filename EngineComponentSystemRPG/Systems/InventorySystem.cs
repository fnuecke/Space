using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>
    ///     Handles removing entities from inventories when they are removed from the system, as well as removing items
    ///     from the system in inventory slots when an inventory component is removed.
    /// </summary>
    public sealed class InventorySystem : AbstractComponentSystem<Inventory>
    {
        #region Logic

        /// <summary>Check for removed entities to remove them from inventories.</summary>
        /// <param name="component">The removed component.</param>
        public override void OnComponentRemoved(IComponent component)
        {
            base.OnComponentRemoved(component);

            if (component is Item)
            {
                // An item was removed, remove it from all inventories.
                foreach (var inventory in Components)
                {
                    inventory.Remove(inventory.Entity);
                }
            }
            else
            {
                var inventory = component as Inventory;
                if (inventory != null)
                {
                    // An inventory was removed, remove all items in it.
                    foreach (var item in inventory)
                    {
                        Manager.RemoveEntity(item);
                    }
                }
            }
        }

        #endregion
    }
}