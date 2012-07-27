using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Components.Logic;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles the death of entities.
    /// </summary>
    public sealed class DeathSystem : AbstractComponentSystem<Respawn>
    {
        /// <summary>
        /// Checks for entities to respawn.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, Respawn component)
        {
            if (component.TimeToRespawn <= 0 || --component.TimeToRespawn != 0)
            {
                return;
            }

            // Respawn.

            // Try to position.
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));
            if (transform != null)
            {
                transform.SetTranslation(ref component.Position);
                transform.SetRotation(0);
            }

            // Kill of remainder velocity.
            var velocity = ((Velocity)Manager.GetComponent(component.Entity, Velocity.TypeId));
            if (velocity != null)
            {
                velocity.Value = Vector2.Zero;
            }

            // Fill up health / energy.
            var health = ((Health)Manager.GetComponent(component.Entity, Health.TypeId));
            if (health != null)
            {
                health.SetValue(health.MaxValue * component.RelativeHealth);
            }
            var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
            if (energy != null)
            {
                energy.SetValue(energy.MaxValue * component.RelativeEnergy);
            }

            // Enable components.
            foreach (var componentType in component.ComponentsToDisable)
            {
                Manager.GetComponent(component.Entity, componentType).Enabled = true;
            }
        }

        /// <summary>
        /// Checks if an entity died, and handles death accordingly.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is EntityDied)
            {
                var entity = ((EntityDied)(ValueType)message).Entity;

                // Play explosion effect at point of death.
                var particleSystem = (CameraCenteredParticleEffectSystem)Manager.GetSystem(CameraCenteredParticleEffectSystem.TypeId);
                if (particleSystem != null)
                {
                    particleSystem.Play("Effects/BasicExplosion", entity);
                }
                var soundSystem = (CameraCenteredSoundSystem)Manager.GetSystem(CameraCenteredSoundSystem.TypeId);
                if (soundSystem != null)
                {
                    soundSystem.Play("Explosion", entity);
                }

                // See if the entity respawns.
                var respawn = ((Respawn)Manager.GetComponent(entity, Respawn.TypeId));
                if (respawn != null)
                {
                    // Entity does respawn, components and wait.
                    foreach (var componentType in respawn.ComponentsToDisable)
                    {
                        Manager.GetComponent(entity, componentType).Enabled = false;
                    }
                    respawn.TimeToRespawn = respawn.Delay;

                    // Stop the entity, to avoid zooming off to nowhere when
                    // killed by a sun, e.g.
                    var velocity = ((Velocity)Manager.GetComponent(entity, Velocity.TypeId));
                    if (velocity != null)
                    {
                        velocity.Value = Vector2.Zero;
                    }
                }
                else
                {
                    // Entity does not respawn, remove it.
                    Manager.RemoveEntity(entity);   
                }
            }
            else if (message is TranslationChanged)
            {
                var changedMessage = ((TranslationChanged)(ValueType)message);

                // Only remove entities marked for removal.
                if (Manager.GetComponent(changedMessage.Entity, CellDeath.TypeId) == null)
                {
                    return;
                }

                // Check our new cell after the position change.
                var position = changedMessage.CurrentPosition;
                var cellId = CoordinateIds.Combine(
                    (int)position.X >> CellSystem.CellSizeShiftAmount,
                    (int)position.Y >> CellSystem.CellSizeShiftAmount);

                // If the cell changed, check if we're out of bounds.
                if (!((CellSystem)Manager.GetSystem(CellSystem.TypeId)).IsCellActive(cellId))
                {
                    // Dead space, kill it.
                    Manager.RemoveEntity(changedMessage.Entity);
                }
            }
        }
    }
}
