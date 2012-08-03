using System;
using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
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
        private FarPosition? _start;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public AttackBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 10)
        {
        }

        /// <summary>
        /// Reset this behavior so it can be reused later on.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Target = 0;
            _start = null;
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
            var control = ((ShipControl)AI.Manager.GetComponent(AI.Entity, ShipControl.TypeId));

            // If our target died, we're done here.
            if (!AI.Manager.HasEntity(Target))
            {
                control.Shooting = false;
                _start = null;
                AI.PopBehavior();
                return false;
            }

            // Get our position.
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;

            // Check if we've traveled too far.
            if (_start.HasValue)
            {
                if (((Vector2)(position - _start.Value)).LengthSquared() > ChaseDistance * ChaseDistance)
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
            var info = ((ShipInfo)AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId));

            // Target still lives, see how far away it is.
            var targetPosition = ((Transform)AI.Manager.GetComponent(Target, Transform.TypeId)).Translation;
            var toTarget = (Vector2)(position - targetPosition);

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
        protected override FarPosition GetTargetPosition()
        {
            // Don't fly directly towards the enemy, but to a point slightly
            // offset, and at something slightly above the flocking separation.
            // This will lead to a circling effect.
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var targetPosition = ((Transform)AI.Manager.GetComponent(Target, Transform.TypeId)).Translation;
            var toTarget = (Vector2)(position - targetPosition);
            var separationIntercept = 1 - FlockingSeparation * FlockingSeparation / toTarget.LengthSquared();
            FarPosition separationPosition;
            FarPosition.Lerp(ref position, ref targetPosition, separationIntercept, out separationPosition);
            if (toTarget != Vector2.Zero && separationIntercept > 0 && separationIntercept < 0.7f)
            {
                toTarget.Normalize();
                separationPosition.X += toTarget.Y * 50 / separationIntercept;
                separationPosition.Y -= toTarget.X * 50 / separationIntercept;
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
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var targetPosition = ((Transform)AI.Manager.GetComponent(Target, Transform.TypeId)).Translation;
            var toTarget = (Vector2)(targetPosition - position);
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
            var info = ((ShipInfo)AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId));

            // Decrease output if we're getting closer to our target.
            var position = ((Transform)AI.Manager.GetComponent(AI.Entity, Transform.TypeId)).Translation;
            var targetPosition = ((Transform)AI.Manager.GetComponent(Target, Transform.TypeId)).Translation;
            var toTarget = (Vector2)(position - targetPosition);
            var weaponRange = info.WeaponRange;
            var thrusterPower = Math.Min(1, toTarget.LengthSquared() / weaponRange);

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
            base.Packetize(packet)
                .Write(Target)
                .Write(_start.HasValue);
            if (_start.HasValue)
            {
                packet.Write(_start.Value);
            }
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Target = packet.ReadInt32();
            if (packet.ReadBoolean())
            {
                _start = packet.ReadVector2();
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Target);
            hasher.Put(_start.HasValue ? _start.Value : Vector2.Zero);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public override void CopyInto(Behavior into)
        {
            base.CopyInto(into);

            var copy = (AttackBehavior)into;

            copy.Target = Target;
            copy._start = _start;
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
            return base.ToString() + ", Target=" + Target + ", Start=" + (_start.HasValue ? _start.Value.ToString() : "null");
        }

        #endregion
    }
}
