using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Messages;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>Represents the health available on a entity.</summary>
    public sealed class Health : AbstractRegeneratingValue
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Accessors

        /// <summary>Sets the value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="causingEntity">The entity that triggered the value change.</param>
        public override void SetValue(float value, int causingEntity = 0)
        {
            base.SetValue(value, causingEntity);
            if (Value <= 0)
            {
                EntityDied message;
                message.KilledEntity = Entity;
                message.KillingEntity = causingEntity;
                Manager.SendMessage(message);
            }
        }

        #endregion

        #region Logic

        /// <summary>Recomputes the maximum value and regeneration speed.</summary>
        internal override void RecomputeValues()
        {
            // Recompute our values.
            var attributes = (Attributes<AttributeType>) Manager.GetComponent(Entity, Attributes<AttributeType>.TypeId);

            // Remember current relative value. Set to full if it was zero
            // before, because that means we're initializing for the first
            // time.
            var relative = (MaxValue > 0) ? (Value / MaxValue) : 1;

            // Rebuild base health and regeneration values.
            MaxValue = System.Math.Max(1, attributes.GetValue(AttributeType.Health));
            Regeneration = System.Math.Max(
                0, attributes.GetValue(AttributeType.HealthRegeneration) / Settings.TicksPerSecond);

            // Set new relative value.
            Value = relative * MaxValue;
        }

        #endregion
    }
}