using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system reacts to equipment changes and adjusts thruster effects accordingly.</summary>
    public sealed class ItemEffectSystem : AbstractSystem, IMessagingSystem
    {
        #region Implementation of IMessagingSystem

        /// <summary>Handle a message of the specified type.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as ItemEquipped?;
                if (cm != null)
                {
                    var m = cm.Value;

                    // Check if we can show effects.
                    var effects = (ParticleEffects) Manager.GetComponent(m.Slot.Root.Entity, ParticleEffects.TypeId);
                    if (effects == null)
                    {
                        return;
                    }

                    // Get the item to get its size (for scaling offset and effect).
                    var item = (SpaceItem) Manager.GetComponent(m.Item, Item.TypeId);

                    // OK, add the effects, if there are any.
                    foreach (ItemEffect effect in Manager.GetComponents(m.Item, ItemEffect.TypeId))
                    {
                        var slot = (SpaceItemSlot) m.Slot;
                        var offset = effect.Offset;
                        var direction = effect.Direction;
                        offset *= item.RequiredSlotSize.Scale();
                        slot.Accumulate(ref offset, ref direction);
                        effects.TryAdd(
                            effect.Id,
                            effect.Name,
                            item.RequiredSlotSize.Scale() * effect.Scale,
                            direction,
                            offset,
                            effect.Group,
                            effect.Group == ParticleEffects.EffectGroup.None);
                    }
                    return;
                }
            }
            {
                var cm = message as ItemUnequipped?;
                if (cm != null)
                {
                    var m = cm.Value;

                    // Check if we can show effects.
                    var effects = (ParticleEffects) Manager.GetComponent(m.Slot.Root.Entity, ParticleEffects.TypeId);
                    if (effects == null)
                    {
                        return;
                    }

                    // OK, remove the effects, if there are any.
                    foreach (ItemEffect effect in Manager.GetComponents(m.Item, ItemEffect.TypeId))
                    {
                        effects.Remove(effect.Id);
                    }
                }
            }
        }

        #endregion
    }
}