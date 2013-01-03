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

        private int _indexA;

        private int _indexB;

        private LocalPoint _localCenterA;

        private LocalPoint _localCenterB;

        private float _inverseMassA;

        private float _inverseMassB;

        private float _inverseInertiaA;

        private float _inverseInertiaB;

        private Vector2 _ax, _ay;

        private float _sAx, _sBx;

        private float _sAy, _sBy;

        private float _mass;

        private float _motorMass;

        private float _springMass;

        private float _bias;

        private float _gamma;

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
            _indexA = BodyA.IslandIndex;
            _indexB = BodyB.IslandIndex;
            _localCenterA = BodyA.Sweep.LocalCenter;
            _localCenterB = BodyB.Sweep.LocalCenter;
            _inverseMassA = BodyA.InverseMass;
            _inverseMassB = BodyB.InverseMass;
            _inverseInertiaA = BodyA.InverseInertia;
            _inverseInertiaB = BodyB.InverseInertia;

            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseInertiaB;

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

            // Point to line constraint
            {
                _ay = qA * _localYAxisA;
                _sAy = Vector2Util.Cross(d + rA, _ay);
                _sBy = Vector2Util.Cross(rB, _ay);

                _mass = mA + mB + iA * _sAy * _sAy + iB * _sBy * _sBy;

                if (_mass > 0.0f)
                {
                    _mass = 1.0f / _mass;
                }
            }

            // Spring constraint
            _springMass = 0.0f;
            _bias = 0.0f;
            _gamma = 0.0f;
            if (_frequency > 0.0f)
            {
                _ax = qA * _localXAxisA;
                _sAx = Vector2Util.Cross(d + rA, _ax);
                _sBx = Vector2Util.Cross(rB, _ax);

                var invMass = mA + mB + iA * _sAx * _sAx + iB * _sBx * _sBx;

                if (invMass > 0.0f)
                {
                    _springMass = 1.0f / invMass;

                    var c = Vector2.Dot(d, _ax);

                    // Frequency
                    var omega = 2.0f * MathHelper.Pi * _frequency;

                    // Damping coefficient
                    var dc = 2.0f * _springMass * _dampingRatio * omega;

                    // Spring stiffness
                    var k = _springMass * omega * omega;

                    // magic formulas
                    var h = step.DeltaT;
                    _gamma = h * (dc + h * k);
                    if (_gamma > 0.0f)
                    {
                        _gamma = 1.0f / _gamma;
                    }

                    _bias = c * h * k * _gamma;

                    _springMass = invMass + _gamma;
                    if (_springMass > 0.0f)
                    {
                        _springMass = 1.0f / _springMass;
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
                _motorMass = iA + iB;
                if (_motorMass > 0.0f)
                {
                    _motorMass = 1.0f / _motorMass;
                }
            }
            else
            {
                _motorMass = 0.0f;
                _motorImpulse = 0.0f;
            }

            if (step.IsWarmStarting)
            {
                var p = _impulse * _ay + _springImpulse * _ax;
                var lA = _impulse * _sAy + _springImpulse * _sAx + _motorImpulse;
                var lB = _impulse * _sBy + _springImpulse * _sBx + _motorImpulse;

                vA -= _inverseMassA * p;
                wA -= _inverseInertiaA * lA;

                vB += _inverseMassB * p;
                wB += _inverseInertiaB * lB;
            }
            else
            {
                _impulse = 0.0f;
                _springImpulse = 0.0f;
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
            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseInertiaB;

            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            // Solve spring constraint
            {
                var cdot = Vector2.Dot(_ax, vB - vA) + _sBx * wB - _sAx * wA;
                var impulse = -_springMass * (cdot + _bias + _gamma * _springImpulse);
                _springImpulse += impulse;

                var p = impulse * _ax;
                var lA = impulse * _sAx;
                var lB = impulse * _sBx;

                vA -= mA * p;
                wA -= iA * lA;

                vB += mB * p;
                wB += iB * lB;
            }

            // Solve rotational motor constraint
            {
                var cdot = wB - wA - _motorSpeed;
                var impulse = -_motorMass * cdot;

                var oldImpulse = _motorImpulse;
                var maxImpulse = step.DeltaT * _maxMotorTorque;
                _motorImpulse = MathHelper.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve point to line constraint
            {
                var cdot = Vector2.Dot(_ay, vB - vA) + _sBy * wB - _sAy * wA;
                var impulse = -_mass * cdot;
                _impulse += impulse;

                var p = impulse * _ay;
                var lA = impulse * _sAy;
                var lB = impulse * _sBy;

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

            var rA = qA * (_localAnchorA - _localCenterA);
            var rB = qB * (_localAnchorB - _localCenterB);
            var d = (Vector2)(cB - cA) + (rB - rA);

            var ay = qA * _localYAxisA;

            var sAy = Vector2Util.Cross(d + rA, ay);
            var sBy = Vector2Util.Cross(rB, ay);

            var c = Vector2.Dot(d, ay);

            var k = _inverseMassA + _inverseMassB + _inverseInertiaA * _sAy * _sAy + _inverseInertiaB * _sBy * _sBy;

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

            cA -= _inverseMassA * p;
            aA -= _inverseInertiaA * lA;
            cB += _inverseMassB * p;
            aB += _inverseInertiaB * lB;

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;

            return System.Math.Abs(c) <= Settings.LinearSlop;
        }

        #endregion
    }
}
