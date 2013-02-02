using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>Handles keeping modified character attributes up-to-date.</summary>
    /// <typeparam name="TAttribute">Possible attribute values.</typeparam>
    public sealed class CharacterSystem<TAttribute> : AbstractSystem
        where TAttribute : struct
    {
        #region Logic

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="message"></param>
        [MessageCallback]
        public void OnComponentAdded(ComponentAdded message)
        {
            // Check if the component is of the right type.
            var attributes = message.Component as Attributes<TAttribute>;
            if (attributes != null)
            {
                attributes.RecomputeAttributes();
            }
        }

        /// <summary>Recompute if an item with attribute modifiers was added.</summary>
        [MessageCallback]
        public void OnItemEquipped(ItemEquipped message)
        {
            if (Manager.GetComponent(message.Item, Attribute<TAttribute>.TypeId) != null)
            {
                var attributes =
                    ((Attributes<TAttribute>)
                        Manager.GetComponent(message.Slot.Root.Entity, Attributes<TAttribute>.TypeId));
                if (attributes != null)
                {
                    attributes.RecomputeAttributes();
                }
            }
        }

        /// <summary>Recompute if an item with attribute modifiers was removed.</summary>
        [MessageCallback]
        public void OnItemUnequipped(ItemUnequipped message)
        {
            if (Manager.GetComponent(message.Item, Attribute<TAttribute>.TypeId) != null)
            {
                var attributes =
                    ((Attributes<TAttribute>)
                        Manager.GetComponent(message.Slot.Root.Entity, Attributes<TAttribute>.TypeId));
                if (attributes != null)
                {
                    attributes.RecomputeAttributes();
                }
            }
        }

        #endregion
    }
}