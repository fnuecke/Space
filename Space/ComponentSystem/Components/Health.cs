﻿using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the health available on a entity.
    /// </summary>
    public sealed class Health : AbstractRegeneratingValue
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
                foreach (var hull in modules.GetModules<HullModule>())
                {
                    MaxValue += hull.Health;
                    Regeneration += hull.HealthRegeneration;
                }

                // Apply bonuses.
                MaxValue = modules.GetValue(EntityAttributeType.Health, MaxValue);
                Regeneration = modules.GetValue(EntityAttributeType.HealthRegeneration, Regeneration);

                // Adjust current health so it does not exceed our new maximum.
                if (Value > MaxValue)
                {
                    Value = MaxValue;
                }
            }
        }
    }
}