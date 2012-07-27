using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the health available on a entity.
    /// </summary>
    public sealed class Health : AbstractRegeneratingValue
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = Engine.ComponentSystem.Manager.GetComponentTypeId(typeof(Health));

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value.</param>
        public override void SetValue(float value)
        {
            base.SetValue(value);
            if (Value <= 0)
            {
                EntityDied message;
                message.Entity = Entity;
                Manager.SendMessage(ref message);
            }
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

            // Rebuild base health and regeneration values.
            MaxValue = System.Math.Max(1, character.GetValue(AttributeType.Health));
            Regeneration = System.Math.Max(0, character.GetValue(AttributeType.HealthRegeneration) / 60f);

            // Set new relative value.
            Value = relative * MaxValue;
        }

        #endregion
    }
}
