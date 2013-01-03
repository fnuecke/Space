using System.Globalization;
using Engine.Physics.Components;
using Engine.Physics.Math;
using Engine.XnaExtensions;
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
    public sealed class GearJoint : Joint
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
        /// Gets or sets the gear ratio.
        /// </summary>
        public float Ratio
        {
            get { return _ratio; }
            set { _ratio = value; }
        }

        /// <summary>
        /// Body A is connected to body C.
        /// </summary>
        private Body BodyC
        {
            get { return _bodyIdC != 0 ? Manager.GetComponentById(_bodyIdC) as Body : null; }
        }

        /// <summary>
        /// Body B is connected to body D.
        /// </summary>
        private Body BodyD
        {
            get { return _bodyIdD != 0 ? Manager.GetComponentById(_bodyIdD) as Body : null; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private JointType _typeA;

        private JointType _typeB;

        private int _bodyIdC;

        private int _bodyIdD;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private LocalPoint _localAnchorC;

        private LocalPoint _localAnchorD;

        private Vector2 _localAxisC;

        private Vector2 _localAxisD;

        private float _referenceAngleA;

        private float _referenceAngleB;

        private float _constant;

        private float _ratio;

        private float _impulse;

        #endregion

        #region Solver temp

        private int _indexA, _indexB, _indexC, _indexD;

        private LocalPoint _lcA, _lcB, _lcC, _lcD;

        private float _mA, _mB, _mC, _mD;

        private float _iA, _iB, _iC, _iD;

        private Vector2 _JvAC, _JvBD;

        private float _JwA, _JwB, _JwC, _JwD;

        private float _mass;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="GearJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public GearJoint() : base(JointType.Gear)
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

        // Gear Joint:
        // C0 = (coordinate1 + ratio * coordinate2)_initial
        // C = (coordinate1 + ratio * coordinate2) - C0 = 0
        // J = [J1 ratio * J2]
        // K = J * invM * JT
        //   = J1 * invM1 * J1T + ratio * ratio * J2 * invM2 * J2T
        //
        // Revolute:
        // coordinate = rotation
        // Cdot = angularVelocity
        // J = [0 0 1]
        // K = J * invM * JT = invI
        //
        // Prismatic:
        // coordinate = dot(p - pg, ug)
        // Cdot = dot(v + cross(w, r), ug)
        // J = [ug cross(r, ug)]
        // K = J * invM * JT = invMass + invI * cross(r, ug)^2

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
            _indexC = BodyC.IslandIndex;
            _indexD = BodyD.IslandIndex;
            _lcA = BodyA.Sweep.LocalCenter;
            _lcB = BodyB.Sweep.LocalCenter;
            _lcC = BodyC.Sweep.LocalCenter;
            _lcD = BodyD.Sweep.LocalCenter;
            _mA = BodyA.InverseMass;
            _mB = BodyB.InverseMass;
            _mC = BodyC.InverseMass;
            _mD = BodyD.InverseMass;
            _iA = BodyA.InverseInertia;
            _iB = BodyB.InverseInertia;
            _iC = BodyC.InverseInertia;
            _iD = BodyD.InverseInertia;

            var aA = positions[_indexA].Angle;
            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;

            var aB = positions[_indexB].Angle;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var aC = positions[_indexC].Angle;
            var vC = velocities[_indexC].LinearVelocity;
            var wC = velocities[_indexC].AngularVelocity;

            var aD = positions[_indexD].Angle;
            var vD = velocities[_indexD].LinearVelocity;
            var wD = velocities[_indexD].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);
            var qC = new Rotation(aC);
            var qD = new Rotation(aD);

            _mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                _JvAC = Vector2.Zero;
                _JwA = 1.0f;
                _JwC = 1.0f;
                _mass += _iA + _iC;
            }
            else
            {
                var u = qC * _localAxisC;
                var rC = qC * (_localAnchorC - _lcC);
                var rA = qA * (_localAnchorA - _lcA);
                _JvAC = u;
                _JwC = Vector2Util.Cross(rC, u);
                _JwA = Vector2Util.Cross(rA, u);
                _mass += _mC + _mA + _iC * _JwC * _JwC + _iA * _JwA * _JwA;
            }

            if (_typeB == JointType.Revolute)
            {
                _JvBD = Vector2.Zero;
                _JwB = _ratio;
                _JwD = _ratio;
                _mass += _ratio * _ratio * (_iB + _iD);
            }
            else
            {
                var u = qD * _localAxisD;
                var rD = qD * (_localAnchorD - _lcD);
                var rB = qB * (_localAnchorB - _lcB);
                _JvBD = _ratio * u;
                _JwD = _ratio * Vector2Util.Cross(rD, u);
                _JwB = _ratio * Vector2Util.Cross(rB, u);
                _mass += _ratio * _ratio * (_mD + _mB) + _iD * _JwD * _JwD + _iB * _JwB * _JwB;
            }

            // Compute effective mass.
            _mass = _mass > 0.0f ? 1.0f / _mass : 0.0f;

            if (step.IsWarmStarting)
            {
                vA += (_mA * _impulse) * _JvAC;
                wA += _iA * _impulse * _JwA;
                vB += (_mB * _impulse) * _JvBD;
                wB += _iB * _impulse * _JwB;
                vC -= (_mC * _impulse) * _JvAC;
                wC -= _iC * _impulse * _JwC;
                vD -= (_mD * _impulse) * _JvBD;
                wD -= _iD * _impulse * _JwD;
            }
            else
            {
                _impulse = 0.0f;
            }

            velocities[_indexA].LinearVelocity = vA;
            velocities[_indexA].AngularVelocity = wA;
            velocities[_indexB].LinearVelocity = vB;
            velocities[_indexB].AngularVelocity = wB;
            velocities[_indexC].LinearVelocity = vC;
            velocities[_indexC].AngularVelocity = wC;
            velocities[_indexD].LinearVelocity = vD;
            velocities[_indexD].AngularVelocity = wD;
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
            var vC = velocities[_indexC].LinearVelocity;
            var wC = velocities[_indexC].AngularVelocity;
            var vD = velocities[_indexD].LinearVelocity;
            var wD = velocities[_indexD].AngularVelocity;

            var cdot = Vector2.Dot(_JvAC, vA - vC) + Vector2.Dot(_JvBD, vB - vD);
            cdot += (_JwA * wA - _JwC * wC) + (_JwB * wB - _JwD * wD);

            var impulse = -_mass * cdot;
            _impulse += impulse;

            vA += (_mA * impulse) * _JvAC;
            wA += _iA * impulse * _JwA;
            vB += (_mB * impulse) * _JvBD;
            wB += _iB * impulse * _JwB;
            vC -= (_mC * impulse) * _JvAC;
            wC -= _iC * impulse * _JwC;
            vD -= (_mD * impulse) * _JvBD;
            wD -= _iD * impulse * _JwD;

            velocities[_indexA].LinearVelocity = vA;
            velocities[_indexA].AngularVelocity = wA;
            velocities[_indexB].LinearVelocity = vB;
            velocities[_indexB].AngularVelocity = wB;
            velocities[_indexC].LinearVelocity = vC;
            velocities[_indexC].AngularVelocity = wC;
            velocities[_indexD].LinearVelocity = vD;
            velocities[_indexD].AngularVelocity = wD;
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
            var cC = positions[_indexC].Point;
            var aC = positions[_indexC].Angle;
            var cD = positions[_indexD].Point;
            var aD = positions[_indexD].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);
            var qC = new Rotation(aC);
            var qD = new Rotation(aD);

            const float linearError = 0.0f;

            float coordinateA, coordinateB;

            Vector2 JvAC, JvBD;
            float JwA, JwB, JwC, JwD;
            var mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                JvAC = Vector2.Zero;
                JwA = 1.0f;
                JwC = 1.0f;
                mass += _iA + _iC;

                coordinateA = aA - aC - _referenceAngleA;
            }
            else
            {
                var u = qC * _localAxisC;
                var rC = qC * (_localAnchorC - _lcC);
                var rA = qA * (_localAnchorA - _lcA);
                JvAC = u;
                JwC = Vector2Util.Cross(rC, u);
                JwA = Vector2Util.Cross(rA, u);
                mass += _mC + _mA + _iC * JwC * JwC + _iA * JwA * JwA;

                var pC = _localAnchorC - _lcC;
                var pA = -qC * (rA + (Vector2)(cA - cC));
                coordinateA = Vector2.Dot(pA - pC, _localAxisC);
            }

            if (_typeB == JointType.Revolute)
            {
                JvBD = Vector2.Zero;
                JwB = _ratio;
                JwD = _ratio;
                mass += _ratio * _ratio * (_iB + _iD);

                coordinateB = aB - aD - _referenceAngleB;
            }
            else
            {
                var u = qD * _localAxisD;
                var rD = qD * (_localAnchorD - _lcD);
                var rB = qB * (_localAnchorB - _lcB);
                JvBD = _ratio * u;
                JwD = _ratio * Vector2Util.Cross(rD, u);
                JwB = _ratio * Vector2Util.Cross(rB, u);
                mass += _ratio * _ratio * (_mD + _mB) + _iD * JwD * JwD + _iB * JwB * JwB;

                var pD = _localAnchorD - _lcD;
                var pB = -qD * (rB + (Vector2)(cB - cD));
                coordinateB = Vector2.Dot(pB - pD, _localAxisD);
            }

            var c = (coordinateA + _ratio * coordinateB) - _constant;

            var impulse = 0.0f;
            if (mass > 0.0f)
            {
                impulse = -c / mass;
            }

            cA += _mA * impulse * JvAC;
            aA += _iA * impulse * JwA;
            cB += _mB * impulse * JvBD;
            aB += _iB * impulse * JwB;
            cC -= _mC * impulse * JvAC;
            aC -= _iC * impulse * JwC;
            cD -= _mD * impulse * JvBD;
            aD -= _iD * impulse * JwD;

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;
            positions[_indexC].Point = cC;
            positions[_indexC].Angle = aC;
            positions[_indexD].Point = cD;
            positions[_indexD].Angle = aD;

            // TODO_ERIN not implemented
            return linearError < Settings.LinearSlop;
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
            return base.Packetize(packet)
                .Write((byte)_typeA)
                .Write((byte)_typeB)
                .Write(_bodyIdC)
                .Write(_bodyIdD)
                .Write(_localAnchorA)
                .Write(_localAnchorB)
                .Write(_localAnchorC)
                .Write(_localAnchorD)
                .Write(_localAxisC)
                .Write(_localAxisD)
                .Write(_referenceAngleA)
                .Write(_referenceAngleB)
                .Write(_constant)
                .Write(_ratio)
                .Write(_impulse);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);

            _typeA = (JointType)packet.ReadByte();
            _typeB = (JointType)packet.ReadByte();
            _bodyIdC = packet.ReadInt32();
            _bodyIdD = packet.ReadInt32();
            _localAnchorA = packet.ReadVector2();
            _localAnchorB = packet.ReadVector2();
            _localAnchorC = packet.ReadVector2();
            _localAnchorD = packet.ReadVector2();
            _localAxisC = packet.ReadVector2();
            _localAxisD = packet.ReadVector2();
            _referenceAngleA = packet.ReadSingle();
            _referenceAngleB = packet.ReadSingle();
            _constant = packet.ReadSingle();
            _ratio = packet.ReadSingle();
            _impulse = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Serialization.Hasher hasher)
        {
            base.Hash(hasher);

            hasher
                .Put((byte)_typeA)
                .Put((byte)_typeB)
                .Put(_bodyIdC)
                .Put(_bodyIdD)
                .Put(_localAnchorA)
                .Put(_localAnchorB)
                .Put(_localAnchorC)
                .Put(_localAnchorD)
                .Put(_localAxisC)
                .Put(_localAxisD)
                .Put(_referenceAngleA)
                .Put(_referenceAngleB)
                .Put(_constant)
                .Put(_ratio)
                .Put(_impulse);
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

            var copy = (GearJoint)into;

            copy._typeA = _typeA;
            copy._typeB = _typeB;
            copy._bodyIdC = _bodyIdC;
            copy._bodyIdD = _bodyIdD;
            copy._localAnchorA = _localAnchorA;
            copy._localAnchorB = _localAnchorB;
            copy._localAnchorC = _localAnchorC;
            copy._localAnchorD = _localAnchorD;
            copy._localAxisC = _localAxisC;
            copy._localAxisD = _localAxisD;
            copy._referenceAngleA = _referenceAngleA;
            copy._referenceAngleB = _referenceAngleB;
            copy._constant = _constant;
            copy._ratio = _ratio;
            copy._impulse = _impulse;
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
            return base.ToString() +
                ", TypeA=" + _typeA +
                ", TypeB=" + _typeB +
                ", BodyC=" + _bodyIdC +
                ", BodyD=" + _bodyIdD +
                ", LocalAnchorA=" + _localAnchorA.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorA.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorB=" + _localAnchorB.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorB.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorC=" + _localAnchorC.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorC.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorD=" + _localAnchorD.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorD.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAxisC=" + _localAxisC.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAxisC.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAxisD=" + _localAxisD.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAxisD.Y.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceAngleA=" + _referenceAngleA.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceAngleB=" + _referenceAngleB.ToString(CultureInfo.InvariantCulture) +
                ", Constant=" + _constant.ToString(CultureInfo.InvariantCulture) +
                ", Ratio=" + _ratio.ToString(CultureInfo.InvariantCulture) +
                ", Impulse=" + _impulse.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
