﻿using Engine.ComponentSystem.Physics.Math;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Joints
{
    /// <summary>A rope joint enforces a maximum distance between two points on two bodies. It has no other effect.</summary>
    /// <remarks>
    ///     Changing the maximum length during the simulation would result in non-physical behavior, thus it is not
    ///     allowed. A model that would allow you to dynamically modify the length would have some sponginess, so Erin chose
    ///     not to implement it that way. See b2DistanceJoint if you want to dynamically control length.
    /// </remarks>
    public sealed class RopeJoint : Joint
    {
        #region Properties

        /// <summary>Get the anchor point on the first body in world coordinates.</summary>
        public override WorldPoint AnchorA
        {
            get { return BodyA.GetWorldPoint(_localAnchorA); }
        }

        /// <summary>Get the anchor point on the second body in world coordinates.</summary>
        public override WorldPoint AnchorB
        {
            get { return BodyB.GetWorldPoint(_localAnchorB); }
        }

        /// <summary>Get the anchor point on the first body in local coordinates.</summary>
        public LocalPoint LocalAnchorA
        {
            get { return _localAnchorA; }
        }

        /// <summary>Get the anchor point on the second body in local coordinates.</summary>
        public LocalPoint LocalAnchorB
        {
            get { return _localAnchorB; }
        }

        /// <summary>Set/Get the maximum length of the rope.</summary>
        public float MaxLength
        {
            get { return _maxLength; }
            set { _maxLength = value; }
        }

        #endregion

        #region Fields

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _maxLength;

        private float _length;

        private float _impulse;

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public Vector2 Axis;

            public Vector2 RotA;

            public Vector2 RotB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public float InverseMassA;

            public float InverseMassB;

            public float InverseInertiaA;

            public float InverseInertiaB;

            public float Mass;
        }

        [CopyIgnore, PacketizeIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="RopeJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public RopeJoint() : base(JointType.Rope) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(WorldPoint anchorA, WorldPoint anchorB, float length)
        {
            _localAnchorA = BodyA.GetLocalPoint(anchorA);
            _localAnchorB = BodyB.GetLocalPoint(anchorB);
            _maxLength = length;

            _impulse = 0;
            _length = 0;
        }

        #endregion

        #region Logic

        // Limit:
        // C = norm(pB - pA) - L
        // u = (pB - pA) / norm(pB - pA)
        // Cdot = dot(u, vB + cross(wB, rB) - vA - cross(wA, rA))
        // J = [-u -cross(rA, u) u cross(rB, u)]
        // K = J * invM * JT
        //   = invMassA + invIA * cross(rA, u)^2 + invMassB + invIB * cross(rB, u)^2

        /// <summary>Initializes the velocity constraints.</summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void InitializeVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            _tmp.IndexA = BodyA.IslandIndex;
            _tmp.IndexB = BodyB.IslandIndex;
            _tmp.LocalCenterA = BodyA.Sweep.LocalCenter;
            _tmp.LocalCenterB = BodyB.Sweep.LocalCenter;
            _tmp.InverseMassA = BodyA.InverseMass;
            _tmp.InverseMassB = BodyB.InverseMass;
            _tmp.InverseInertiaA = BodyA.InverseInertia;
            _tmp.InverseInertiaB = BodyB.InverseInertia;

            var cA = positions[_tmp.IndexA].Point;
            var aA = positions[_tmp.IndexA].Angle;
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;

            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            _tmp.RotA = qA * (_localAnchorA - _tmp.LocalCenterA);
            _tmp.RotB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            _tmp.Axis = (Vector2) (cB - cA) + (_tmp.RotB - _tmp.RotA);
// ReSharper restore RedundantCast

            _length = _tmp.Axis.Length();

            if (_length > Settings.LinearSlop)
            {
                _tmp.Axis *= 1.0f / _length;
            }
            else
            {
                _tmp.Axis = Vector2.Zero;
                _tmp.Mass = 0.0f;
                _impulse = 0.0f;
                return;
            }

            // Compute effective mass.
            var crA = Vector2Util.Cross(ref _tmp.RotA, ref _tmp.Axis);
            var crB = Vector2Util.Cross(ref _tmp.RotB, ref _tmp.Axis);
            var invMass = _tmp.InverseMassA + _tmp.InverseInertiaA * crA * crA + _tmp.InverseMassB +
                          _tmp.InverseInertiaB * crB * crB;

// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (invMass != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                _tmp.Mass = 1.0f / invMass;
            }
            else
            {
                _tmp.Mass = 0.0f;
            }

            var p = _impulse * _tmp.Axis;
            vA -= _tmp.InverseMassA * p;
            wA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref _tmp.RotA, ref p);
            vB += _tmp.InverseMassB * p;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(ref _tmp.RotB, ref p);

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
        }

        /// <summary>Solves the velocity constraints.</summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void SolveVelocityConstraints(TimeStep step, Velocity[] velocities)
        {
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            // Cdot = dot(u, v + cross(w, r))
            var vpA = vA + Vector2Util.Cross(wA, ref _tmp.RotA);
            var vpB = vB + Vector2Util.Cross(wB, ref _tmp.RotB);
            var c = _length - _maxLength;
            var cdot = Vector2Util.Dot(_tmp.Axis, vpB - vpA);

            // Predictive constraint.
            if (c < 0.0f)
            {
                cdot += step.InverseDeltaT * c;
            }

            var impulse = -_tmp.Mass * cdot;
            var oldImpulse = _impulse;
            _impulse = System.Math.Min(0.0f, _impulse + impulse);
            impulse = _impulse - oldImpulse;

            var p = impulse * _tmp.Axis;
            vA -= _tmp.InverseMassA * p;
            wA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref _tmp.RotA, ref p);
            vB += _tmp.InverseMassB * p;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(ref _tmp.RotB, ref p);

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
        }

        /// <summary>This returns true if the position errors are within tolerance, allowing an early exit from the iteration loop.</summary>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <returns>
        ///     <c>true</c> if the position errors are within tolerance.
        /// </returns>
        internal override bool SolvePositionConstraints(Position[] positions)
        {
            var cA = positions[_tmp.IndexA].Point;
            var aA = positions[_tmp.IndexA].Angle;
            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var u = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            var length = u.Length();
            u /= length;
            var c = length - _maxLength;

            c = MathHelper.Clamp(c, 0.0f, Settings.MaxLinearCorrection);

            var impulse = -_tmp.Mass * c;
            var p = impulse * u;

            cA -= _tmp.InverseMassA * p;
            aA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref rA, ref p);
            cB += _tmp.InverseMassB * p;
            aB += _tmp.InverseInertiaB * Vector2Util.Cross(ref rB, ref p);

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return length - _maxLength < Settings.LinearSlop;
        }

        #endregion
    }
}