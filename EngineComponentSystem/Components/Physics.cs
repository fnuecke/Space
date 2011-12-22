using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents dynamic physical properties such as speed and acceleration.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// Optional: <c>Velocity</c>, <c>Spin</c>, <c>Acceleration</c>, <c>Friction</c>.
    /// </para>
    /// </summary>
    public sealed class Physics : AbstractComponent
    {
        #region Logic

        /// <summary>
        /// Updates an objects position/rotation and speed according to its acceleration
        /// and speeds.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var velocity = Entity.GetComponent<Velocity>();
            var acceleration = Entity.GetComponent<Acceleration>();
            var friction = Entity.GetComponent<Friction>();
            var spin = Entity.GetComponent<Spin>();
            var transform = Entity.GetComponent<Transform>();

            // Stuff that applies to velocity.
            if (velocity != null)
            {
                // Apply acceleration if set.
                if (acceleration != null)
                {
                    velocity.Value += acceleration.Value;
                }

                // Apply friction if set.
                if (friction != null && friction.Value > (Fixed)0)
                {
                    // Save previous velocity for stop check (due to MinVelocity).
                    var previousVelocity = velocity.Value.Norm;

                    // Apply friction.
                    velocity.Value = velocity.Value * ((Fixed)1 - friction.Value);

                    // If we're below a certain minimum speed, just stop, otherwise
                    // it'd be hard to. We only stop if we were faster than the minimum,
                    // before application of friction. Otherwise we might have problems
                    // getting moving at all, if the acceleration is too low.
                    if (previousVelocity >= friction.MinVelocity &&
                        velocity.Value.Norm < friction.MinVelocity)
                    {
                        velocity.Value = FPoint.Zero;
                    }
                }

                // Apply new velocity.
                transform.AddTranslation(velocity.Value);
            }

            // Apply rotation if set.
            if (spin != null)
            {
                transform.AddRotation(spin.Value);
            }
        }

        /// <summary>
        /// Accepts <c>PhysicsParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(PhysicsParameterization));
        }

        #endregion
    }
}
