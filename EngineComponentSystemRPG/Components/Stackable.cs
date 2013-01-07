using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Makes an item stackable. Items with the same group id can be merged
    /// into a single stack.
    /// </summary>
    public sealed class Stackable : Component
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
        /// The current number of items in the stack.
        /// </summary>
        public int Count;

        /// <summary>
        /// The maximum number of items that can be in a single stack.
        /// </summary>
        public int MaxCount;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherStackable = (Stackable)other;
            Count = otherStackable.Count;
            MaxCount = otherStackable.MaxCount;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="maxCount">The maximum number of items that can be
        /// merged into a single stack.</param>
        public Stackable Initialize(int maxCount)
        {
            this.Count = 1;
            this.MaxCount = maxCount;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Count = 0;
            MaxCount = 0;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Count=" + Count + ", MaxCount=" + MaxCount;
        }

        #endregion
    }
}
