using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>This component can be used to represent ownership relationships between entities.</summary>
    public sealed class Owner : Component
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

        /// <summary>The id of the owning entity.</summary>
        public int Value;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((Owner) other).Value;

            return this;
        }

        /// <summary>Initialize the component with the specified player number.</summary>
        /// <param name="ownerId">The owning entity's id.</param>
        public Owner Initialize(int ownerId)
        {
            Value = ownerId;

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