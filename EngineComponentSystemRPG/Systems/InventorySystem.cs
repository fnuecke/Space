using System;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    public sealed class InventorySystem : AbstractComponentSystem<Inventory>
    {
        /// <summary>
        /// Check for removed entities to remove them from inventories.
        /// </summary>
        public override void Receive<T>(ref T message)
        {
            base.Receive<T>(ref message);

            if (message is EntityRemoved)
            {
                // If an entity was removed from the game and it was in this
                // inventory, remove it here, too.
                var removed = (EntityRemoved)(ValueType)message;
                foreach (var component in Components)
                {
                    component.Remove(removed.Entity);
                }
            }
        }
    }
}
