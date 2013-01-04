using Engine.Physics.Math;
using Engine.Serialization;
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
    /// <summary>
    /// 
    /// </summary>
    public sealed class WheelJoint : Joint
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
        /// The local joint axis relative to bodyA.
        /// </summary>
        public Vector2 LocalAxisA
        {
            get { return _localXAxisA; }
        }

        /// <summary>
        /// Get the current joint translation, usually in meters.
        /// </summary>
        public float JointTranslation
        {
            get
            {
                var bA = BodyA;
                var bB = BodyB;

                var pA = bA.GetWorldPoint(_localAnchorA);
                var pB = bB.GetWorldPoint(_localAnchorB);
                var d = (Vector2)(pB - pA);
                var axis = bA.GetWorldVector(_localXAxisA);

                float translation = Vector2.Dot(d, axis);
                return translation;
            }
        }

        /// <summary>
        /// Get the current joint translation speed, usually in meters per second.
        /// </summary>
        public float JointSpeed
        {
            get { return BodyB.AngularVelocityInternal - BodyA.AngularVelocityInternal; }
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
        /// Set/Get the motor speed, usually in radians per second.
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
        /// Set/Get the maximum motor force, usually in N-m.
        /// </summary>
        public float MaxMotorTorque
        {
            get { return _maxMotorTorque; }
            set
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (value != _maxMotorTorque)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _maxMotorTorque = value;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        /// <summary>
        /// Set/Get the spring frequency in hertz. Setting the frequency to zero disables the spring.
        /// </summary>
        public float SpringFrequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Set/Get the spring damping ratio.
        /// </summary>
        public float SpringDampingRatio
        {
            get { return _dampingRatio; }
            set { _dampingRatio = value; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private float _frequency;

        private float _dampingRatio;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private Vector2 _localXAxisA;

        private Vector2 _localYAxisA;

        private float _impulse;

        private float _motorImpulse;

        private float _springImpulse;

        private float _maxMotorTorque;

        private float _motorSpeed;

        private bool _enableMotor;

        #endregion

        #region Solver temp

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

            public Vector2 _ax, _ay;

            public float _sAx, _sBx;

            public float _sAy, _sBy;

            public float Mass;

            public float MotorMass;

            public float SpringMass;

            public float Bias;

            public float Gamma;
        }

        [PacketizerIgnore]
        private SolverTemp _tmp;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="WheelJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public WheelJoint() : base(JointType.Wheel)
        {
        }

        /// <summary>
        /// Initializes this joint with the specified parameters.
        /// </summary>
        internal void Initialize()
        {
        }

        #endregion

        #region Logic

        // Linear constraint (point-to-line)
        // d = pB - pA = xB + rB - xA - rA
        // C = dot(ay, d)
        // Cdot = dot(d, cross(wA, ay)) + dot(ay, vB + cross(wB, rB) - vA - cross(wA, rA))
        //      = -dot(ay, vA) - dot(cross(d + rA, ay), wA) + dot(ay, vB) + dot(cross(rB, ay), vB)
        // J = [-ay, -cross(d + rA, ay), ay, cross(rB, ay)]

        // Spring linear constraint
        // C = dot(ax, d)
        // Cdot = = -dot(ax, vA) - dot(cross(d + rA, ax), wA) + dot(ax, vB) + dot(cross(rB, ax), vB)
        // J = [-ax -cross(d+rA, ax) ax cross(rB, ax)]

        // Motor rotational constraint
        // Cdot = wB - wA
        // J = [0 0 -1 0 0 1]

        /// <summary>
        /// Initializes the velocity constraints.
        /// </summary>
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

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

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
            var d = (Vector2)(cB - cA) + (rB - rA);

            // Point to line constraint
            {
                _tmp._ay = qA * _localYAxisA;
                _tmp._sAy = Vector2Util.Cross(d + rA, _tmp._ay);
                _tmp._sBy = Vector2Util.Cross(rB, _tmp._ay);

                _tmp.Mass = mA + mB + iA * _tmp._sAy * _tmp._sAy + iB * _tmp._sBy * _tmp._sBy;

                if (_tmp.Mass > 0.0f)
                {
                    _tmp.Mass = 1.0f / _tmp.Mass;
                }
            }

            // Spring constraint
            _tmp.SpringMass = 0.0f;
            _tmp.Bias = 0.0f;
            _tmp.Gamma = 0.0f;
            if (_frequency > 0.0f)
            {
                _tmp._ax = qA * _localXAxisA;
                _tmp._sAx = Vector2Util.Cross(d + rA, _tmp._ax);
                _tmp._sBx = Vector2Util.Cross(rB, _tmp._ax);

                var invMass = mA + mB + iA * _tmp._sAx * _tmp._sAx + iB * _tmp._sBx * _tmp._sBx;

                if (invMass > 0.0f)
                {
                    _tmp.SpringMass = 1.0f / invMass;

                    var c = Vector2.Dot(d, _tmp._ax);

                    // Frequency
                    var omega = 2.0f * MathHelper.Pi * _frequency;

                    // Damping coefficient
                    var dc = 2.0f * _tmp.SpringMass * _dampingRatio * omega;

                    // Spring stiffness
                    var k = _tmp.SpringMass * omega * omega;

                    // magic formulas
                    var h = step.DeltaT;
                    _tmp.Gamma = h * (dc + h * k);
                    if (_tmp.Gamma > 0.0f)
                    {
                        _tmp.Gamma = 1.0f / _tmp.Gamma;
                    }

                    _tmp.Bias = c * h * k * _tmp.Gamma;

                    _tmp.SpringMass = invMass + _tmp.Gamma;
                    if (_tmp.SpringMass > 0.0f)
                    {
                        _tmp.SpringMass = 1.0f / _tmp.SpringMass;
                    }
                }
            }
            else
            {
                _springImpulse = 0.0f;
            }

            // Rotational motor
            if (_enableMotor)
            {
                _tmp.MotorMass = iA + iB;
                if (_tmp.MotorMass > 0.0f)
                {
                    _tmp.MotorMass = 1.0f / _tmp.MotorMass;
                }
            }
            else
            {
                _tmp.MotorMass = 0.0f;
                _motorImpulse = 0.0f;
            }

            if (step.IsWarmStarting)
            {
                var p = _impulse * _tmp._ay + _springImpulse * _tmp._ax;
                var lA = _impulse * _tmp._sAy + _springImpulse * _tmp._sAx + _motorImpulse;
                var lB = _impulse * _tmp._sBy + _springImpulse * _tmp._sBx + _motorImpulse;

                vA -= _tmp.InverseMassA * p;
                wA -= _tmp.InverseInertiaA * lA;

                vB += _tmp.InverseMassB * p;
                wB += _tmp.InverseInertiaB * lB;
            }
            else
            {
                _impulse = 0.0f;
                _springImpulse = 0.0f;
                _motorImpulse = 0.0f;
            }

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
        }

        /// <summary>
        /// Solves the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void SolveVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            // Solve spring constraint
            {
                var cdot = Vector2.Dot(_tmp._ax, vB - vA) + _tmp._sBx * wB - _tmp._sAx * wA;
                var impulse = -_tmp.SpringMass * (cdot + _tmp.Bias + _tmp.Gamma * _springImpulse);
                _springImpulse += impulse;

                var p = impulse * _tmp._ax;
                var lA = impulse * _tmp._sAx;
                var lB = impulse * _tmp._sBx;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }

            // Solve rotational motor constraint
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

            // Solve point to line constraint
            {
                var cdot = Vector2.Dot(_tmp._ay, vB - vA) + _tmp._sBy * wB - _tmp._sAy * wA;
                var impulse = -_tmp.Mass * cdot;
                _impulse += impulse;

                var p = impulse * _tmp._ay;
                var lA = impulse * _tmp._sAy;
                var lB = impulse * _tmp._sBy;

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
            var cA = positions[_tmp.IndexA].Point;
            var aA = positions[_tmp.IndexA].Angle;
            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
            var d = (Vector2)(cB - cA) + (rB - rA);

            var ay = qA * _localYAxisA;

            var sAy = Vector2Util.Cross(d + rA, ay);
            var sBy = Vector2Util.Cross(rB, ay);

            var c = Vector2.Dot(d, ay);

            var k = _tmp.InverseMassA + _tmp.InverseMassB + _tmp.InverseInertiaA * _tmp._sAy * _tmp._sAy + _tmp.InverseInertiaB * _tmp._sBy * _tmp._sBy;

            float impulse;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (k != 0.0f)
            {
                impulse = - c / k;
            }
            else
            {
                impulse = 0.0f;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            var p = impulse * ay;
            var lA = impulse * sAy;
            var lB = impulse * sBy;

            cA -= _tmp.InverseMassA * p;
            aA -= _tmp.InverseInertiaA * lA;
            cB += _tmp.InverseMassB * p;
            aB += _tmp.InverseInertiaB * lB;

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return System.Math.Abs(c) <= Settings.LinearSlop;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
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
