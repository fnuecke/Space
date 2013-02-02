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
    ///     This joint provides two degrees of freedom: translation along an axis fixed in the first body and rotation in
    ///     the plane. You can use a joint limit to restrict the range of motion and a joint motor to drive the rotation or to
    ///     model rotational friction. This joint is designed for vehicle suspensions.
    /// </summary>
    public sealed class WheelJoint : Joint
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

        /// <summary>The local joint axis relative to bodyA.</summary>
        public Vector2 LocalAxisA
        {
            get { return _localXAxisA; }
        }

        /// <summary>Get the current joint translation, usually in meters.</summary>
        public float JointTranslation
        {
            get
            {
                var bA = BodyA;
                var bB = BodyB;

                var pA = bA.GetWorldPoint(_localAnchorA);
                var pB = bB.GetWorldPoint(_localAnchorB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var d = (Vector2) (pB - pA);
// ReSharper restore RedundantCast
                var axis = bA.GetWorldVector(_localXAxisA);

                return Vector2Util.Dot(ref d, ref axis);
            }
        }

        /// <summary>Get the current joint translation speed, usually in meters per second.</summary>
        public float JointSpeed
        {
            get { return BodyB.AngularVelocityInternal - BodyA.AngularVelocityInternal; }
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

        /// <summary>Set/Get the motor speed, usually in radians per second.</summary>
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

        /// <summary>Set/Get the maximum motor force, usually in N-m.</summary>
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

        /// <summary>Set/Get the spring frequency in hertz. Setting the frequency to zero disables the spring.</summary>
        public float SpringFrequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>Set/Get the spring damping ratio.</summary>
        public float SpringDampingRatio
        {
            get { return _dampingRatio; }
            set { _dampingRatio = value; }
        }

        #endregion

        #region Fields

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

            public Vector2 Ax, Ay;

            public float SAx, SBx;

            public float SAy, SBy;

            public float Mass;

            public float MotorMass;

            public float SpringMass;

            public float Bias;

            public float Gamma;
        }

        [CopyIgnore, PacketizeIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="WheelJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public WheelJoint() : base(JointType.Wheel) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(
            WorldPoint anchor,
            Vector2 axis,
            float frequency,
            float dampingRatio,
            float maxMotorTorque,
            float motorSpeed,
            bool enableMotor)
        {
            _localAnchorA = BodyA.GetLocalPoint(anchor);
            _localAnchorB = BodyB.GetLocalPoint(anchor);
            _localXAxisA = BodyA.GetLocalVector(axis);
            _localYAxisA = Vector2Util.Cross(1.0f, ref _localXAxisA);

            _frequency = frequency;
            _dampingRatio = dampingRatio;

            _maxMotorTorque = maxMotorTorque;
            _motorSpeed = motorSpeed;
            _enableMotor = enableMotor;

            _impulse = 0;
            _motorImpulse = 0;
            _springImpulse = 0;
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
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var d = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            // Point to line constraint
            {
                _tmp.Ay = qA * _localYAxisA;
                _tmp.SAy = Vector2Util.Cross(d + rA, _tmp.Ay);
                _tmp.SBy = Vector2Util.Cross(rB, _tmp.Ay);

                _tmp.Mass = mA + mB + iA * _tmp.SAy * _tmp.SAy + iB * _tmp.SBy * _tmp.SBy;

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
                _tmp.Ax = qA * _localXAxisA;
                _tmp.SAx = Vector2Util.Cross(d + rA, _tmp.Ax);
                _tmp.SBx = Vector2Util.Cross(rB, _tmp.Ax);

                var invMass = mA + mB + iA * _tmp.SAx * _tmp.SAx + iB * _tmp.SBx * _tmp.SBx;

                if (invMass > 0.0f)
                {
                    _tmp.SpringMass = 1.0f / invMass;

                    var c = Vector2Util.Dot(ref d, ref _tmp.Ax);

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

            var p = _impulse * _tmp.Ay + _springImpulse * _tmp.Ax;
            var lA = _impulse * _tmp.SAy + _springImpulse * _tmp.SAx + _motorImpulse;
            var lB = _impulse * _tmp.SBy + _springImpulse * _tmp.SBx + _motorImpulse;

            vA -= _tmp.InverseMassA * p;
            wA -= _tmp.InverseInertiaA * lA;

            vB += _tmp.InverseMassB * p;
            wB += _tmp.InverseInertiaB * lB;

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
                var cdot = Vector2Util.Dot(_tmp.Ax, vB - vA) + _tmp.SBx * wB - _tmp.SAx * wA;
                var impulse = -_tmp.SpringMass * (cdot + _tmp.Bias + _tmp.Gamma * _springImpulse);
                _springImpulse += impulse;

                var p = impulse * _tmp.Ax;
                var lA = impulse * _tmp.SAx;
                var lB = impulse * _tmp.SBx;

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
                var cdot = Vector2Util.Dot(_tmp.Ay, vB - vA) + _tmp.SBy * wB - _tmp.SAy * wA;
                var impulse = -_tmp.Mass * cdot;
                _impulse += impulse;

                var p = impulse * _tmp.Ay;
                var lA = impulse * _tmp.SAy;
                var lB = impulse * _tmp.SBy;

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

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var d = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            var ay = qA * _localYAxisA;

            var sAy = Vector2Util.Cross(d + rA, ay);
            var sBy = Vector2Util.Cross(rB, ay);

            var c = Vector2Util.Dot(ref d, ref ay);

            var k = _tmp.InverseMassA + _tmp.InverseMassB + _tmp.InverseInertiaA * _tmp.SAy * _tmp.SAy +
                    _tmp.InverseInertiaB * _tmp.SBy * _tmp.SBy;

            float impulse;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (k != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                impulse = - c / k;
            }
            else
            {
                impulse = 0.0f;
            }

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
    }
}