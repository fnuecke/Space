using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system reacts to equipment changes and adjusts thruster effects accordingly.</summary>
    public sealed class ItemEffectSystem : AbstractSystem
    {
        #region Implementation of IMessagingSystem

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<ItemEquipped>(OnItemEquipped);
            Manager.AddMessageListener<ItemUnequipped>(OnItemUnequipped);
        }

        private void OnItemEquipped(ItemEquipped message)
        {
            // Check if we can show effects.
            var effects = (ParticleEffects) Manager.GetComponent(message.Slot.Root.Entity, ParticleEffects.TypeId);
            if (effects == null)
            {
                return;
            }

            // Get the item to get its size (for scaling offset and effect).
            var item = (SpaceItem) Manager.GetComponent(message.Item, Item.TypeId);

            // OK, add the effects, if there are any.
            foreach (ItemEffect effect in Manager.GetComponents(message.Item, ItemEffect.TypeId))
            {
                var slot = (SpaceItemSlot) message.Slot;
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
        }

        private void OnItemUnequipped(ItemUnequipped message)
        {
            // Check if we can show effects.
            var effects = (ParticleEffects) Manager.GetComponent(message.Slot.Root.Entity, ParticleEffects.TypeId);
            if (effects == null)
            {
                return;
            }

            // OK, remove the effects, if there are any.
            foreach (ItemEffect effect in Manager.GetComponents(message.Item, ItemEffect.TypeId))
            {
                effects.Remove(effect.Id);
            }
        }

        #endregion
    }
}