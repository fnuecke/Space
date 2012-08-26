using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles input to control how a ship flies, i.e. its acceleration and
    /// rotation, as well as whether it's shooting or not.
    /// </summary>
    public sealed class ShipControlSystem : AbstractParallelComponentSystem<ShipControl>
    {
        #region Logic

        /// <summary>
        /// Updates the component.
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ShipControl component)
        {
            //TODO: Add flag to component to check if we actually need to recompute anything?

            // Get components we depend upon / modify.
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));
            var spin = ((Spin)Manager.GetComponent(component.Entity, Spin.TypeId));
            var character = ((Character<AttributeType>)Manager.GetComponent(component.Entity, Character<AttributeType>.TypeId));
            var info = ((ShipInfo)Manager.GetComponent(component.Entity, ShipInfo.TypeId));
            var weaponControl = ((WeaponControl)Manager.GetComponent(component.Entity, WeaponControl.TypeId));
            var acceleration = ((Acceleration)Manager.GetComponent(component.Entity, Acceleration.TypeId));
            var effects = ((ParticleEffects)Manager.GetComponent(component.Entity, ParticleEffects.TypeId));
            var sound = ((Sound)Manager.GetComponent(component.Entity, Sound.TypeId));
            var maxAccelerationForce = character.GetValue(AttributeType.AccelerationForce) / Settings.TicksPerSecond;

            // Get the mass of the ship.
            var mass = info.Mass;

            // Get our acceleration direction, based on whether we're
            // currently stabilizing or not.
            var direction = component.DirectedAcceleration;
            var desiredAccelerationForce = 0f;
            if (direction == Vector2.Zero)
            {
                if (component.Stabilizing)
                {
                    // We want to stabilize.
                    var velocity = (Velocity)Manager.GetComponent(component.Entity, Velocity.TypeId);
                    direction = -(velocity.Value + acceleration.Value);
                    desiredAccelerationForce = direction.Length() * mass;
                }
            }
            else
            {
                // Player is accelerating.
                desiredAccelerationForce = maxAccelerationForce * direction.Length();
            }

            // Check if we're accelerating at all.
            if (direction != Vector2.Zero && desiredAccelerationForce > 0)
            {
                // Normalize our direction vector.
                direction.Normalize();

                // Get the needed energy and thruster power.
                var maxEnergyConsumption = character.GetValue(AttributeType.ThrusterEnergyConsumption) / Settings.TicksPerSecond;

                // Apply some dampening on how fast we accelerate when accelerating
                // sideways/backwards. We do this before computing energy consumption
                // and such, to avoid using full energy even though we're moving
                // slower.
                var angle = Math.Abs(MathHelper.ToDegrees(Angle.MinAngle(transform.Rotation, (float)Math.Atan2(direction.Y, direction.X))));
                maxAccelerationForce *= Math.Max(0, 200f - Math.Max(0, angle - 60f)) / 200f;

                // Get the percentage of the overall thrusters' power we
                // still need to fulfill our quota. Our timeslices are of
                // size one, so we can define velocity == acceleration.
                // Thus with F=m*a, where a is our velocity, the system
                // load is the fraction of desired acceleration force divided
                // by the possibly achievable one.
                var load = Math.Min(1, desiredAccelerationForce / maxAccelerationForce);

                // And apply it to our energy and power values.
                var energyConsumption = maxEnergyConsumption * load;
                var accelerationForce = maxAccelerationForce * load;

                // Make sure we have enough energy. If we don't we have to slow down.
                var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
                if (energy.Value <= energyConsumption)
                {
                    // Not enough energy, adjust our output.
                    var fraction = energy.Value / energyConsumption;

                    accelerationForce *= fraction;
                    energyConsumption = energy.Value;

                    // Also adjust the original load variable, as it's used for
                    // scaling the thruster effects.
                    load *= fraction;
                }

                // Consume the energy.
                energy.SetValue(energy.Value - energyConsumption);

                // Get actual acceleration by dividing the acceleration force we
                // can produce by our mass.
                var finalAcceleration = accelerationForce / mass;

                // Apply our acceleration in the desired direction.
                acceleration.Value += direction * finalAcceleration;

                // Adjust thruster PFX based on acceleration, if it just started.
                effects.SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, true);
                effects.SetGroupDirection(ParticleEffects.EffectGroup.Thruster, (float)Math.Atan2(-direction.Y, -direction.X) - transform.Rotation, Math.Max(0.3f, load));

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

            // Compute rotation speed. Yes, this is actually the rotation acceleration,
            // but whatever...
            var rotation = character.GetValue(AttributeType.RotationForce) / (mass * Settings.TicksPerSecond);

            // Update rotation / spin.
            var currentDelta = Angle.MinAngle(transform.Rotation, component.TargetRotation);
            var requiredSpin = (currentDelta > 0 ? Directions.Right.ToScalar() : Directions.Left.ToScalar()) * rotation;

            // If the target rotation changed and we're either not spinning,
            // or spinning the wrong way.
            if (component.TargetRotationChanged &&
                Math.Sign(spin.Value) != Math.Sign(requiredSpin))
            {
                // Is it worth starting to spin, or should we just jump to the position?
                // If the distance we need to spin is lower than what we spin in one tick,
                // just set it.
                if (Math.Abs(currentDelta) > rotation)
                {
                    // Spin, the angle takes multiple frames to rotate.
                    spin.Value = requiredSpin;
                }
                else
                {
                    // Set, only one frame (this one) required.
                    transform.SetRotation(component.TargetRotation);
                    spin.Value = 0;
                }
            }

            // Check if we're spinning.
            if (Math.Abs(spin.Value) > 0.001f)
            {
                // Yes, check if we passed our target rotation. This is the
                // case when the distance to the target in the last step was
                // smaller than our rotational speed.
                var remainingAngle = Math.Abs(Angle.MinAngle(component.PreviousRotation, component.TargetRotation));
                if (remainingAngle < Math.Abs(spin.Value))
                {
                    // Yes, set to that rotation and stop spinning.
                    transform.SetRotation(component.TargetRotation);
                    spin.Value = 0;
                }
            }

            // Set firing state for weapon systems.
            weaponControl.Shooting = component.Shooting;

            // Remember rotation in this update for the next.
            component.PreviousRotation = transform.Rotation;

            // We handled this change, if there was one.
            component.TargetRotationChanged = false;
        }

        #endregion
    }
}
