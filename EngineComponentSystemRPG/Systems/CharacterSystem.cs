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
    public sealed class CharacterSystem<TAttribute> : AbstractComponentSystem<Character<TAttribute>>
        where TAttribute : struct
    {
        /// <summary>
        /// Handles adds to trigger recomputation of modified attribute
        /// values.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        protected override void OnComponentAdded(Character<TAttribute> component)
        {
            component.RecomputeAttributes();
        }

        /// <summary>
        /// Handles messages to trigger recomputation of modified attribute
        /// values.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);
            
            if (message is ItemEquipped)
            {
                // Recompute if an item with attribute modifiers was added.
                var added = (ItemEquipped)(ValueType)message;
                if (Manager.GetComponent(added.Item, Attribute<TAttribute>.TypeId) != null)
                {
                    var character = ((Character<TAttribute>)Manager.GetComponent(added.Entity, Character<TAttribute>.TypeId));
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
                    var character = ((Character<TAttribute>)Manager.GetComponent(removed.Entity, Character<TAttribute>.TypeId));
                    if (character != null)
                    {
                        character.RecomputeAttributes();   
                    }
                }
            }
        }
    }
}
