using Engine.ComponentSystem.RPG.Components;
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

        protected override void RecomputeValues()
        {
            // Recompute our values.
            var character = Entity.GetComponent<Character<AttributeType>>();

            // Remember current relative value. Set to full if it was zero
            // before, because that means we're initializing for the first
            // time.
            float relative = (MaxValue > 0) ? (Value / MaxValue) : 1;

            // Rebuild base energy and regeneration values.
            MaxValue = System.Math.Max(1, character.GetValue(AttributeType.Energy));
            Regeneration = System.Math.Max(0, character.GetValue(AttributeType.EnergyRegeneration) / 60f);

            // Set new relative value.
            Value = relative * MaxValue;
        }

        #endregion
    }
}
