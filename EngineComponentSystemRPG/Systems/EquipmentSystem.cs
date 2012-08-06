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
    public sealed class EquipmentSystem : AbstractComponentSystem<Equipment>
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
                foreach (var equipment in Components)
                {
                    equipment.TryUnequip(component.Entity);
                }
            }
            else if (component is Equipment)
            {
                // An equipment was removed, remove all items still
                // held by it.
                foreach (var item in ((Equipment)component).AllItems)
                {
                    Manager.RemoveEntity(item);
                }
            }
        }

        #endregion
    }
}
