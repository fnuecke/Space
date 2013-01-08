using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Represents rotation speed of an object.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public sealed class Spin : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public float Value;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((Spin)other).Value;

            return this;
        }

        /// <summary>
        /// Initialize with the specified spin.
        /// </summary>
        /// <param name="spin">The spin.</param>
        public Spin Initialize(float spin)
        {
            this.Value = spin;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0;
        }

        #endregion
    }
}
