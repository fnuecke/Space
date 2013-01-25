using System;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Recomputes mass of a player ship based on its character stats.</summary>
    public sealed class DynamicMassSystem : AbstractSystem
    {
        #region Logic

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<CharacterStatsInvalidated>(OnCharacterStatsInvalidated);
        }

        private void OnCharacterStatsInvalidated(CharacterStatsInvalidated message)
        {
            // Module removed or added, recompute mass.
            var entity = message.Entity;
            var attributes = (Attributes<AttributeType>) Manager.GetComponent(entity, Attributes<AttributeType>.TypeId);
            var gravitation = Manager.GetComponent(entity, Gravitation.TypeId) as Gravitation;
            if (gravitation != null)
            {
                // Get the mass of the object and return it.
                gravitation.Mass = Math.Max(1, attributes.GetValue(AttributeType.Mass));
            }
        }

        #endregion
    }
}