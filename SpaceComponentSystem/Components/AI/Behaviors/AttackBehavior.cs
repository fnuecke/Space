using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    /// <summary>
    /// AIs in this state try to destroy a target entity 
    /// </summary>
    internal sealed class AttackBehavior : Behavior
    {
        #region Constants

        /// <summary>
        /// How much earlier to start firing our weapons, before the target
        /// enters our range of fire. This way we have a chance some shots
        /// will hit the target because it's flying into them.
        /// </summary>
        private const float WeaponRangeEpsilon = 100;

        /// <summary>
        /// Distance we have to have to where we were when we started chasing
        /// our target before we give up and fall back to our previous behavior.
        /// </summary>
        private const float ChaseDistance = 2000;

        #endregion

        #region Fields

        /// <summary>
        /// The entity we want to destroy. Utterly.
        /// </summary>
        public int Target;

        /// <summary>
        /// The position we were at when we started attacking.
        /// </summary>
        private Vector2? _start;

        #endregion

        #region Constructor
        
        public AttackBehavior(ArtificialIntelligence ai)
            : base(ai, 10)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Behavior specific update logic, e.g. checking for nearby enemies.
        /// </summary>
        /// <returns>
        /// Whether to do the rest of the update.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // Get our ship control.
            var control = AI.Manager.GetComponent<ShipControl>(AI.Entity);

            // If our target died, we're done here.
            if (!AI.Manager.HasEntity(Target))
            {
                control.Shooting = false;
                _start = null;
                AI.PopBehavior();
                return false;
            }

            // Get our position.
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;

            // Check if we've traveled too far.
            if (_start.HasValue)
            {
                if ((position - _start.Value).LengthSquared() > ChaseDistance * ChaseDistance)
                {
                    // Yeah, that's it, let's give up and return to what we
                    // were doing before.
                    control.Shooting = false;
                    _start = null;
                    AI.PopBehavior();
                    return false;
                }
            }
            else
            {
                // Not set yet, do it now.
                _start = position;
            }

            // Get our ship info.
            var info = AI.Manager.GetComponent<ShipInfo>(AI.Entity);

            // Target still lives, see how far away it is.
            var targetPosition = AI.Manager.GetComponent<Transform>(Target).Translation;
            var toTarget = position - targetPosition;

            // If we're close enough, open fire.
            var weaponRange = info.WeaponRange + WeaponRangeEpsilon;
            control.Shooting = (toTarget.LengthSquared() < weaponRange * weaponRange);

            // All OK.
            return true;
        }

        #endregion

        #region Behavior type specifics

        /// <summary>
        /// Figure out where we want to go.
        /// </summary>
        /// <returns>
        /// The coordinate we want to fly to.
        /// </returns>
        protected override Vector2 GetTargetPosition()
        {
            // Don't fly directly towards the enemy, but to a point slightly
            // offset, and at something slightly above the flocking separation.
            // This will lead to a circling effect.
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            var targetPosition = AI.Manager.GetComponent<Transform>(Target).Translation;
            var toTarget = position - targetPosition;
            var separationIntercept = 1 - FlockingSeparation * FlockingSeparation / toTarget.LengthSquared();
            Vector2 separationPosition;
            Vector2.Lerp(ref position, ref targetPosition, separationIntercept, out separationPosition);
            if (toTarget != Vector2.Zero && separationIntercept < 0.25f)
            {
                toTarget.Normalize();
                separationPosition.X += toTarget.Y * 50;
                separationPosition.Y -= toTarget.X * 50;
            }

            return separationPosition;
        }

        /// <summary>
        /// Gets the target rotation we want to be facing.
        /// </summary>
        /// <param name="direction">The direction we're accelerating in.</param>
        /// <returns>
        /// The desired target rotation.
        /// </returns>
        protected override float GetTargetRotation(ref Vector2 direction)
        {
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            var targetPosition = AI.Manager.GetComponent<Transform>(Target).Translation;
            var toTarget = targetPosition - position;
            return (float)Math.Atan2(toTarget.Y, toTarget.X);
        }

        /// <summary>
        /// How fast do we want to fly, relative to our maximum speed?
        /// </summary>
        /// <returns>
        /// The relative speed we want to fly at.
        /// </returns>
        protected override float GetThrusterPower()
        {
            // Get our ship info.
            var info = AI.Manager.GetComponent<ShipInfo>(AI.Entity);

            // Decrease output if we're getting closer to our target.
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            var targetPosition = AI.Manager.GetComponent<Transform>(Target).Translation;
            var toTarget = position - targetPosition;
            var weaponRange = info.WeaponRange;
            var thrusterPower = (float)Math.Min(1, toTarget.LengthSquared() / weaponRange);

            // Decrease output based on how much energy we have left,
            // to avoid consuming all our energy for fuel when we could
            // shoot instead.
            if (info.RelativeEnergy < 0.1f)
            {
                thrusterPower = 0.3f;
            }
            else if (info.RelativeEnergy < 0.25f)
            {
                thrusterPower *= 0.5f;
            }
            else if (info.RelativeEnergy < 0.5f)
            {
                thrusterPower *= 0.8f;
            }

            return thrusterPower;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Target);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Target = packet.ReadInt32();
        }

        #endregion
    }
}
