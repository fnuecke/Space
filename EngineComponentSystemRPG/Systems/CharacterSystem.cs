using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>Handles keeping modified character attributes up-to-date.</summary>
    /// <typeparam name="TAttribute">Possible attribute values.</typeparam>
    public sealed class CharacterSystem<TAttribute> : AbstractSystem, IMessagingSystem
        where TAttribute : struct
    {
        #region Logic

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(ComponentSystem.Components.Component component)
        {
            // Check if the component is of the right type.
            if (component is Attributes<TAttribute>)
            {
                ((Attributes<TAttribute>) component).RecomputeAttributes();
            }
        }

        /// <summary>Handles messages to trigger recomputation of modified attribute values.</summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as ItemEquipped?;
                if (cm != null)
                {
                    // Recompute if an item with attribute modifiers was added.
                    var m = cm.Value;
                    if (Manager.GetComponent(m.Item, Attribute<TAttribute>.TypeId) != null)
                    {
                        var attributes =
                            ((Attributes<TAttribute>)
                             Manager.GetComponent(m.Slot.Root.Entity, Attributes<TAttribute>.TypeId));
                        if (attributes != null)
                        {
                            attributes.RecomputeAttributes();
                        }
                    }
                    return;
                }
            }
            {
                var cm = message as ItemUnequipped?;
                if (cm != null)
                {
                    // Recompute if an item with attribute modifiers was removed.
                    var m = cm.Value;
                    if (Manager.GetComponent(m.Item, Attribute<TAttribute>.TypeId) != null)
                    {
                        var attributes =
                            ((Attributes<TAttribute>)
                             Manager.GetComponent(m.Slot.Root.Entity, Attributes<TAttribute>.TypeId));
                        if (attributes != null)
                        {
                            attributes.RecomputeAttributes();
                        }
                    }
                }
            }
        }

        #endregion
    }
}