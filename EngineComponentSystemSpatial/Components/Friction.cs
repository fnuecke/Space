﻿using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    ///     Represents friction for an object.
    ///     <para>
    ///         Requires: <c>Velocity</c>.
    ///     </para>
    /// </summary>
    public sealed class Friction : Component
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

        /// <summary>The damping to apply to this objects movement (simulates friction).</summary>
        public float Value;

        /// <summary>
        ///     Minimum velocity of an object before it is stopped (avoids jitter for very low velocity, due to which objects
        ///     with damping never stop, even though they should).
        /// </summary>
        public float StopVelocity;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified friction and stop velocity.</summary>
        /// <param name="value">The friction.</param>
        /// <param name="stopVelocity">The stop velocity.</param>
        public Friction Initialize(float value, float stopVelocity)
        {
            Value = value;
            StopVelocity = stopVelocity;

            return this;
        }

        /// <summary>Initialize with the specified friction.</summary>
        /// <param name="value">The friction.</param>
        public Friction Initialize(float value)
        {
            Initialize(value, 0);

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0;
            StopVelocity = 0;
        }

        #endregion
    }
}