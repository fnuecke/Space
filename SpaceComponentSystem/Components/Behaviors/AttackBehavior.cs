using System;
using System.IO;
using Engine.ComponentSystem.Spatial.Components;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>AIs in this state try to destroy a target entity</summary>
    internal sealed class AttackBehavior : Behavior
    {
        #region Fields

        /// <summary>The entity we want to destroy. Utterly.</summary>
        public int Target;

        /// <summary>The position we were at when we started attacking.</summary>
        [PacketizeIgnore]
        private FarPosition? _start;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="AttackBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public AttackBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0.25f) {}

        /// <summary>Reset this behavior so it can be reused later on.</summary>
        public override void Reset()
        {
            base.Reset();

            Target = 0;
            _start = null;
        }

        #endregion

        #region Logic

        /// <summary>Behavior specific update logic, e.g. checking for nearby enemies.</summary>
        /// <returns>Whether to do the rest of the update.</returns>
        protected override bool UpdateInternal()
        {
            // Get our ship control.
            var control = ((ShipControl) AI.Manager.GetComponent(AI.Entity, ShipControl.TypeId));

            // If our target died, we're done here.
            if (!AI.Manager.HasEntity(Target))
            {
                control.Shooting = false;
                _start = null;
                AI.PopBehavior();
                return false;
            }

            // Get our position.
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;

            // Check if we've traveled too far.
            if (_start.HasValue)
            {
                if (FarPosition.DistanceSquared(_start.Value, position) >
                    AI.Configuration.ChaseDistance * AI.Configuration.ChaseDistance)
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
            var info = (ShipInfo) AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId);

            // Target still lives, see how far away it is.
            var targetPosition = ((ITransform) AI.Manager.GetComponent(Target, TransformTypeId)).Position;
            var toTarget = (Vector2) (targetPosition - position);
            var targetAngle = (float) Math.Atan2(toTarget.Y, toTarget.X);
            var weaponRange = info.WeaponRange + AI.Configuration.WeaponRangeEpsilon;

            // If we're close enough and the target is somewhat in front of us, open fire.
            control.Shooting = (toTarget.LengthSquared() < weaponRange * weaponRange) &&
                               Math.Abs(Angle.MinAngle(targetAngle, info.Rotation)) <
                               AI.Configuration.WeaponFiringAngle * 0.5f;

            // If we're in a squad we want the other members to help us.
            var squad = (Squad) AI.Manager.GetComponent(AI.Entity, Squad.TypeId);
            if (squad != null)
            {
                foreach (var member in squad.Members)
                {
                    // Skip self, we're already busy.
                    if (member == AI.Entity)
                    {
                        continue;
                    }
                    var ai = (ArtificialIntelligence) AI.Manager.GetComponent(member, ArtificialIntelligence.TypeId);
                    if (ai != null && ai.CurrentBehavior != ArtificialIntelligence.BehaviorType.Attack)
                    {
                        ai.Attack(Target);
                    }
                }
            }

            // All OK.
            return true;
        }

        /// <summary>
        ///     Called when an entity becomes an invalid target (removed from the system or died). This is intended to allow
        ///     behaviors to stop in case their related entity is removed (e.g. target when attacking).
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        internal override void OnEntityInvalidated(int entity)
        {
            if (entity == Target)
            {
                Target = 0;
            }
        }

        #endregion

        #region Behavior type specifics

        /// <summary>Figure out where we want to go.</summary>
        /// <returns>The coordinate we want to fly to.</returns>
        protected override FarPosition GetTargetPosition()
        {
            // We can just fly straight at our enemy, the vegetative evasion mechanism
            // will prevent us to crash into it, as well as lead to a circling effect.
            return ((ITransform) AI.Manager.GetComponent(Target, TransformTypeId)).Position;
        }

        /// <summary>Gets the target rotation we want to be facing.</summary>
        /// <param name="direction">The direction we're accelerating in.</param>
        /// <returns>The desired target rotation.</returns>
        protected override float GetTargetRotation(Vector2 direction)
        {
            // Always try to face our target.
            // TODO predict which way we need to face to actually hit
            //      based on target velocity and weapon projectile speed...
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;
            var targetPosition = ((ITransform) AI.Manager.GetComponent(Target, TransformTypeId)).Position;
            var toTarget = (Vector2) (targetPosition - position);
            return (float) Math.Atan2(toTarget.Y, toTarget.X);
        }

        /// <summary>How fast do we want to fly, relative to our maximum speed?</summary>
        /// <returns>The relative speed we want to fly at.</returns>
        protected override float GetThrusterPower()
        {
            // Get our ship info.
            var info = ((ShipInfo) AI.Manager.GetComponent(AI.Entity, ShipInfo.TypeId));

            // Decrease output if we're getting closer to our target than
            // we need to to shoot.
            var position = ((ITransform) AI.Manager.GetComponent(AI.Entity, TransformTypeId)).Position;
            var targetPosition = ((ITransform) AI.Manager.GetComponent(Target, TransformTypeId)).Position;
            var toTarget = (Vector2) (targetPosition - position);
            var weaponRange = info.WeaponRange;
            var thrusterPower = Math.Min(1, toTarget.LengthSquared() / (weaponRange * weaponRange));

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

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet).Write(_start.HasValue);
            if (_start.HasValue)
            {
                packet.Write(_start.Value);
            }
            return packet;
        }

        /// <summary>
        ///     Bring the object to the state in the given packet. This is called after automatic depacketization has been
        ///     performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            if (packet.ReadBoolean())
            {
                _start = packet.ReadFarPosition();
            }
        }

        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Start = ");
            w.Write(_start.HasValue ? _start.Value.ToString() : "null");

            return w;
        }

        #endregion
    }
}