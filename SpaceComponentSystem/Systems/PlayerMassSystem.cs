using System;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Recomputes mass of a player ship based on its character stats.
    /// </summary>
    public sealed class PlayerMassSystem : AbstractSystem, IMessagingSystem
    {
        #region Logic

        /// <summary>
        /// Receives the specified message and handles it if it invalidates character stats.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as CharacterStatsInvalidated?;
            if (cm == null)
            {
                return;
            }

            // Module removed or added, recompute mass.
            var entity = cm.Value.Entity;
            var attributes = (Attributes<AttributeType>)Manager.GetComponent(entity, Attributes<AttributeType>.TypeId);
            var gravitation = (Gravitation)Manager.GetComponent(entity, Gravitation.TypeId);
            if (gravitation == null)
            {
                // Skip if this entity doesn't care for gravitation.
                return;
            }

            // Get the mass of the ship and return it.
            gravitation.Mass = Math.Max(1, attributes.GetValue(AttributeType.Mass));
        }

        #endregion
    }
}
