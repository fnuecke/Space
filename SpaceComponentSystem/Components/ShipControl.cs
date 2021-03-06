﻿using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    ///     Handles control of a single ship, represented by its relevant components.
    ///     <para>
    ///         Requires: <c>Transform</c>, <c>Spin</c>, <c>Acceleration</c>, <c>MovementProperties</c>.
    ///     </para>
    /// </summary>
    public sealed class ShipControl : Component
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

        /// <summary>Whether we're currently stabilizing our position or not.</summary>
        public bool Stabilizing;

        /// <summary>Whether to shoot or not.</summary>
        public bool Shooting;

        /// <summary>Whether the ship's shields are up.</summary>
        public bool ShieldsActive;

        /// <summary>The current acceleration of the ship.</summary>
        internal Vector2 DirectedAcceleration;

        /// <summary>The current target rotation (used to check if the public one changed since the last update).</summary>
        internal float TargetAngle;

        #endregion

        #region Initialization

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Stabilizing = false;
            Shooting = false;
            ShieldsActive = false;
            DirectedAcceleration = Vector2.Zero;
            TargetAngle = 0;
        }

        #endregion

        #region Utility Accessors

        /// <summary>
        ///     Begin or continue accelerating in the specified direction, or stop accelerating if <c>Vector2.Zero</c> is given.
        /// </summary>
        /// <param name="direction">The new directed acceleration.</param>
        public void SetAcceleration(Vector2 direction)
        {
            DirectedAcceleration = direction;
            if (DirectedAcceleration != Vector2.Zero)
            {
                // Make sure we have a unit vector of our direction.
                if (DirectedAcceleration.LengthSquared() > 1f)
                {
                    DirectedAcceleration.Normalize();
                }
            }
        }

        /// <summary>Set a new target rotation for this ship.</summary>
        /// <param name="rotation">The rotation to rotate to.</param>
        public void SetTargetRotation(float rotation)
        {
            TargetAngle = rotation;
        }

        #endregion
    }
}