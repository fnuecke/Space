using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>Represents the velocity of a simple entity.</summary>
    public sealed class Velocity : Component, IVelocity
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

        /// <summary>Gets or sets the linear velocity.</summary>
        public Vector2 LinearVelocity { get; set; }

        /// <summary>Gets or sets the angular velocity.</summary>
        public float AngularVelocity { get; set; }

        /// <summary>Gets the accumulated force currently being applied to the body, which will be applied in the next update.</summary>
        public Vector2 Force
        {
            get
            {
                var acceleration = (Acceleration) Manager.GetComponent(Entity, Acceleration.TypeId);
                return acceleration == null ? Vector2.Zero : acceleration.Value;
            }
        }

        /// <summary>Applies a force onto the component's center of mass.</summary>
        public void ApplyForceToCenter(Vector2 force)
        {
            var acceleration = (Acceleration) Manager.GetComponent(Entity, Acceleration.TypeId);
            if (acceleration != null)
            {
                acceleration.Value += force;
            }
        }

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified velocity.</summary>
        /// <param name="linearVelocity">The linear velocity.</param>
        /// <param name="angularVelocity">The angular velocity.</param>
        /// <returns></returns>
        public Velocity Initialize(Vector2 linearVelocity, float angularVelocity = 0f)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            LinearVelocity = Vector2.Zero;
            AngularVelocity = 0f;
        }

        #endregion
    }
}