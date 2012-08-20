using System;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system reacts to equipment changes and adjusts thruster effects accordingly.
    /// </summary>
    public sealed class ThrusterEffectSystem : AbstractSystem, IMessagingSystem
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

                // Check if it's a thruster.
                var thruster = (Thruster)Manager.GetComponent(equipped.Item, Thruster.TypeId);
                if (thruster == null)
                {
                    return;
                }

                // Check if we can show effects.
                var effects = (ParticleEffects)Manager.GetComponent(equipped.Slot.Root.Entity, ParticleEffects.TypeId);
                if (effects == null)
                {
                    return;
                }

                // OK, add the effect.
                var slot = (SpaceItemSlot)equipped.Slot;
                effects.TryAdd(thruster.Effect, thruster.EffectOffset + slot.AccumulateOffset(), ParticleEffects.EffectGroup.Thrusters);
            }
            else if (message is ItemUnequipped)
            {
                var unequipped = (ItemUnequipped)(ValueType)message;

                // Check if it's a thruster.
                var thruster = (Thruster)Manager.GetComponent(unequipped.Item, Thruster.TypeId);
                if (thruster == null)
                {
                    return;
                }

                // Check if we can show effects.
                var effects = (ParticleEffects)Manager.GetComponent(unequipped.Slot.Root.Entity, ParticleEffects.TypeId);
                if (effects == null)
                {
                    return;
                }

                // OK, remove the effect.
                var slot = (SpaceItemSlot)unequipped.Slot;
                effects.Remove(thruster.Effect, thruster.EffectOffset + slot.AccumulateOffset(), ParticleEffects.EffectGroup.Thrusters);
            }
        }

        #endregion
    }
}
