using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tracks what items a unit may drop on death, via the item pool id to
    /// draw items from.
    /// </summary>
    public sealed class Drops : Component
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
        /// The logical name of the item pool to draw items from when the unit
        /// dies.
        /// </summary>
        public string ItemPool;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            ItemPool = ((Drops)other).ItemPool;

            return this;
        }

        /// <summary>
        /// Initialize with the specified item pool.
        /// </summary>
        /// <param name="itemPool">The item pool.</param>
        public Drops Initialize(string itemPool)
        {
            ItemPool = itemPool;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            ItemPool = null;
        }

        #endregion
    }
}
