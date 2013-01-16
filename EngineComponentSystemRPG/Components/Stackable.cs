using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>Makes an item stackable. Items with the same group id can be merged into a single stack.</summary>
    public sealed class Stackable : Component
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

        /// <summary>The current number of items in the stack.</summary>
        public int Count;

        /// <summary>The maximum number of items that can be in a single stack.</summary>
        public int MaxCount;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified parameters.</summary>
        /// <param name="maxCount">The maximum number of items that can be merged into a single stack.</param>
        public Stackable Initialize(int maxCount)
        {
            Count = 1;
            MaxCount = maxCount;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Count = 0;
            MaxCount = 0;
        }

        #endregion
    }
}