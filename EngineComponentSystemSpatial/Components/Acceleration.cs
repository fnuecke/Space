using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    ///     Represents the acceleration of an object.
    ///     <para>
    ///         Requires: <c>Velocity</c>.
    ///     </para>
    /// </summary>
    public sealed class Acceleration : Component
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

        /// <summary>The directed acceleration of the object.</summary>
        public Vector2 Value;

        #endregion

        #region Initialization

        /// <summary>Initialize the component with the specified value.</summary>
        /// <param name="acceleration">The acceleration.</param>
        public Acceleration Initialize(Vector2 acceleration)
        {
            Value = acceleration;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = Vector2.Zero;
        }

        #endregion
    }
}