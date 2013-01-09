using Engine.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>Tells the player class via a player's ship.</summary>
    public sealed class PlayerClass : Component
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

        /// <summary>The player class of this ship's player.</summary>
        public PlayerClassType Value;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((PlayerClass) other).Value;

            return this;
        }

        /// <summary>Initialize with the specified player class.</summary>
        /// <param name="playerClass">The player class.</param>
        public PlayerClass Initialize(PlayerClassType playerClass)
        {
            Value = playerClass;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = PlayerClassType.Default;
        }

        #endregion
    }
}