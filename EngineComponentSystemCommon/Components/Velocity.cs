using System.Globalization;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Represents the velocity of an object.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public sealed class Velocity : Component
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
        /// The directed speed of the object.
        /// </summary>
        public Vector2 Value;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((Velocity)other).Value;

            return this;
        }

        /// <summary>
        /// Initialize with the specified velocity.
        /// </summary>
        /// <param name="velocity">The velocity.</param>
        public Velocity Initialize(Vector2 velocity)
        {
            Value = velocity;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = Vector2.Zero;
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
            return base.ToString() + ", Value=" + Value.X.ToString(CultureInfo.InvariantCulture) + ":" + Value.Y.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
