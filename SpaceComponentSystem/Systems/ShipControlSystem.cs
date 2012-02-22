using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles input to control how a ship flies, i.e. its acceleration and
    /// rotation, as well as whether it's shooting or not.
    /// </summary>
    public sealed class ShipControlSystem : AbstractComponentSystem<ShipControl>
    {
        #region Logic
        
        protected override void UpdateComponent(GameTime gameTime, long frame, ShipControl component)
        {
            // Get components we depend upon / modify.
            var transform = Manager.GetComponent<Transform>(component.Entity);
            var spin = Manager.GetComponent<Spin>(component.Entity);
            var character = Manager.GetComponent<Character<AttributeType>>(component.Entity);
            var info = Manager.GetComponent<ShipInfo>(component.Entity);
            var weaponControl = Manager.GetComponent<WeaponControl>(component.Entity);
            var acceleration = Manager.GetComponent<Acceleration>(component.Entity);
            var effects = Manager.GetComponent<ParticleEffects>(component.Entity);

            // Get the mass of the ship.
            float mass = info.Mass;

            // Get our acceleration direction, based on whether we're
            // currently stabilizing or not.
            Vector2 accelerationDirection;
            var desiredAcceleration = float.MaxValue;
            if (component.DirectedAcceleration == Vector2.Zero && component.Stabilizing)
            {
                // We want to stabilize.
                accelerationDirection = -Manager.GetComponent<Velocity>(component.Entity).Value;
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
                var energyConsumption = character.GetValue(AttributeType.ThrusterEnergyConsumption) / 60f;
                var accelerationForce = character.GetValue(AttributeType.AccelerationForce);

                // Get the percentage of the overall thrusters' power we
                // still need to fulfill our quota.
                var load = Math.Min(1, desiredAcceleration * mass / accelerationForce);

                // And apply it to our energy and power values.
                energyConsumption *= load;
                accelerationForce *= load;

                // Make sure we have enough energy.
                var energy = Manager.GetComponent<Energy>(component.Entity);
                if (energy.Value <= energyConsumption)
                {
                    // Not enough energy, adjust our output.
                    accelerationForce *= energy.Value / energyConsumption;
                    energyConsumption = energy.Value;
                }

                // Consume it.
                energy.SetValue(energy.Value - energyConsumption);

                // Get modified acceleration, adjust by our mass.
                accelerationForce = character.GetValue(AttributeType.AccelerationForce, accelerationForce) / mass;

                // Adjust thruster PFX based on acceleration, if it just started.
                if (acceleration.Value == Vector2.Zero)
                {
                    var numThrusters = info.EquipmentSlotCount<Thruster>();
                    for (int i = 0; i < numThrusters; i++)
                    {
                        var thrusterId = info.EquipmentItemAt<Thruster>(i);
                        if (thrusterId.HasValue)
                        {
                            // TODO: get offset for that item slot and use it
                            effects.TryAdd("Effects/thruster", Vector2.Zero);
                        }
                    }
                }

                // Apply our acceleration. Use the min to our desired
                // acceleration so we don't exceed our target.
                acceleration.Value = accelerationDirection * Math.Min(desiredAcceleration, accelerationForce);

            }
            else
            {
                // Not accelerating. Disable thruster effects if we were accelerating before.
                if (acceleration.Value != Vector2.Zero)
                {
                    effects.Remove("Effects/thruster");
                }

                // Adjust acceleration value.
                acceleration.Value = Vector2.Zero;
            }

            // Compute rotation speed. Yes, this is actually the rotation acceleration,
            // but whatever...
            var rotation = character.GetValue(AttributeType.RotationForce) / mass;

            // Update rotation / spin.
            var currentDelta = Angle.MinAngle(transform.Rotation, component.TargetRotation);
            var requiredSpin = (currentDelta > 0
                        ? DirectionConversion.DirectionToScalar(Directions.Right)
                        : DirectionConversion.DirectionToScalar(Directions.Left))
                        * rotation;

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
