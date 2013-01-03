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
    public sealed class RevoluteJoint : Joint
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

        /// Get the reference angle.
        public float ReferenceAngle
        {
            get { return _referenceAngle; }
        }

        public float JointAngle
        {
            get
            {
                var bA = BodyA;
                var bB = BodyB;
                return bB.Sweep.Angle - bA.Sweep.Angle - _referenceAngle;
            }
        }

        public float JointSpeed
        {
            get
            {
                var bA = BodyA;
                var bB = BodyB;
                return bB.AngularVelocityInternal - bA.AngularVelocityInternal;
            }
        }

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

        public float LowerLimit
        {
            get { return _lowerAngle; }
            set { SetLimits(value, _upperAngle); }
        }

        public float UpperLimit
        {
            get { return _upperAngle; }
            set { SetLimits(_lowerAngle, value); }
        }

        #endregion

        #region Fields

        #region Solver shared

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

        #endregion

        #region Solver temp

        private int _indexA;

        private int _indexB;

        private Vector2 _rA;

        private Vector2 _rB;

        private LocalPoint _localCenterA;

        private LocalPoint _localCenterB;

        private float _inverseMassA;

        private float _inverseMassB;

        private float _inverseInertiaA;

        private float _inverseIneratiaB;

        private Matrix33 _mass; // effective mass for point-to-point constraint.

        private float _motorMass; // effective mass for motor/limit angular constraint.

        private LimitState _limitState;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="RevoluteJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public RevoluteJoint() : base(JointType.Revolute)
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
            if (lower != _lowerAngle || upper != _upperAngle)
            {
                BodyA.IsAwake = true;
                BodyB.IsAwake = true;
                _impulse.Z = 0.0f;
                _lowerAngle = lower;
                _upperAngle = upper;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator
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
            _inverseIneratiaB = BodyB.InverseInertia;

            var aA = positions[_indexA].Angle;
            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;

            var aB = positions[_indexB].Angle;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            _rA = qA * (_localAnchorA - _localCenterA);
            _rB = qB * (_localAnchorB - _localCenterB);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

            var mA = _inverseMassA;
            var mB = _inverseMassB;
            var iA = _inverseInertiaA;
            var iB = _inverseIneratiaB;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            var fixedRotation = (iA + iB == 0.0f);
            // ReSharper restore CompareOfFloatsByEqualityOperator

            _mass.Column1.X = mA + mB + _rA.Y * _rA.Y * iA + _rB.Y * _rB.Y * iB;
            _mass.Column2.X = -_rA.Y * _rA.X * iA - _rB.Y * _rB.X * iB;
            _mass.Column3.X = -_rA.Y * iA - _rB.Y * iB;
            _mass.Column1.Y = _mass.Column2.X;
            _mass.Column2.Y = mA + mB + _rA.X * _rA.X * iA + _rB.X * _rB.X * iB;
            _mass.Column3.Y = _rA.X * iA + _rB.X * iB;
            _mass.Column1.Z = _mass.Column3.X;
            _mass.Column2.Z = _mass.Column3.Y;
            _mass.Column3.Z = iA + iB;

            _motorMass = iA + iB;
            if (_motorMass > 0.0f)
            {
                _motorMass = 1.0f / _motorMass;
            }

            if (_enableMotor == false || fixedRotation)
            {
                _motorImpulse = 0.0f;
            }

            if (_enableLimit && fixedRotation == false)
            {
                float jointAngle = aB - aA - _referenceAngle;
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

            if (step.IsWarmStarting)
            {
                var p = new Vector2(_impulse.X, _impulse.Y);

                vA -= mA * p;
                wA -= iA * (Vector2Util.Cross(_rA, p) + _motorImpulse + _impulse.Z);

                vB += mB * p;
                wB += iB * (Vector2Util.Cross(_rB, p) + _motorImpulse + _impulse.Z);
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
            var iB = _inverseIneratiaB;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            var fixedRotation = (iA + iB == 0.0f);
            // ReSharper restore CompareOfFloatsByEqualityOperator

            // Solve motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal && fixedRotation == false)
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

            // Solve limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive && fixedRotation == false)
            {
                var cdot1 = vB + Vector2Util.Cross(wB, _rB) - vA - Vector2Util.Cross(wA, _rA);
                var cdot2 = wB - wA;
                var cdot = new Vector3(cdot1.X, cdot1.Y, cdot2);

                var impulse = -_mass.Solve33(cdot);

                if (_limitState == LimitState.Equal)
                {
                    _impulse += impulse;
                }
                else if (_limitState == LimitState.AtLower)
                {
                    var newImpulse = _impulse.Z + impulse.Z;
                    if (newImpulse < 0.0f)
                    {
                        var rhs = -cdot1 + _impulse.Z * new Vector2(_mass.Column3.X, _mass.Column3.Y);
                        var reduced = _mass.Solve22(rhs);
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
                        var rhs = -cdot1 + _impulse.Z * new Vector2(_mass.Column3.X, _mass.Column3.Y);
                        var reduced = _mass.Solve22(rhs);
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
                wA -= iA * (Vector2Util.Cross(_rA, p) + impulse.Z);

                vB += mB * p;
                wB += iB * (Vector2Util.Cross(_rB, p) + impulse.Z);
            }
            else
            {
                // Solve point-to-point constraint
                var cdot = vB + Vector2Util.Cross(wB, _rB) - vA - Vector2Util.Cross(wA, _rA);
                var impulse = _mass.Solve22(-cdot);

                _impulse.X += impulse.X;
                _impulse.Y += impulse.Y;

                vA -= mA * impulse;
                wA -= iA * Vector2Util.Cross(_rA, impulse);

                vB += mB * impulse;
                wB += iB * Vector2Util.Cross(_rB, impulse);
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

            var angularError = 0.0f;
            float positionError;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            var fixedRotation = (_inverseInertiaA + _inverseIneratiaB == 0.0f);
            // ReSharper restore CompareOfFloatsByEqualityOperator

            // Solve angular limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive && fixedRotation == false)
            {
                var angle = aB - aA - _referenceAngle;
                var limitImpulse = 0.0f;

                if (_limitState == LimitState.Equal)
                {
                    // Prevent large angular corrections
                    var c = MathHelper.Clamp(angle - _lowerAngle, -Settings.MaxAngularCorrection,
                                             Settings.MaxAngularCorrection);
                    limitImpulse = -_motorMass * c;
                    angularError = System.Math.Abs(c);
                }
                else if (_limitState == LimitState.AtLower)
                {
                    var c = angle - _lowerAngle;
                    angularError = -c;

                    // Prevent large angular corrections and allow some slop.
                    c = MathHelper.Clamp(c + Settings.AngularSlop, -Settings.MaxAngularCorrection, 0.0f);
                    limitImpulse = -_motorMass * c;
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    var c = angle - _upperAngle;
                    angularError = c;

                    // Prevent large angular corrections and allow some slop.
                    c = MathHelper.Clamp(c - Settings.AngularSlop, 0.0f, Settings.MaxAngularCorrection);
                    limitImpulse = -_motorMass * c;
                }

                aA -= _inverseInertiaA * limitImpulse;
                aB += _inverseIneratiaB * limitImpulse;
            }

            // Solve point-to-point constraint.
            {
                qA.Set(aA);
                qB.Set(aB);
                var rA = qA * (_localAnchorA - _localCenterA);
                var rB = qB * (_localAnchorB - _localCenterB);

                var c = (Vector2)(cB - cA) + (rB - rA);
                positionError = c.Length();

                var mA = _inverseMassA;
                var mB = _inverseMassB;
                var iA = _inverseInertiaA;
                var iB = _inverseIneratiaB;

                Matrix22 k;
                k.Column1.X = mA + mB + iA * rA.Y * rA.Y + iB * rB.Y * rB.Y;
                k.Column1.Y = -iA * rA.X * rA.Y - iB * rB.X * rB.Y;
                k.Column2.X = k.Column1.Y;
                k.Column2.Y = mA + mB + iA * rA.X * rA.X + iB * rB.X * rB.X;

                var impulse = -k.Solve(c);

                cA -= mA * impulse;
                aA -= iA * Vector2Util.Cross(rA, impulse);

                cB += mB * impulse;
                aB += iB * Vector2Util.Cross(rB, impulse);
            }

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;

            return positionError <= Settings.LinearSlop && angularError <= Settings.AngularSlop;
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
