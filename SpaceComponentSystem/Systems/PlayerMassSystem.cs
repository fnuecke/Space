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
    public sealed class PlayerMassSystem : AbstractSystem
    {
        public override void Receive<T>(ref T message)
        {
            if (message is CharacterStatsInvalidated)
            {
                // Module removed or added, recompute mass.
                var entity = ((CharacterStatsInvalidated)(ValueType)message).Entity;
                var character = Manager.GetComponent<Character<AttributeType>>(entity);
                var gravitation = Manager.GetComponent<Gravitation>(entity);

                // Get the mass of the ship and return it.
                gravitation.Mass = Math.Max(1, character.GetValue(AttributeType.Mass));
            }
        }
    }
}
