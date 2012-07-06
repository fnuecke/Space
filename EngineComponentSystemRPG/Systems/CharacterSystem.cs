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
            
            if (message is ItemAdded)
            {
                // Recompute if an item with attribute modifiers was added.
                var added = (ItemAdded)(ValueType)message;
                if (Manager.GetComponent<Attribute<TAttribute>>(added.Item) != null)
                {
                    var character = Manager.GetComponent<Character<TAttribute>>(added.Entity);
                    if (character != null)
                    {
                        character.RecomputeAttributes();
                    }
                }
            }
            else if (message is ItemRemoved)
            {
                // Recompute if an item with attribute modifiers was removed.
                var removed = (ItemRemoved)(ValueType)message;
                if (Manager.GetComponent<Attribute<TAttribute>>(removed.Item) != null)
                {
                    var character = Manager.GetComponent<Character<TAttribute>>(removed.Entity);
                    if (character != null)
                    {
                        character.RecomputeAttributes();   
                    }
                }
            }
        }
    }
}
