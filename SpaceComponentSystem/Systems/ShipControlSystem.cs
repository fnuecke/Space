using System;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     Handles input to control how a ship flies, i.e. its acceleration and rotation, as well as whether it's
    ///     shooting or not.
    /// </summary>
    public sealed class ShipControlSystem : AbstractUpdatingComponentSystem<ShipControl>
    {
        #region Constants
        
        /// <summary>This is the angle between orientation and acceleration direction inside which a ship gets to use full power.</summary>
        /// <remarks>This is the angle in one direction, i.e. the full angle is twice this.</remarks>
        private static readonly float MaxAccelerationAngle = MathHelper.ToRadians(60f);

        /// <summary>
        ///     This is the interval in which the power output shrinks. We essentially let it drop until it hits zero when
        ///     it's in the completely opposite direction.
        /// </summary>
        private static readonly float AccelerationAngleInterval = MathHelper.ToRadians(180f) - MaxAccelerationAngle;

        /// <summary>This is the minimum angle based acceleration power, to avoid getting no thrust at all when flying backwards.</summary>
        private const float MinAcceleration = 0.5f;

        /// <summary>If the angular velocity or angle delta of body to target angle are lower than this we ignore it.</summary>
        private static readonly float AngularSleepTolerance = MathHelper.ToRadians(1f);

        #endregion

        #region Properties

        /// <summary>
        ///     Whether the ship cannot fulfill the required acceleration because it exceeds the maximum output of its
        ///     thrusters. This can happen in particular when the stabilizer fails due to being too close to a body with high
        ///     gravitation.
        /// </summary>
        public bool IsOverloading { get; private set; }

        /// <summary>
        ///     Whether the ship cannot fulfill the required acceleration because it does not have enough energy for its
        ///     thrusters to work at the required level.
        /// </summary>
        public bool IsUnderpowered { get; private set; }

        #endregion

        #region Logic

        /// <summary>Updates the component.</summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ShipControl component)
        {
            // Set firing state for weapon systems.
            var weaponControl = (WeaponControl) Manager.GetComponent(component.Entity, WeaponControl.TypeId);
            weaponControl.Shooting = component.Shooting;

            // Get components we depend upon / modify.
            var attributes = (Attributes<AttributeType>) Manager.GetComponent(component.Entity, Attributes<AttributeType>.TypeId);
            var body = (Body) Manager.GetComponent(component.Entity, Body.TypeId);
            var effects = (ParticleEffects) Manager.GetComponent(component.Entity, ParticleEffects.TypeId);
            var sound = (Sound) Manager.GetComponent(component.Entity, Sound.TypeId);
            
            // We need to wrap it, so get it once.
            var bodyAngle = MathHelper.WrapAngle(body.Angle);

            // Compute the linear force we want to apply to the ship. This depends on the current
            // acceleration direction, or whether we're stabilizing or not. This is relatively
            // straightforward, except that we have to enforce our limits given by thruster power
            // and available energy.
            {
                var maxAcceleration = attributes.GetValue(AttributeType.AccelerationForce);

                // Get our acceleration direction, based on whether we're currently stabilizing or not.
                var forceDirection = component.DirectedAcceleration;
                var forceValue = 0f;
                if (forceDirection == Vector2.Zero)
                {
                    if (component.Stabilizing)
                    {
                        // We want to stabilize. Generate a force that nulls whatever forces are
                        // currently being applied to the body, plus one that nulls out the momentum
                        // of the body. We store the value and direction of the force in two variables
                        // because we adjust the actually applied value based on available resources.
                        forceDirection = -(body.Force + body.LinearVelocity * body.Mass);
                        forceValue = forceDirection.Length();
                    }
                }
                else
                {
                    // Player is accelerating. Take the maximum possible acceleration force and apply our
                    // input amplitude which is of the interval [0, 1) (direction is at maximum a unit vector).
                    forceValue = maxAcceleration * forceDirection.Length();
                }

                // Check if we're accelerating at all.
                if (forceDirection != Vector2.Zero && forceValue > 0.01f)
                {
                    // Normalize our direction vector.
                    forceDirection.Normalize();

                    // Get the needed energy consumption when running at full capacity. This value's unit
                    // is [energy/second], so we want to adjust it to our simulation speed.
                    var maxEnergyConsumption = attributes.GetValue(AttributeType.ThrusterEnergyConsumption) /
                                               Settings.TicksPerSecond;

                    // Apply some dampening on how fast we accelerate when accelerating sideways/backwards.
                    // We do this before computing energy consumption and such, to avoid using full energy
                    // even though we're moving slower.
                    var angleDifference = Math.Abs(Angle.MinAngle(bodyAngle, (float) Math.Atan2(forceDirection.Y, forceDirection.X)));
                    maxAcceleration *= MinAcceleration + (1f - MinAcceleration) * (1f - Math.Max(0, angleDifference - MaxAccelerationAngle) / AccelerationAngleInterval);

                    // Get the percentage of the overall thrusters' power we need to fulfill our quota.
                    var load = forceValue / maxAcceleration;
                    if (load > 1f)
                    {
                        // Let's store this in case we want to display it in the GUI/show some fancy effects.
                        IsOverloading = true;
                        load = 1f;
                    }
                    else
                    {
                        // All systems green.
                        IsOverloading = false;
                    }

                    // And apply it to our energy and power values.
                    var energyConsumption = maxEnergyConsumption * load;
                    var acceleration = maxAcceleration * load;

                    // Make sure we have enough energy. If we don't we have to slow down.
                    var energy = ((Energy) Manager.GetComponent(component.Entity, Energy.TypeId));
                    if (energy.Value <= energyConsumption)
                    {
                        // Not enough energy. Store this, in case we want to display it in the GUI.
                        IsUnderpowered = true;

                        // Compute the fraction of the desired output we can actually fulfill with the available energy.
                        var fraction = energy.Value / energyConsumption;

                        // Then adjust our output.  Use all energy and reduce produced force.
                        energyConsumption = energy.Value;
                        acceleration *= fraction;

                        // Also adjust the original load variable, as it's used for
                        // scaling the thruster effects.
                        load *= fraction;
                    }
                    else
                    {
                        // Got enough juice, no need to worry!
                        IsUnderpowered = false;
                    }

                    // Consume the energy.
                    energy.SetValue(energy.Value - energyConsumption);

                    // Apply our force in the desired direction.
                    body.ApplyForceToCenter(forceDirection * acceleration);

                    // Adjust thruster PFX based on acceleration, if it just started.
                    effects.SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, true);
                    effects.SetGroupDirection(
                        ParticleEffects.EffectGroup.Thruster,
                        (float) Math.Atan2(-forceDirection.Y, -forceDirection.X) - bodyAngle,
                        Math.Max(0.3f, load));

                    // Enable thruster sound for this ship.
                    sound.Enabled = true;
                }
                else
                {
                    // Not accelerating. Disable thruster effects if we were accelerating before.
                    effects.SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, false);

                    // Disable thruster sound for this ship.
                    sound.Enabled = false;
                }
            }

            // Compute rotation. As opposed to our linear acceleration we have a target value here, so we want to
            // make sure we don't overshoot it (too much anyways). Rotating does not cost energy. Skip all these
            // computations, though, if the ship is pretty much at a standstill.
            if (Math.Abs(Angle.MinAngle(bodyAngle, component.TargetAngle)) > AngularSleepTolerance ||
                Math.Abs(body.AngularVelocity) > AngularSleepTolerance)
            {
                // Get the maximum rotational force we can produce.
                var maxAcceleration = attributes.GetValue(AttributeType.RotationForce);

                // We use it quite a bit, so get the current angular velocity of the body once.
                var angularVelocity = body.AngularVelocity;
                
                // See in which direction we have to rotate the body to get it to the target angle. We want to take
                // into account the current angular velocity here, because it might be faster to just keep spinning
                // instead of reversing our spin first. To do that we use the angle at which the body would be if
                // we'd start slowing down now and continued to do so until we stopped. This will also make us
                // inverse our acceleration if we'd overshoot our target angle.
                var distanceToStop = (angularVelocity * angularVelocity) / (2 * maxAcceleration);
                var angleDeltaToTarget = Angle.MinAngle(bodyAngle + Math.Sign(angularVelocity) * distanceToStop, component.TargetAngle);
                
                // Get our signed acceleration.
                var acceleration = maxAcceleration * Math.Sign(angleDeltaToTarget);
                
                // We already reverse our acceleration above when necessary, but we still have the problem that we
                // can accelerate too fast -- i.e. we run past our target angle from the acceleration in a single
                // frame. This would lead to the ship 'jittering' around the target angle. So what we want is to
                // slow down to just the speed that will get us to our target angle. To do that we compute the angle
                // we rotate in the next frame assuming we use our full rotation and see it we run past our target.
                var angleDeltaNextFrame = (angularVelocity + acceleration / Settings.TicksPerSecond) / Settings.TicksPerSecond;
                if (Math.Sign(angleDeltaToTarget - angleDeltaNextFrame) != Math.Sign(angleDeltaToTarget))
                {
                    // Yes, we go too far. Tune down the acceleration to just lower our current angular velocity so
                    // as to land on our target angle, if possible (it's possible that we rotate faster than we can
                    // handle, so we have to check if we can actually handle that acceleration).
                    var accelerationToStopOnTarget = (angleDeltaToTarget * Settings.TicksPerSecond - angularVelocity) * Settings.TicksPerSecond;
                    if (Math.Abs(accelerationToStopOnTarget) <= maxAcceleration)
                    {
                        // We can stop next frame!
                        acceleration = accelerationToStopOnTarget;
                    }
                    else
                    {
                        // We can't stop, but we can adjust our acceleration.
                        System.Diagnostics.Debug.Assert(Math.Sign(-acceleration) == Math.Sign(accelerationToStopOnTarget));
                        acceleration = -acceleration;
                    }
                }

                // Apply our acceleration.
                body.ApplyTorque(acceleration * body.Inertia);
            }
        }

        #endregion
    }
}