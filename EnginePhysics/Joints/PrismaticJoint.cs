using System;
using Engine.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Joints
{
    public sealed class PrismaticJoint : Joint
    {
        #region Properties

        /// <summary>
        /// Get the anchor point on the first body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorA
        {
            get { return BodyA.GetWorldPoint(_localAnchorA); }
        }

        /// <summary>
        /// Get the anchor point on the second body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorB
        {
            get { return BodyB.GetWorldPoint(_localAnchorB); }
        }

        /// <summary>
        /// Get the anchor point on the first body in local coordinates.
        /// </summary>
        public LocalPoint LocalAnchorA
        {
            get { return _localAnchorA; }
        }

        /// <summary>
        /// Get the anchor point on the second body in local coordinates.
        /// </summary>
        public LocalPoint LocalAnchorB
        {
            get { return _localAnchorB; }
        }

        /// <summary>
        /// Set/Get whether the joint limit is enabled.
        /// </summary>
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

        /// <summary>
        /// Set/Get the lower joint limit, usually in meters.
        /// </summary>
        public float LowerLimit
        {
            get { return _lowerTranslation; }
            set { SetLimits(value, _upperTranslation); }
        }

        /// <summary>
        /// Set/Get the upper joint limit, usually in meters.
        /// </summary>
        public float UpperLimit
        {
            get { return _upperTranslation; }
            set { SetLimits(_lowerTranslation, value); }
        }

        /// <summary>
        /// Set/Get whether the joint motor is enabled.
        /// </summary>
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

        /// <summary>
        /// Set/Get the motor speed, usually in meters per second.
        /// </summary>
        public float MotorSpeed
        {
            get { return _motorSpeed; }
            set
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (value != _motorSpeed)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _motorSpeed = value;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        /// <summary>
        /// Set/Get the maximum motor force, usually in N.
        /// </summary>
        public float MaxMotorForce
        {
            get { return _maxMotorForce; }
            set
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (value != _maxMotorForce)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _maxMotorForce = value;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        #endregion

        #region Fields

        #region Solver shared

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

        #endregion

        #region Solver temp

        private int _indexA;

        private int _indexB;

        private LocalPoint _localCenterA;

        private LocalPoint _localCenterB;

        private float _inverseMassA;

        private float _inverseMassB;

        private float _inverseInertiaA;

        private float _inverseInertiaB;

        private Vector2 _axis, _perp;

        private float _s1, _s2;

        private float _a1, _a2;

        private Matrix33 _k;

        private float _motorMass;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="PrismaticJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public PrismaticJoint() : base(JointType.Prismatic)
        {
        }

        /// <summary>
        /// Initializes this joint with the specified parameters.
        /// </summary>
        internal void Initialize()
        {
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Set the joint limits, usually in meters.
        /// </summary>
        public void SetLimits(float lower, float upper)
        {
            if (lower > upper)
            {
                throw new ArgumentException("lower must be less than or equal to upper", "lower");
            }

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (lower != _lowerTranslation ||
                upper != _upperTranslation)
            {
                BodyA.IsAwake = true;
                BodyB.IsAwake = true;
                _lowerTranslation = lower;
                _upperTranslation = upper;
                _impulse.Z = 0.0f;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator
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

        /// <summary>
        /// Initializes the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void InitializeVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            _indexA = BodyA.IslandIndex;
            _indexB = BodyB.IslandIndex;
            _localCenterA = BodyA.Sweep.LocalCenter;
            _localCenterB = BodyB.Sweep.LocalCenter;
            _inverseMassA = BodyA.InverseMass;
            _inverseMassB = BodyB.InverseMass;
            _inverseInertiaA = BodyA.InverseInertia;
            _inverseInertiaB = BodyB.InverseInertia;

            var cA = positions[_indexA].Point;
            var aA = positions[_indexA].Angle;
            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;

            var cB = positions[_indexB].Point;
            var aB = positions[_indexB].Angle;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            // Compute the effective masses.
            var rA = qA * (_localAnchorA - _localCenterA);
            var rB = qB * (_localAnchorB - _localCenterB);
            var d = (Vector2)(cB - cA) + (rB - rA);

            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseInertiaB;

            // Compute motor Jacobian and effective mass.
            {
                _axis = qA * _localXAxisA;
                _a1 = Vector2Util.Cross(d + rA, _axis);
                _a2 = Vector2Util.Cross(rB, _axis);

                _motorMass = mA + mB + iA * _a1 * _a1 + iB * _a2 * _a2;
                if (_motorMass > 0.0f)
                {
                    _motorMass = 1.0f / _motorMass;
                }
            }

            // Prismatic constraint.
            {
                _perp = qA * _localYAxisA;

                _s1 = Vector2Util.Cross(d + rA, _perp);
                _s2 = Vector2Util.Cross(rB, _perp);

                var k11 = mA + mB + iA * _s1 * _s1 + iB * _s2 * _s2;
                var k12 = iA * _s1 + iB * _s2;
                var k13 = iA * _s1 * _a1 + iB * _s2 * _a2;
                var k22 = iA + iB;
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (k22 == 0.0f)
                {
                    // For bodies with fixed rotation.
                    k22 = 1.0f;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
                var k23 = iA * _a1 + iB * _a2;
                var k33 = mA + mB + iA * _a1 * _a1 + iB * _a2 * _a2;

                Vector3Util.Set(out _k.Column1, k11, k12, k13);
                Vector3Util.Set(out _k.Column2, k12, k22, k23);
                Vector3Util.Set(out _k.Column3, k13, k23, k33);
            }

            // Compute motor and limit terms.
            if (_enableLimit)
            {
                var jointTranslation = Vector2.Dot(_axis, d);
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

            if (step.IsWarmStarting)
            {
                var p = _impulse.X * _perp + (_motorImpulse + _impulse.Z) * _axis;
                var lA = _impulse.X * _s1 + _impulse.Y + (_motorImpulse + _impulse.Z) * _a1;
                var lB = _impulse.X * _s2 + _impulse.Y + (_motorImpulse + _impulse.Z) * _a2;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }
            else
            {
                _impulse = Vector3.Zero;
                _motorImpulse = 0.0f;
            }

            velocities[_indexA].LinearVelocity = vA;
            velocities[_indexA].AngularVelocity = wA;
            velocities[_indexB].LinearVelocity = vB;
            velocities[_indexB].AngularVelocity = wB;
        }

        /// <summary>
        /// Solves the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void SolveVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseInertiaB;

            // Solve linear motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal)
            {
                var cdot = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
                var impulse = _motorMass * (_motorSpeed - cdot);
                var oldImpulse = _motorImpulse;
                var maxImpulse = step.DeltaT * _maxMotorForce;
                _motorImpulse = MathHelper.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                var p = impulse * _axis;
                var lA = impulse * _a1;
                var lB = impulse * _a2;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }

            Vector2 cdot1;
            cdot1.X = Vector2.Dot(_perp, vB - vA) + _s2 * wB - _s1 * wA;
            cdot1.Y = wB - wA;

            if (_enableLimit && _limitState != LimitState.Inactive)
            {
                // Solve prismatic and limit constraint in block form.
                var cdot2 = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
                var cdot = new Vector3(cdot1.X, cdot1.Y, cdot2);

                var f1 = _impulse;
                var df = _k.Solve33(-cdot);
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
                var b = -cdot1 - (_impulse.Z - f1.Z) * new Vector2(_k.Column3.X, _k.Column3.Y);
                var fToR = _k.Solve22(b) + new Vector2(f1.X, f1.Y);
                _impulse.X = fToR.X;
                _impulse.Y = fToR.Y;

                df = _impulse - f1;

                var p = df.X * _perp + df.Z * _axis;
                var lA = df.X * _s1 + df.Y + df.Z * _a1;
                var lB = df.X * _s2 + df.Y + df.Z * _a2;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }
            else
            {
                // Limit is inactive, just solve the prismatic constraint in block form.
                var df = _k.Solve22(-cdot1);
                _impulse.X += df.X;
                _impulse.Y += df.Y;

                var p = df.X * _perp;
                var lA = df.X * _s1 + df.Y;
                var lB = df.X * _s2 + df.Y;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }

            velocities[_indexA].LinearVelocity = vA;
            velocities[_indexA].AngularVelocity = wA;
            velocities[_indexB].LinearVelocity = vB;
            velocities[_indexB].AngularVelocity = wB;
        }

        /// <summary>
        /// This returns true if the position errors are within tolerance, allowing an
        /// early exit from the iteration loop.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        /// <returns><c>true</c> if the position errors are within tolerance.</returns>
        internal override bool SolvePositionConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            var cA = positions[_indexA].Point;
            var aA = positions[_indexA].Angle;
            var cB = positions[_indexB].Point;
            var aB = positions[_indexB].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseInertiaB;

            // Compute fresh Jacobians
            var rA = qA * (_localAnchorA - _localCenterA);
            var rB = qB * (_localAnchorB - _localCenterB);
            var d = (Vector2)(cB - cA) + (rB - rA);

            var axis = qA * _localXAxisA;
            var a1 = Vector2Util.Cross(d + rA, axis);
            var a2 = Vector2Util.Cross(rB, axis);
            var perp = qA * _localYAxisA;

            var s1 = Vector2Util.Cross(d + rA, perp);
            var s2 = Vector2Util.Cross(rB, perp);

            Vector3 impulse;
            Vector2 c1;
            c1.X = Vector2.Dot(perp, d);
            c1.Y = aB - aA - _referenceAngle;

            var linearError = System.Math.Abs(c1.X);
            var angularError = System.Math.Abs(c1.Y);

            var active = false;
            var c2 = 0.0f;
            if (_enableLimit)
            {
                var translation = Vector2.Dot(axis, d);
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
                    c2 = MathHelper.Clamp(translation - _lowerTranslation + Settings.LinearSlop,
                                          -Settings.MaxLinearCorrection, 0.0f);
                    linearError = System.Math.Max(linearError, _lowerTranslation - translation);
                    active = true;
                }
                else if (translation >= _upperTranslation)
                {
                    // Prevent large linear corrections and allow some slop.
                    c2 = MathHelper.Clamp(translation - _upperTranslation - Settings.LinearSlop, 0.0f,
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
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (k22 == 0.0f)
                {
                    // For fixed rotation
                    k22 = 1.0f;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
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
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (k22 == 0.0f)
                {
                    k22 = 1.0f;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator

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

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;

            return linearError <= Settings.LinearSlop && angularError <= Settings.AngularSlop;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Serialization.Packet Packetize(Serialization.Packet packet)
        {
            return base.Packetize(packet);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Serialization.Hasher hasher)
        {
            base.Hash(hasher);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        public override void CopyInto(Joint into)
        {
            base.CopyInto(into);
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
            return base.ToString();
        }

        #endregion
    }
}
