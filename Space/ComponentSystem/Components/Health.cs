using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the health available on a entity.
    /// </summary>
    public sealed class Health : AbstractRegeneratingValue
    {
        #region Constructor

        /// <summary>
        /// Creates a new health component.
        /// </summary>
        /// <param name="timeout">The number of ticks to wait after taking
        /// damage before starting to regenerate health again.</param>
        public Health(int timeout)
            : base(timeout)
        {
        }

        public Health()
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
            if (message is CharacterStatsInvalidated)
            {
                RecomputeValues();
            }
        }

        private void RecomputeValues()
        {
            // Recompute our values.
            var character = Entity.GetComponent<Character<AttributeType>>();

            // Rebuild base energy and regeneration values.
            MaxValue = System.Math.Max(1, character.GetValue(AttributeType.Energy));
            Regeneration = System.Math.Max(0, character.GetValue(AttributeType.EnergyRegeneration));

            // Adjust current energy so it does not exceed our new maximum.
            if (Value > MaxValue)
            {
                Value = MaxValue;
            }
        }

        #endregion
    }
}
