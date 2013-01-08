using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Part of entities that represent a player's presence in a game.
    /// </summary>
    public sealed class Avatar : Component
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
        /// The number of player whose avatar this is.
        /// </summary>
        public int PlayerNumber;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            PlayerNumber = ((Avatar)other).PlayerNumber;

            return this;
        }

        /// <summary>
        /// Initialize the component with the specified player number.
        /// </summary>
        /// <param name="playerNumber">The player number.</param>
        public Avatar Initialize(int playerNumber)
        {
            PlayerNumber = playerNumber;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            PlayerNumber = 0;
        }

        #endregion
    }
}
