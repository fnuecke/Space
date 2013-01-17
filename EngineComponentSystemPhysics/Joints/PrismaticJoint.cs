using System;
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
    ///     A prismatic joint. This joint provides one degree of freedom: translation along an axis fixed in bodyA.
    ///     Relative rotation is prevented. You can use a joint limit to restrict the range of motion and a joint motor to
    ///     drive the motion or to model joint friction.
    /// </summary>
    public sealed class PrismaticJoint : Joint
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

        /// <summary>The local joint axis relative to the first body.</summary>
        public Vector2 LocalAxisA
        {
            get { return _localXAxisA; }
        }

        /// <summary>Get the reference angle.</summary>
        public float ReferenceAngle
        {
            get { return _referenceAngle; }
        }

        /// <summary>Get the current joint translation, usually in meters.</summary>
        public float JointTranslation
        {
            get
            {
                var pA = BodyA.GetWorldPoint(_localAnchorA);
                var pB = BodyB.GetWorldPoint(_localAnchorB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var d = (Vector2) (pB - pA);
// ReSharper restore RedundantCast
                var axis = BodyA.GetWorldVector(_localXAxisA);
                return Vector2Util.Dot(ref d, ref axis);
            }
        }

        /// <summary>Get the current joint translation speed, usually in meters per second.</summary>
        public float JointSpeed
        {
            get
            {
                var bA = BodyA;
                var bB = BodyB;

                var rA = bA.Transform.Rotation * (_localAnchorA - bA.Sweep.LocalCenter);
                var rB = bB.Transform.Rotation * (_localAnchorB - bB.Sweep.LocalCenter);
                var p1 = bA.Sweep.CenterOfMass + rA;
                var p2 = bB.Sweep.CenterOfMass + rB;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var d = (Vector2) (p2 - p1);
// ReSharper restore RedundantCast
                var axis = bA.Transform.Rotation * _localXAxisA;

                var vA = bA.LinearVelocityInternal;
                var vB = bB.LinearVelocityInternal;
                var wA = bA.AngularVelocityInternal;
                var wB = bB.AngularVelocityInternal;

                return Vector2Util.Dot(d, Vector2Util.Cross(wA, ref axis)) +
                       Vector2Util.Dot(axis, vB + Vector2Util.Cross(wB, ref rB) - vA - Vector2Util.Cross(wA, ref rA));
            }
        }

        /// <summary>Set/Get whether the joint limit is enabled.</summary>
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

        /// <summary>Set/Get the lower joint limit, usually in meters.</summary>
        public float LowerLimit
        {
            get { return _lowerTranslation; }
            set { SetLimits(value, _upperTranslation); }
        }

        /// <summary>Set/Get the upper joint limit, usually in meters.</summary>
        public float UpperLimit
        {
            get { return _upperTranslation; }
            set { SetLimits(_lowerTranslation, value); }
        }

        /// <summary>Set/Get whether the joint motor is enabled.</summary>
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

        /// <summary>Set/Get the motor speed, usually in meters per second.</summary>
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

        /// <summary>Set/Get the maximum motor force, usually in N.</summary>
        public float MaxMotorForce
        {
            get { return _maxMotorForce; }
            set
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (value != _maxMotorForce)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _maxMotorForce = value;
                }
            }
        }

        #endregion

        #region Fields

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private Vector2 _localXAxisA;

        private Vector2 _localYAxisA;

        private float _referenceAngle;

        private Vector3 _impulse;

        private float _motorImpulse;

        private float _lowerTranslation;

        private float _upperTranslation;

        private float _maxMotorForce;

        private float _motorSpeed;

        private bool _enableLimit;

        private bool _enableMotor;

        private LimitState _limitState;

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public float InverseMassA;

            public float InverseMassB;

            public float InverseInertiaA;

            public float InverseInertiaB;

            public Vector2 Axis, Perp;

            public float S1, S2;

            public float A1, A2;

            public Matrix33 K;

            public float MotorMass;
        }

        [CopyIgnore, PacketizerIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrismaticJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public PrismaticJoint() : base(JointType.Prismatic) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(
            WorldPoint anchor,
            Vector2 axis,
            float lowerTranslation = 0,
            float upperTranslation = 0,
            float maxMotorForce = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false)
        {
            _localAnchorA = BodyA.GetLocalPoint(anchor);
            _localAnchorB = BodyB.GetLocalPoint(anchor);
            _localXAxisA = BodyA.GetLocalVector(axis);
            _localXAxisA.Normalize();
            _localYAxisA = Vector2Util.Cross(1.0f, ref _localXAxisA);
            _referenceAngle = BodyB.Angle - BodyA.Angle;
            _lowerTranslation = lowerTranslation;
            _upperTranslation = upperTranslation;
            _maxMotorForce = maxMotorForce;
            _motorSpeed = motorSpeed;
            _enableLimit = enableLimit;
            _enableMotor = enableMotor;

            _impulse = Vector3.Zero;
            _motorImpulse = 0.0f;
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
            if (lower != _lowerTranslation ||
                upper != _upperTranslation)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                BodyA.IsAwake = true;
                BodyB.IsAwake = true;
                _lowerTranslation = lower;
                _upperTranslation = upper;
                _impulse.Z = 0.0f;
            }
        }

        #endregion

        #region Logic

        // Linear constraint (point-to-line)
        // d = p2 - p1 = x2 + r2 - x1 - r1
        // C = dot(perp, d)
        // Cdot = dot(d, cross(w1, perp)) + dot(perp, v2 + cross(w2, r2) - v1 - cross(w1, r1))
        //      = -dot(perp, v1) - dot(cross(d + r1, perp), w1) + dot(perp, v2) + dot(cross(r2, perp), v2)
        // J = [-perp, -cross(d + r1, perp), perp, cross(r2,perp)]
        //
        // Angular constraint
        // C = a2 - a1 + a_initial
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        //
        // K = J * invM * JT
        //
        // J = [-a -s1 a s2]
        //     [0  -1  0  1]
        // a = perp
        // s1 = cross(d + r1, a) = cross(p2 - x1, a)
        // s2 = cross(r2, a) = cross(p2 - x2, a)

        // Motor/Limit linear constraint
        // C = dot(ax1, d)
        // Cdot = = -dot(ax1, v1) - dot(cross(d + r1, ax1), w1) + dot(ax1, v2) + dot(cross(r2, ax1), v2)
        // J = [-ax1 -cross(d+r1,ax1) ax1 cross(r2,ax1)]

        // Block Solver
        // We develop a block solver that includes the joint limit. This makes the limit stiff (inelastic) even
        // when the mass has poor distribution (leading to large torques about the joint anchor points).
        //
        // The Jacobian has 3 rows:
        // J = [-uT -s1 uT s2] // linear
        //     [0   -1   0  1] // angular
        //     [-vT -a1 vT a2] // limit
        //
        // u = perp
        // v = axis
        // s1 = cross(d + r1, u), s2 = cross(r2, u)
        // a1 = cross(d + r1, v), a2 = cross(r2, v)

        // M * (v2 - v1) = JT * df
        // J * v2 = bias
        //
        // v2 = v1 + invM * JT * df
        // J * (v1 + invM * JT * df) = bias
        // K * df = bias - J * v1 = -Cdot
        // K = J * invM * JT
        // Cdot = J * v1 - bias
        //
        // Now solve for f2.
        // df = f2 - f1
        // K * (f2 - f1) = -Cdot
        // f2 = invK * (-Cdot) + f1
        //
        // Clamp accumulated limit impulse.
        // lower: f2(3) = max(f2(3), 0)
        // upper: f2(3) = min(f2(3), 0)
        //
        // Solve for correct f2(1:2)
        // K(1:2, 1:2) * f2(1:2) = -Cdot(1:2) - K(1:2,3) * f2(3) + K(1:2,1:3) * f1
        //                       = -Cdot(1:2) - K(1:2,3) * f2(3) + K(1:2,1:2) * f1(1:2) + K(1:2,3) * f1(3)
        // K(1:2, 1:2) * f2(1:2) = -Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3)) + K(1:2,1:2) * f1(1:2)
        // f2(1:2) = invK(1:2,1:2) * (-Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3))) + f1(1:2)
        //
        // Now compute impulse to be applied:
        // df = f2 - f1

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

            // Compute the effective masses.
            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var d = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            // Compute motor Jacobian and effective mass.
            {
                _tmp.Axis = qA * _localXAxisA;
                _tmp.A1 = Vector2Util.Cross(d + rA, _tmp.Axis);
                _tmp.A2 = Vector2Util.Cross(rB, _tmp.Axis);

                _tmp.MotorMass = mA + mB + iA * _tmp.A1 * _tmp.A1 + iB * _tmp.A2 * _tmp.A2;
                if (_tmp.MotorMass > 0.0f)
                {
                    _tmp.MotorMass = 1.0f / _tmp.MotorMass;
                }
            }

            // Prismatic constraint.
            {
                _tmp.Perp = qA * _localYAxisA;

                _tmp.S1 = Vector2Util.Cross(d + rA, _tmp.Perp);
                _tmp.S2 = Vector2Util.Cross(rB, _tmp.Perp);

                var k11 = mA + mB + iA * _tmp.S1 * _tmp.S1 + iB * _tmp.S2 * _tmp.S2;
                var k12 = iA * _tmp.S1 + iB * _tmp.S2;
                var k13 = iA * _tmp.S1 * _tmp.A1 + iB * _tmp.S2 * _tmp.A2;
                var k22 = iA + iB;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (k22 == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    // For bodies with fixed rotation.
                    k22 = 1.0f;
                }
                var k23 = iA * _tmp.A1 + iB * _tmp.A2;
                var k33 = mA + mB + iA * _tmp.A1 * _tmp.A1 + iB * _tmp.A2 * _tmp.A2;

                Vector3Util.Set(out _tmp.K.Column1, k11, k12, k13);
                Vector3Util.Set(out _tmp.K.Column2, k12, k22, k23);
                Vector3Util.Set(out _tmp.K.Column3, k13, k23, k33);
            }

            // Compute motor and limit terms.
            if (_enableLimit)
            {
                var jointTranslation = Vector2Util.Dot(ref _tmp.Axis, ref d);
                if (System.Math.Abs(_upperTranslation - _lowerTranslation) < 2.0f * Settings.LinearSlop)
                {
                    _limitState = LimitState.Equal;
                }
                else if (jointTranslation <= _lowerTranslation)
                {
                    if (_limitState != LimitState.AtLower)
                    {
                        _limitState = LimitState.AtLower;
                        _impulse.Z = 0.0f;
                    }
                }
                else if (jointTranslation >= _upperTranslation)
                {
                    if (_limitState != LimitState.AtUpper)
                    {
                        _limitState = LimitState.AtUpper;
                        _impulse.Z = 0.0f;
                    }
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
                _impulse.Z = 0.0f;
            }

            if (_enableMotor == false)
            {
                _motorImpulse = 0.0f;
            }

            var p = _impulse.X * _tmp.Perp + (_motorImpulse + _impulse.Z) * _tmp.Axis;
            var lA = _impulse.X * _tmp.S1 + _impulse.Y + (_motorImpulse + _impulse.Z) * _tmp.A1;
            var lB = _impulse.X * _tmp.S2 + _impulse.Y + (_motorImpulse + _impulse.Z) * _tmp.A2;
            vA -= mA * p;
            wA -= iA * lA;
            vB += mB * p;
            wB += iB * lB;

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

            // Solve linear motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal)
            {
                var cdot = Vector2Util.Dot(_tmp.Axis, vB - vA) + _tmp.A2 * wB - _tmp.A1 * wA;
                var impulse = _tmp.MotorMass * (_motorSpeed - cdot);
                var oldImpulse = _motorImpulse;
                var maxImpulse = step.DeltaT * _maxMotorForce;
                _motorImpulse = MathHelper.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                var p = impulse * _tmp.Axis;
                var lA = impulse * _tmp.A1;
                var lB = impulse * _tmp.A2;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }

            Vector2 cdot1;
            cdot1.X = Vector2Util.Dot(_tmp.Perp, vB - vA) + _tmp.S2 * wB - _tmp.S1 * wA;
            cdot1.Y = wB - wA;

            if (_enableLimit && _limitState != LimitState.Inactive)
            {
                // Solve prismatic and limit constraint in block form.
                var cdot2 = Vector2Util.Dot(_tmp.Axis, vB - vA) + _tmp.A2 * wB - _tmp.A1 * wA;
                var cdot = new Vector3(cdot1.X, cdot1.Y, cdot2);

                var f1 = _impulse;
                var df = _tmp.K.Solve33(-cdot);
                _impulse += df;

                if (_limitState == LimitState.AtLower)
                {
                    _impulse.Z = System.Math.Max(_impulse.Z, 0.0f);
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    _impulse.Z = System.Math.Min(_impulse.Z, 0.0f);
                }

                // f2(1:2) = invK(1:2,1:2) * (-Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3))) + f1(1:2)
                var b = -cdot1 - (_impulse.Z - f1.Z) * new Vector2(_tmp.K.Column3.X, _tmp.K.Column3.Y);
                var fToR = _tmp.K.Solve22(b) + new Vector2(f1.X, f1.Y);
                _impulse.X = fToR.X;
                _impulse.Y = fToR.Y;

                df = _impulse - f1;

                var p = df.X * _tmp.Perp + df.Z * _tmp.Axis;
                var lA = df.X * _tmp.S1 + df.Y + df.Z * _tmp.A1;
                var lB = df.X * _tmp.S2 + df.Y + df.Z * _tmp.A2;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }
            else
            {
                // Limit is inactive, just solve the prismatic constraint in block form.
                var df = _tmp.K.Solve22(-cdot1);
                _impulse.X += df.X;
                _impulse.Y += df.Y;

                var p = df.X * _tmp.Perp;
                var lA = df.X * _tmp.S1 + df.Y;
                var lB = df.X * _tmp.S2 + df.Y;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
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

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            // Compute fresh Jacobians
            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var d = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            var axis = qA * _localXAxisA;
            var a1 = Vector2Util.Cross(d + rA, axis);
            var a2 = Vector2Util.Cross(rB, axis);
            var perp = qA * _localYAxisA;

            var s1 = Vector2Util.Cross(d + rA, perp);
            var s2 = Vector2Util.Cross(rB, perp);

            Vector3 impulse;
            Vector2 c1;
            c1.X = Vector2Util.Dot(ref perp, ref d);
            c1.Y = aB - aA - _referenceAngle;

            var linearError = System.Math.Abs(c1.X);
            var angularError = System.Math.Abs(c1.Y);

            var active = false;
            var c2 = 0.0f;
            if (_enableLimit)
            {
                var translation = Vector2Util.Dot(ref axis, ref d);
                if (System.Math.Abs(_upperTranslation - _lowerTranslation) < 2.0f * Settings.LinearSlop)
                {
                    // Prevent large angular corrections
                    c2 = MathHelper.Clamp(translation, -Settings.MaxLinearCorrection, Settings.MaxLinearCorrection);
                    linearError = System.Math.Max(linearError, System.Math.Abs(translation));
                    active = true;
                }
                else if (translation <= _lowerTranslation)
                {
                    // Prevent large linear corrections and allow some slop.
                    c2 = MathHelper.Clamp(
                        translation - _lowerTranslation + Settings.LinearSlop,
                        -Settings.MaxLinearCorrection,
                        0.0f);
                    linearError = System.Math.Max(linearError, _lowerTranslation - translation);
                    active = true;
                }
                else if (translation >= _upperTranslation)
                {
                    // Prevent large linear corrections and allow some slop.
                    c2 = MathHelper.Clamp(
                        translation - _upperTranslation - Settings.LinearSlop,
                        0.0f,
                        Settings.MaxLinearCorrection);
                    linearError = System.Math.Max(linearError, translation - _upperTranslation);
                    active = true;
                }
            }

            if (active)
            {
                var k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
                var k12 = iA * s1 + iB * s2;
                var k13 = iA * s1 * a1 + iB * s2 * a2;
                var k22 = iA + iB;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (k22 == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    // For fixed rotation
                    k22 = 1.0f;
                }
                var k23 = iA * a1 + iB * a2;
                var k33 = mA + mB + iA * a1 * a1 + iB * a2 * a2;

                Matrix33 k;
                Vector3Util.Set(out k.Column1, k11, k12, k13);
                Vector3Util.Set(out k.Column2, k12, k22, k23);
                Vector3Util.Set(out k.Column3, k13, k23, k33);

                Vector3 c;
                c.X = c1.X;
                c.Y = c1.Y;
                c.Z = c2;

                impulse = k.Solve33(-c);
            }
            else
            {
                var k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
                var k12 = iA * s1 + iB * s2;
                var k22 = iA + iB;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (k22 == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    k22 = 1.0f;
                }

                Matrix22 k;
                Vector2Util.Set(out k.Column1, k11, k12);
                Vector2Util.Set(out k.Column2, k12, k22);

                var impulse1 = k.Solve(-c1);
                impulse.X = impulse1.X;
                impulse.Y = impulse1.Y;
                impulse.Z = 0.0f;
            }

            var p = impulse.X * perp + impulse.Z * axis;
            var lA = impulse.X * s1 + impulse.Y + impulse.Z * a1;
            var lB = impulse.X * s2 + impulse.Y + impulse.Z * a2;

            cA -= mA * p;
            aA -= iA * lA;
            cB += mB * p;
            aB += iB * lB;

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return linearError <= (Settings.LinearSlop + Settings.Epsilon) && angularError <= (Settings.AngularSlop + Settings.Epsilon);
        }

        #endregion
    }
}