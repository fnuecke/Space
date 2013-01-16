using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>Base class for timed status effects that apply to the entity they are attached to.</summary>
    public abstract class StatusEffect : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>The remaining number of ticks this effect will stay active.</summary>
        public int Remaining;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified duration.</summary>
        /// <param name="duration">The duration.</param>
        public StatusEffect Initialize(int duration)
        {
            Remaining = duration;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Remaining = 0;
        }

        #endregion
    }
}