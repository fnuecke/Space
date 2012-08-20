using System;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>
    /// Handles keeping modified character attributes up-to-date.
    /// </summary>
    /// <typeparam name="TAttribute">Possible attribute values.</typeparam>
    public sealed class CharacterSystem<TAttribute> : AbstractSystem, IMessagingSystem
        where TAttribute : struct
    {
        #region Logic
        
        /// <summary>
        /// Called by the manager when a new component was added.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(ComponentSystem.Components.Component component)
        {
            // Check if the component is of the right type.
            if (component is Character<TAttribute>)
            {
                ((Character<TAttribute>)component).RecomputeAttributes();
            }
        }

        /// <summary>
        /// Handles messages to trigger recomputation of modified attribute
        /// values.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
        {
            if (message is ItemEquipped)
            {
                // Recompute if an item with attribute modifiers was added.
                var added = (ItemEquipped)(ValueType)message;
                if (Manager.GetComponent(added.Item, Attribute<TAttribute>.TypeId) != null)
                {
                    var character = ((Character<TAttribute>)Manager.GetComponent(added.Slot.Root.Entity, Character<TAttribute>.TypeId));
                    if (character != null)
                    {
                        character.RecomputeAttributes();
                    }
                }
            }
            else if (message is ItemUnequipped)
            {
                // Recompute if an item with attribute modifiers was removed.
                var removed = (ItemUnequipped)(ValueType)message;
                if (Manager.GetComponent(removed.Item, Attribute<TAttribute>.TypeId) != null)
                {
                    var character = ((Character<TAttribute>)Manager.GetComponent(removed.Slot.Root.Entity, Character<TAttribute>.TypeId));
                    if (character != null)
                    {
                        character.RecomputeAttributes();   
                    }
                }
            }
        }

        #endregion
    }
}
