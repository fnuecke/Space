using System;
using Engine.ComponentSystem.Messages;
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
        /// <summary>
        /// Check for removed entities to remove them from inventories.
        /// </summary>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is ComponentRemoved)
            {
                // If a component was removed from the game and it was an item
                // remove it from all known inventories.
                var removed = (ComponentRemoved)(ValueType)message;
                if (removed.Component is Item)
                {
                    foreach (var component in Components)
                    {
                        component.TryUnequip(removed.Component.Entity);
                    }
                }
                else if (removed.Component is Equipment)
                {
                    foreach (var item in ((Equipment)removed.Component).AllItems)
                    {
                        Manager.RemoveEntity(item);
                    }
                }
            }
        }
    }
}
