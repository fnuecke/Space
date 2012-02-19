using System;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    public sealed class CharacterSystem<TAttribute> : AbstractComponentSystem<Character<TAttribute>>
        where TAttribute : struct
    {
        /// <summary>
        /// Handles messages to trigger recomputation of modified attribute
        /// values.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);
            
            if (message is EntityAdded && ((EntityAdded)(ValueType)message).Entity == Entity)
            {
                RecomputeAttributes();
            }
            // Only handle local commands if we're part of the system.
            else if (Entity.Manager != null)
            {
                if (message is ItemAdded)
                {
                    // Recompute if an item with attribute modifiers was added.
                    var added = (ItemAdded)(ValueType)message;
                    if (added.Item.GetComponent<Attribute<TAttribute>>() != null)
                    {
                        RecomputeAttributes();
                    }
                }
                else if (message is ItemRemoved)
                {
                    // Recompute if an item with attribute modifiers was removed.
                    var removed = (ItemRemoved)(ValueType)message;
                    if (removed.Item.GetComponent<Attribute<TAttribute>>() != null)
                    {
                        RecomputeAttributes();
                    }
                }
                else if (message is ComponentAdded)
                {
                    // Recompute if a status effect with attribute modifiers was added.
                    var added = (ComponentAdded)(ValueType)message;
                    if (added.Component is AttributeStatusEffect<TAttribute> || added.Component == this)
                    {
                        RecomputeAttributes();
                    }
                }
                else if (message is ComponentRemoved)
                {
                    // Recompute if a status effect with attribute modifiers was removed.
                    var removed = (ComponentRemoved)(ValueType)message;
                    if (removed.Component is AttributeStatusEffect<TAttribute>)
                    {
                        RecomputeAttributes();
                    }
                }
            }
        }
    }
}
