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
        /// Sends <c>EntityDied</c> messages if health is zero. It is expected
        /// that this component will be disabled on death, so this won't be
        /// spammed all the time.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            if (Value <= 0)
            {
                EntityDied message;
                Entity.SendMessage(ref message);
            }

            base.Update(parameterization);
        }

        protected override void RecomputeValues()
        {
            // Recompute our values.
            var character = Entity.GetComponent<Character<AttributeType>>();

            // Remember current relative value. Set to full if it was zero
            // before, because that means we're initializing for the first
            // time.
            float relative = (MaxValue > 0) ? (Value / MaxValue) : 1;

            // Rebuild base health and regeneration values.
            MaxValue = System.Math.Max(1, character.GetValue(AttributeType.Health));
            Regeneration = System.Math.Max(0, character.GetValue(AttributeType.HealthRegeneration) / 60f);

            // Set new relative value.
            Value = relative * MaxValue;
        }

        #endregion
    }
}
