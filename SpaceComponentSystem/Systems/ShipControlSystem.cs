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

            // Get the mass of the ship.
            var mass = info.Mass;

            // Get our acceleration direction, based on whether we're
            // currently stabilizing or not.
            Vector2 accelerationDirection;
            var desiredAcceleration = float.MaxValue;
            if (component.DirectedAcceleration == Vector2.Zero && component.Stabilizing)
            {
                // We want to stabilize.
                var velocity = (Velocity)Manager.GetComponent(component.Entity, Velocity.TypeId);
                accelerationDirection = -(velocity.Value + acceleration.Value);
                desiredAcceleration = accelerationDirection.Length();

                // If it's zero, normalize will make it {NaN, NaN}. Avoid that.
                if (accelerationDirection != Vector2.Zero)
                {
                    accelerationDirection.Normalize();
                }
            }
            else
            {
                // Player is accelerating.
                accelerationDirection = component.DirectedAcceleration;
            }

            // Check if we're accelerating at all.
            if (accelerationDirection != Vector2.Zero && desiredAcceleration > 0)
            {
                // Get the needed energy and thruster power.
                var energyConsumption = character.GetValue(AttributeType.ThrusterEnergyConsumption) / Settings.TicksPerSecond;
                var accelerationForce = character.GetValue(AttributeType.AccelerationForce) / Settings.TicksPerSecond;

                // Get the percentage of the overall thrusters' power we
                // still need to fulfill our quota.
                var load = Math.Min(1, desiredAcceleration * mass / accelerationForce);

                // And apply it to our energy and power values.
                energyConsumption *= load;
                accelerationForce *= load;

                // Make sure we have enough energy.
                var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
                if (energy.Value <= energyConsumption)
                {
                    // Not enough energy, adjust our output.
                    accelerationForce *= energy.Value / energyConsumption;
                    energyConsumption = energy.Value;
                }

                // Consume it.
                energy.SetValue(energy.Value - energyConsumption);

                // Get modified acceleration, adjust by our mass.
                accelerationForce /= mass;

                // Apply our acceleration. Use the min to our desired
                // acceleration so we don't exceed our target.
                acceleration.Value += accelerationDirection * Math.Min(desiredAcceleration, accelerationForce);

                // Adjust thruster PFX based on acceleration, if it just started.
                effects.SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, true);

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
