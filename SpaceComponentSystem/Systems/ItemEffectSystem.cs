using System;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system reacts to equipment changes and adjusts thruster effects accordingly.
    /// </summary>
    public sealed class ItemEffectSystem : AbstractSystem, IMessagingSystem
    {
        #region Implementation of IMessagingSystem

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
        {
            if (message is ItemEquipped)
            {
                var equipped = (ItemEquipped)(ValueType)message;

                // Check if we can show effects.
                var effects = (ParticleEffects)Manager.GetComponent(equipped.Slot.Root.Entity, ParticleEffects.TypeId);
                if (effects == null)
                {
                    return;
                }

                // Get the item to get its size (for scaling offset and effect).
                var item = (SpaceItem)Manager.GetComponent(equipped.Item, Item.TypeId);

                // OK, add the effects, if there are any.
                foreach (ItemEffect effect in Manager.GetComponents(equipped.Item, ItemEffect.TypeId))
                {
                    var slot = (SpaceItemSlot)equipped.Slot;
                    var offset = effect.Offset;
                    offset.X = item.RequiredSlotSize.Scale(offset.X);
                    offset.Y = item.RequiredSlotSize.Scale(offset.Y);
                    effects.TryAdd(effect.Id, effect.Name, item.RequiredSlotSize.Scale(effect.Scale), effect.Direction,
                        slot.AccumulateOffset(offset), effect.Group, effect.Group == ParticleEffects.EffectGroup.None);
                }
            }
            else if (message is ItemUnequipped)
            {
                var unequipped = (ItemUnequipped)(ValueType)message;

                // Check if we can show effects.
                var effects = (ParticleEffects)Manager.GetComponent(unequipped.Slot.Root.Entity, ParticleEffects.TypeId);
                if (effects == null)
                {
                    return;
                }
                
                // OK, remove the effects, if there are any.
                foreach (ItemEffect effect in Manager.GetComponents(unequipped.Item, ItemEffect.TypeId))
                {
                    effects.Remove(effect.Id);
                }
            }
        }

        #endregion
    }
}
