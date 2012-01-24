using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Space.ComponentSystem.Modules;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the energy available on a entity.
    /// </summary>
    public sealed class Energy : AbstractRegeneratingValue
    {
        #region Constructor

        /// <summary>
        /// Creates a new energy component.
        /// </summary>
        /// <param name="timeout">The number of ticks to wait after using
        /// energy before starting to regenerate energy again.</param>
        public Energy(int timeout)
            : base(timeout)
        {
        }

        public Energy()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Test for change in equipment.
        /// </summary>
        /// <param name="message">Handles module added / removed messages.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is ModuleValueInvalidated<SpaceModifier>)
            {
                var type = ((ModuleValueInvalidated<SpaceModifier>)(ValueType)message).ValueType;
                if (type == SpaceModifier.Energy || type == SpaceModifier.EnergyRegeneration)
                {
                    // Module removed or added, recompute our values.
                    var modules = Entity.GetComponent<ModuleManager<SpaceModifier>>();

                    // Rebuild base energy and regeneration values.
                    MaxValue = 0;
                    Regeneration = 0;
                    foreach (var reactor in modules.GetModules<Reactor>())
                    {
                        MaxValue += reactor.Energy;
                        Regeneration += reactor.EnergyRegeneration;
                    }

                    // Apply bonuses.
                    MaxValue = modules.GetValue(SpaceModifier.Energy, MaxValue);
                    Regeneration = modules.GetValue(SpaceModifier.EnergyRegeneration, Regeneration);

                    // Adjust current energy so it does not exceed our new maximum.
                    if (Value > MaxValue)
                    {
                        Value = MaxValue;
                    }
                }
            }
        }

        #endregion
    }
}
