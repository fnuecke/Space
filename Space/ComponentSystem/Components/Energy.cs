using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Space.Data;
using Space.Data.Modules;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the energy available on a entity.
    /// </summary>
    public sealed class Energy : AbstractRegeneratingValue
    {
        /// <summary>
        /// Test for change in equipment.
        /// </summary>
        /// <param name="message">Handles module added / removed messages.</param>
        public override void HandleMessage(ValueType message)
        {
            if (message is ModuleAdded<EntityAttributeType> || message is ModuleRemoved<EntityAttributeType>)
            {
                // Module removed or added, recompute our values.
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

                // Rebuild base energy and regeneration values.
                MaxValue = 0;
                Regeneration = 0;
                foreach (var reactor in modules.GetModules<ReactorModule>())
                {
                    MaxValue += reactor.Energy;
                    Regeneration += reactor.EnergyRegeneration;
                }

                // Apply bonuses.
                MaxValue = modules.GetValue(EntityAttributeType.Energy, MaxValue);
                Regeneration = modules.GetValue(EntityAttributeType.EnergyRegeneration, Regeneration);

                // Adjust current energy so it does not exceed our new maximum.
                if (Value > MaxValue)
                {
                    Value = MaxValue;
                }
            }
        }
    }
}
