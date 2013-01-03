using System.Globalization;
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
    public sealed class MotorJoint : Joint
    {
        #region Properties

        /// <summary>
        /// Get the anchor point on the first body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorA
        {
            get { return BodyA.Position; }
        }

        /// <summary>
        /// Get the anchor point on the second body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorB
        {
            get { return BodyB.Position; }
        }

        /// <summary>
        /// Set/Get the target linear offset, in frame A, in meters.
        /// </summary>
        public Vector2 LinearOffset
        {
            get { return _linearOffset; }
            set
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (value.X != _linearOffset.X || value.Y != _linearOffset.Y)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _linearOffset = value;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        /// <summary>
        /// Set/Get the target angular offset, in radians.
        /// </summary>
        public float AngularOffset
        {
            get { return _angularOffset; }
            set
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (value != _angularOffset)
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _angularOffset = value;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        /// <summary>
        /// Set/Get the maximum friction force in N.
        /// </summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set { _maxForce = value; }
        }

        /// <summary>
        /// Set/Get the maximum friction torque in N*m.
        /// </summary>
        public float MaxTorque
        {
            get { return _maxTorque; }
            set { _maxTorque = value; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private Vector2 _linearOffset;

        private float _angularOffset;

        private Vector2 _linearImpulse;

        private float _angularImpulse;

        private float _maxForce;

        private float _maxTorque;

        private float _correctionFactor;

        #endregion

        #region Solver temp

        private int _indexA;

        private int _indexB;

        private Vector2 _rA;

        private Vector2 _rB;

        private LocalPoint _localCenterA;

        private LocalPoint _localCenterB;

        private Vector2 _linearError;

        private float _angularError;

        private float _inverseMassA;

        private float _inverseMassB;

        private float _inverseInertiaA;

        private float _inverseInertiaB;

        private Matrix22 _linearMass;

        private float _angularMass;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public MotorJoint() : base(JointType.Motor)
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

        // Point-to-point constraint
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
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

            // Compute the effective mass matrix.
            _rA = qA * -_localCenterA;
            _rB = qB * -_localCenterB;

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
            var iB = _inverseInertiaB;

            Matrix22 k;
            k.Column1.X = mA + mB + iA * _rA.Y * _rA.Y + iB * _rB.Y * _rB.Y;
            k.Column1.Y = -iA * _rA.X * _rA.Y - iB * _rB.X * _rB.Y;
            k.Column2.X = k.Column1.Y;
            k.Column2.Y = mA + mB + iA * _rA.X * _rA.X + iB * _rB.X * _rB.X;

            _linearMass = k.GetInverse();

            _angularMass = iA + iB;
            if (_angularMass > 0.0f)
            {
                _angularMass = 1.0f / _angularMass;
            }

            _linearError = (Vector2)(cB - cA) + (_rB - _rA) - qA * _linearOffset;
            _angularError = aB - aA - _angularOffset;

            if (step.IsWarmStarting)
            {
                var p = new Vector2(_linearImpulse.X, _linearImpulse.Y);
                vA -= mA * p;
                wA -= iA * (Vector2Util.Cross(_rA, p) + _angularImpulse);
                vB += mB * p;
                wB += iB * (Vector2Util.Cross(_rB, p) + _angularImpulse);
            }
            else
            {
                _linearImpulse = Vector2.Zero;
                _angularImpulse = 0.0f;
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

            var h = step.DeltaT;
            var invH = step.InverseDeltaT;

            // Solve angular friction
            {
                var cdot = wB - wA + invH * _correctionFactor * _angularError;
                var impulse = -_angularMass * cdot;

                var oldImpulse = _angularImpulse;
                var maxImpulse = h * _maxTorque;
                _angularImpulse = MathHelper.Clamp(_angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve linear friction
            {
                var cdot = vB + Vector2Util.Cross(wB, _rB) - vA - Vector2Util.Cross(wA, _rA) +
                           invH * _correctionFactor * _linearError;

                var impulse = -(_linearMass * cdot);
                var oldImpulse = _linearImpulse;
                _linearImpulse += impulse;

                var maxImpulse = h * _maxForce;

                if (_linearImpulse.LengthSquared() > maxImpulse * maxImpulse)
                {
                    _linearImpulse.Normalize();
                    _linearImpulse *= maxImpulse;
                }

                impulse = _linearImpulse - oldImpulse;

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
            return true;
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
                .Write(_linearOffset)
                .Write(_angularOffset)
                .Write(_linearImpulse)
                .Write(_angularImpulse)
                .Write(_maxForce)
                .Write(_maxTorque)
                .Write(_correctionFactor);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);

            _linearOffset = packet.ReadVector2();
            _angularOffset = packet.ReadSingle();
            _linearImpulse = packet.ReadVector2();
            _angularImpulse = packet.ReadSingle();
            _maxForce = packet.ReadSingle();
            _maxTorque = packet.ReadSingle();
            _correctionFactor = packet.ReadSingle();
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
                .Put(_linearOffset)
                .Put(_angularOffset)
                .Put(_linearImpulse)
                .Put(_angularImpulse)
                .Put(_maxForce)
                .Put(_maxTorque)
                .Put(_correctionFactor);
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

            var copy = (MotorJoint)into;

            copy._linearOffset = _linearOffset;
            copy._angularOffset = _angularOffset;
            copy._linearImpulse = _linearImpulse;
            copy._angularImpulse = _angularImpulse;
            copy._maxForce = _maxForce;
            copy._maxTorque = _maxTorque;
            copy._correctionFactor = _correctionFactor;
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
                ", LinearOffset=" + _linearOffset.X.ToString(CultureInfo.InvariantCulture) + ":" + _linearOffset.Y.ToString(CultureInfo.InvariantCulture) +
                ", AngularOffset=" + _angularOffset.ToString(CultureInfo.InvariantCulture) +
                ", LinearImpulse=" + _linearImpulse.X.ToString(CultureInfo.InvariantCulture) + ":" + _linearImpulse.Y.ToString(CultureInfo.InvariantCulture) +
                ", AngularImpulse=" + _angularImpulse.ToString(CultureInfo.InvariantCulture) +
                ", MaxForce=" + _maxForce.ToString(CultureInfo.InvariantCulture) +
                ", MaxTorque=" + _maxTorque.ToString(CultureInfo.InvariantCulture) +
                ", CorrectionFactor=" + _correctionFactor.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
