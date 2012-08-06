using Engine.ComponentSystem.RPG.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the energy available on a entity.
    /// </summary>
    public sealed class Energy : AbstractRegeneratingValue
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Recomputes the maximum value and regeneration speed.
        /// </summary>
        internal override void RecomputeValues()
        {
            // Recompute our values.
            var character = ((Character<AttributeType>)Manager.GetComponent(Entity, Character<AttributeType>.TypeId));

            // Remember current relative value. Set to full if it was zero
            // before, because that means we're initializing for the first
            // time.
            var relative = (MaxValue > 0) ? (Value / MaxValue) : 1;

            // Rebuild base energy and regeneration values.
            MaxValue = System.Math.Max(1, character.GetValue(AttributeType.Energy));
            Regeneration = System.Math.Max(0, character.GetValue(AttributeType.EnergyRegeneration) / Settings.TicksPerSecond);

            // Set new relative value.
            Value = relative * MaxValue;
        }

        #endregion
    }
}
