using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    ///     Allows targeting another entity, i.e. storing the target of an entity. This can be used for auto targeting
    ///     weapons (missiles) but may also be used to store an AIs current target entity.
    /// </summary>
    public sealed class Target : Component
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

        /// <summary>The ID of the entity that's being targeted.</summary>
        public int Value;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((Target) other).Value;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0;
        }

        #endregion
    }
}