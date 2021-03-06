﻿using System;
using Engine.ComponentSystem.Physics.Math;
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
    /// <summary>
    ///     A revolute joint constrains two bodies to share a common point while they are free to rotate about the point.
    ///     The relative rotation about the shared point is the joint angle. You can limit the relative rotation with a joint
    ///     limit that specifies a lower and upper angle. You can use a motor to drive the relative rotation about the shared
    ///     point. A maximum motor torque is provided so that infinite forces are not generated.
    /// </summary>
    public sealed class RevoluteJoint : Joint
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

        /// <summary>Get the reference angle.</summary>
        public float ReferenceAngle
        {
            get { return _referenceAngle; }
        }

        /// <summary>Get the current joint angle in radians.</summary>
        public float JointAngle
        {
            get { return BodyB.Sweep.Angle - BodyA.Sweep.Angle - _referenceAngle; }
        }

        /// <summary>Get the current joint angle speed in radians per second.</summary>
        public float JointSpeed
        {
            get { return BodyB.AngularVelocityInternal - BodyA.AngularVelocityInternal; }
        }

        /// <summary>Set/Get the lower joint limit in radians.</summary>
        public float LowerLimit
        {
            get { return _lowerAngle; }
            set { SetLimits(value, _upperAngle); }
        }

        /// <summary>Set/Get the upper joint limit in radians.</summary>
        public float UpperLimit
        {
            get { return _upperAngle; }
            set { SetLimits(_lowerAngle, value); }
        }

        /// <summary>Set/Get whether the lower and upper angle limits are used.</summary>
        public bool IsLimitEnabled
        {
            get { return _enableLimit; }
            set
            {
                if (value != _enableLimit)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _enableLimit = value;
                    _impulse.Z = 0.0f;
                }
            }
        }

        /// <summary>Set/Get the motor speed in radians per second.</summary>
        public float MotorSpeed
        {
            get { return _motorSpeed; }
            set
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (value != _motorSpeed)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _motorSpeed = value;
                }
            }
        }

        /// <summary>Set/Get the maximum motor torque, usually in N-m.</summary>
        public float MaxMotorTorque
        {
            get { return _maxMotorTorque; }
            set
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (value != _maxMotorTorque)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _maxMotorTorque = value;
                }
            }
        }

        /// <summary>Set/Get whether the motor is enabled.</summary>
        public bool IsMotorEnabled
        {
            get { return _enableMotor; }
            set
            {
                if (value != _enableMotor)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _enableMotor = value;
                }
            }
        }

        #endregion

        #region Fields

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private Vector3 _impulse;

        private float _motorImpulse;

        private bool _enableMotor;

        private float _maxMotorTorque;

        private float _motorSpeed;

        private bool _enableLimit;

        private float _referenceAngle;

        private float _lowerAngle;

        private float _upperAngle;

        private LimitState _limitState;

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public Vector2 RotA;

            public Vector2 RotB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public float InverseMassA;

            public float InverseMassB;

            public float InverseInertiaA;

            public float InverseInertiaB;

            public Matrix33 Mass; // effective mass for point-to-point constraint.

            public float MotorMass; // effective mass for motor/limit angular constraint.
        }

        [CopyIgnore, PacketizeIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="RevoluteJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public RevoluteJoint() : base(JointType.Revolute) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(
            WorldPoint anchor,
            float lowerAngle = 0,
            float upperAngle = 0,
            float maxMotorTorque = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false)
        {
            _localAnchorA = BodyA.GetLocalPoint(anchor);
            _localAnchorB = BodyB.GetLocalPoint(anchor);
            _referenceAngle = BodyB.Angle - BodyA.Angle;

            _lowerAngle = lowerAngle;
            _upperAngle = upperAngle;
            _maxMotorTorque = maxMotorTorque;
            _motorSpeed = motorSpeed;
            _enableLimit = enableLimit;
            _enableMotor = enableMotor;

            _limitState = LimitState.Inactive;
        }

        #endregion

        #region Accessors

        /// <summary>Set the joint limits, usually in meters.</summary>
        public void SetLimits(float lower, float upper)
        {
            if (lower > upper)
            {
                throw new ArgumentException("lower must be less than or equal to upper", "lower");
            }

// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (lower != _lowerAngle || upper != _upperAngle)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                BodyA.IsAwake = true;
                BodyB.IsAwake = true;
                _impulse.Z = 0.0f;
                _lowerAngle = lower;
                _upperAngle = upper;
            }
        }

        #endregion

        #region Logic

        // Point-to-point constraint
        // C = p2 - p1
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Motor constraint
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2

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

            var aA = positions[_tmp.IndexA].Angle;
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;

            var aB = positions[_tmp.IndexB].Angle;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            _tmp.RotA = qA * (_localAnchorA - _tmp.LocalCenterA);
            _tmp.RotB = qB * (_localAnchorB - _tmp.LocalCenterB);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            var fixedRotation = (iA + iB == 0.0f);
// ReSharper restore CompareOfFloatsByEqualityOperator

            _tmp.Mass.Column1.X = mA + mB + _tmp.RotA.Y * _tmp.RotA.Y * iA + _tmp.RotB.Y * _tmp.RotB.Y * iB;
            _tmp.Mass.Column2.X = -_tmp.RotA.Y * _tmp.RotA.X * iA - _tmp.RotB.Y * _tmp.RotB.X * iB;
            _tmp.Mass.Column3.X = -_tmp.RotA.Y * iA - _tmp.RotB.Y * iB;
            _tmp.Mass.Column1.Y = _tmp.Mass.Column2.X;
            _tmp.Mass.Column2.Y = mA + mB + _tmp.RotA.X * _tmp.RotA.X * iA + _tmp.RotB.X * _tmp.RotB.X * iB;
            _tmp.Mass.Column3.Y = _tmp.RotA.X * iA + _tmp.RotB.X * iB;
            _tmp.Mass.Column1.Z = _tmp.Mass.Column3.X;
            _tmp.Mass.Column2.Z = _tmp.Mass.Column3.Y;
            _tmp.Mass.Column3.Z = iA + iB;

            _tmp.MotorMass = iA + iB;
            if (_tmp.MotorMass > 0.0f)
            {
                _tmp.MotorMass = 1.0f / _tmp.MotorMass;
            }

            if (!_enableMotor || fixedRotation)
            {
                _motorImpulse = 0.0f;
            }

            if (_enableLimit && !fixedRotation)
            {
                var jointAngle = aB - aA - _referenceAngle;
                if (System.Math.Abs(_upperAngle - _lowerAngle) < 2.0f * Settings.AngularSlop)
                {
                    _limitState = LimitState.Equal;
                }
                else if (jointAngle <= _lowerAngle)
                {
                    if (_limitState != LimitState.AtLower)
                    {
                        _impulse.Z = 0.0f;
                    }
                    _limitState = LimitState.AtLower;
                }
                else if (jointAngle >= _upperAngle)
                {
                    if (_limitState != LimitState.AtUpper)
                    {
                        _impulse.Z = 0.0f;
                    }
                    _limitState = LimitState.AtUpper;
                }
                else
                {
                    _limitState = LimitState.Inactive;
                    _impulse.Z = 0.0f;
                }
            }
            else
            {
                _limitState = LimitState.Inactive;
            }

            var p = new Vector2(_impulse.X, _impulse.Y);
            vA -= mA * p;
            wA -= iA * (Vector2Util.Cross(ref _tmp.RotA, ref p) + _motorImpulse + _impulse.Z);
            vB += mB * p;
            wB += iB * (Vector2Util.Cross(ref _tmp.RotB, ref p) + _motorImpulse + _impulse.Z);

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

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            var fixedRotation = (iA + iB == 0.0f);
// ReSharper restore CompareOfFloatsByEqualityOperator

            // Solve motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal && fixedRotation == false)
            {
                var cdot = wB - wA - _motorSpeed;
                var impulse = -_tmp.MotorMass * cdot;
                var oldImpulse = _motorImpulse;
                var maxImpulse = step.DeltaT * _maxMotorTorque;
                _motorImpulse = MathHelper.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive && fixedRotation == false)
            {
                var cdot1 = vB + Vector2Util.Cross(wB, ref _tmp.RotB) - vA - Vector2Util.Cross(wA, ref _tmp.RotA);
                var cdot2 = wB - wA;
                var cdot = new Vector3(cdot1.X, cdot1.Y, cdot2);

                var impulse = -_tmp.Mass.Solve33(cdot);

                if (_limitState == LimitState.Equal)
                {
                    _impulse += impulse;
                }
                else if (_limitState == LimitState.AtLower)
                {
                    var newImpulse = _impulse.Z + impulse.Z;
                    if (newImpulse < 0.0f)
                    {
                        var rhs = -cdot1 + _impulse.Z * new Vector2(_tmp.Mass.Column3.X, _tmp.Mass.Column3.Y);
                        var reduced = _tmp.Mass.Solve22(rhs);
                        impulse.X = reduced.X;
                        impulse.Y = reduced.Y;
                        impulse.Z = -_impulse.Z;
                        _impulse.X += reduced.X;
                        _impulse.Y += reduced.Y;
                        _impulse.Z = 0.0f;
                    }
                    else
                    {
                        _impulse += impulse;
                    }
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    var newImpulse = _impulse.Z + impulse.Z;
                    if (newImpulse > 0.0f)
                    {
                        var rhs = -cdot1 + _impulse.Z * new Vector2(_tmp.Mass.Column3.X, _tmp.Mass.Column3.Y);
                        var reduced = _tmp.Mass.Solve22(rhs);
                        impulse.X = reduced.X;
                        impulse.Y = reduced.Y;
                        impulse.Z = -_impulse.Z;
                        _impulse.X += reduced.X;
                        _impulse.Y += reduced.Y;
                        _impulse.Z = 0.0f;
                    }
                    else
                    {
                        _impulse += impulse;
                    }
                }

                var p = new Vector2(impulse.X, impulse.Y);

                vA -= mA * p;
                wA -= iA * (Vector2Util.Cross(ref _tmp.RotA, ref p) + impulse.Z);

                vB += mB * p;
                wB += iB * (Vector2Util.Cross(ref _tmp.RotB, ref p) + impulse.Z);
            }
            else
            {
                // Solve point-to-point constraint
                var cdot = vB + Vector2Util.Cross(wB, ref _tmp.RotB) - vA - Vector2Util.Cross(wA, ref _tmp.RotA);
                var impulse = _tmp.Mass.Solve22(-cdot);

                _impulse.X += impulse.X;
                _impulse.Y += impulse.Y;

                vA -= mA * impulse;
                wA -= iA * Vector2Util.Cross(ref _tmp.RotA, ref impulse);

                vB += mB * impulse;
                wB += iB * Vector2Util.Cross(ref _tmp.RotB, ref impulse);
            }

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

            var angularError = 0.0f;
            float positionError;

// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            var fixedRotation = (_tmp.InverseInertiaA + _tmp.InverseInertiaB == 0.0f);
// ReSharper restore CompareOfFloatsByEqualityOperator

            // Solve angular limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive && fixedRotation == false)
            {
                var angle = aB - aA - _referenceAngle;
                var limitImpulse = 0.0f;

                if (_limitState == LimitState.Equal)
                {
                    // Prevent large angular corrections
                    var c = MathHelper.Clamp(
                        angle - _lowerAngle,
                        -Settings.MaxAngularCorrection,
                        Settings.MaxAngularCorrection);
                    limitImpulse = -_tmp.MotorMass * c;
                    angularError = System.Math.Abs(c);
                }
                else if (_limitState == LimitState.AtLower)
                {
                    var c = angle - _lowerAngle;
                    angularError = -c;

                    // Prevent large angular corrections and allow some slop.
                    c = MathHelper.Clamp(c + Settings.AngularSlop, -Settings.MaxAngularCorrection, 0.0f);
                    limitImpulse = -_tmp.MotorMass * c;
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    var c = angle - _upperAngle;
                    angularError = c;

                    // Prevent large angular corrections and allow some slop.
                    c = MathHelper.Clamp(c - Settings.AngularSlop, 0.0f, Settings.MaxAngularCorrection);
                    limitImpulse = -_tmp.MotorMass * c;
                }

                aA -= _tmp.InverseInertiaA * limitImpulse;
                aB += _tmp.InverseInertiaB * limitImpulse;
            }

            // Solve point-to-point constraint.
            {
                qA.Set(aA);
                qB.Set(aB);
                var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
                var rB = qB * (_localAnchorB - _tmp.LocalCenterB);

// ReSharper disable RedundantCast Necessary for FarPhysics.
                var c = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast
                positionError = c.Length();

                var mA = _tmp.InverseMassA;
                var mB = _tmp.InverseMassB;
                var iA = _tmp.InverseInertiaA;
                var iB = _tmp.InverseInertiaB;

                Matrix22 k;
                k.Column1.X = mA + mB + iA * rA.Y * rA.Y + iB * rB.Y * rB.Y;
                k.Column1.Y = -iA * rA.X * rA.Y - iB * rB.X * rB.Y;
                k.Column2.X = k.Column1.Y;
                k.Column2.Y = mA + mB + iA * rA.X * rA.X + iB * rB.X * rB.X;

                var impulse = -k.Solve(c);

                cA -= mA * impulse;
                aA -= iA * Vector2Util.Cross(ref rA, ref impulse);

                cB += mB * impulse;
                aB += iB * Vector2Util.Cross(ref rB, ref impulse);
            }

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return positionError <= Settings.LinearSlop && angularError <= Settings.AngularSlop;
        }

        #endregion
    }
}