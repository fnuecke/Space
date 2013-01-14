using System.Linq;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system tracks entities that respawn (players, normally).</summary>
    public sealed class RespawnSystem : AbstractParallelComponentSystem<Respawn>, IMessagingSystem
    {
        #region Logic

        /// <summary>Checks for entities to respawn.</summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, Respawn component)
        {
            // Skip if already alive or not yet ready to revive.
            if (component.TimeToRespawn <= 0 || --component.TimeToRespawn != 0)
            {
                return;
            }

            // Try to position.
            var transform = ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId));
            if (transform != null)
            {
                transform.Translation = component.Position;
                transform.Update();
                transform.Rotation = 0;
            }

            // Kill of remainder velocity.
            var velocity = ((Velocity) Manager.GetComponent(component.Entity, Velocity.TypeId));
            if (velocity != null)
            {
                velocity.Value = Vector2.Zero;
            }

            // Fill up health / energy.
            var health = ((Health) Manager.GetComponent(component.Entity, Health.TypeId));
            if (health != null)
            {
                health.SetValue(health.MaxValue * component.RelativeHealth);
            }
            var energy = ((Energy) Manager.GetComponent(component.Entity, Energy.TypeId));
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

        /// <summary>Checks if an entity died, and marks it for respawn if possible.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as EntityDied?;
            if (cm == null)
            {
                return;
            }

            var entity = cm.Value.KilledEntity;

            // See if the entity respawns.
            var respawn = ((Respawn) Manager.GetComponent(entity, Respawn.TypeId));
            if (respawn == null)
            {
                return;
            }

            // Entity does respawn, disable components and wait.
            foreach (var componentType in respawn.ComponentsToDisable)
            {
                Manager.GetComponent(entity, componentType).Enabled = false;
            }
            respawn.TimeToRespawn = respawn.Delay;

            // Remove any remaining damage debuffs.
            foreach (var dot in Manager.GetComponents(entity, DamagingStatusEffect.TypeId).ToList())
            {
                Manager.RemoveComponent(dot);
            }

            // Stop the entity, to avoid zooming off to nowhere when
            // killed by a sun, e.g.
            var velocity = ((Velocity) Manager.GetComponent(entity, Velocity.TypeId));
            if (velocity != null)
            {
                velocity.Value = Vector2.Zero;
            }
        }

        #endregion
    }
}