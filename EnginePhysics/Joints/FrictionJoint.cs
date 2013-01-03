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
    public sealed class FrictionJoint : Joint
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
        /// Gets or sets the maximum friction force in N.
        /// </summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set { _maxForce = value; }
        }

        /// <summary>
        /// Gets or sets the maximum friction torque in N*m.
        /// </summary>
        public float MaxTorque
        {
            get { return _maxTorque; }
            set { _maxTorque = value; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private Vector2 _linearImpulse; // kept across updates for warm starting

        private float _angularImpulse; // kept across updates for warm starting

        private float _maxForce;

        private float _maxTorque;

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

        private float _inverseInertiaB;

        private Matrix22 _linearMass;

        private float _angularMass;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="FrictionJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public FrictionJoint() : base(JointType.Friction)
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

            var aA = positions[_indexA].Angle;
            var vA = velocities[_indexA].LinearVelocity;
            var wA = velocities[_indexA].AngularVelocity;

            var aB = positions[_indexB].Angle;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            // Compute the effective mass matrix.
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

            // Solve angular friction
            {
                var cdot = wB - wA;
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
                var cdot = vB + Vector2Util.Cross(wB, _rB) - vA - Vector2Util.Cross(wA, _rA);

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
                .Write(_localAnchorA)
                .Write(_localAnchorB)
                .Write(_linearImpulse)
                .Write(_angularImpulse)
                .Write(_maxForce)
                .Write(_maxTorque);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);

            _localAnchorA = packet.ReadVector2();
            _localAnchorB = packet.ReadVector2();
            _linearImpulse = packet.ReadVector2();
            _angularImpulse = packet.ReadSingle();
            _maxForce = packet.ReadSingle();
            _maxTorque = packet.ReadSingle();
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
                .Put(_localAnchorA)
                .Put(_localAnchorB)
                .Put(_linearImpulse)
                .Put(_angularImpulse)
                .Put(_maxForce)
                .Put(_maxTorque);
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

            var copy = (FrictionJoint)into;

            copy._localAnchorA = _localAnchorA;
            copy._localAnchorB = _localAnchorB;
            copy._linearImpulse = _linearImpulse;
            copy._angularImpulse = _angularImpulse;
            copy._maxForce = _maxForce;
            copy._maxTorque = _maxTorque;
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
                   ", LocalAnchorA=" + _localAnchorA.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorA.Y.ToString(CultureInfo.InvariantCulture) +
                   ", LocalAnchorB=" + _localAnchorB.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorB.Y.ToString(CultureInfo.InvariantCulture) +
                   ", LinearImpulse=" + _linearImpulse.X.ToString(CultureInfo.InvariantCulture) + ":" + _linearImpulse.Y.ToString(CultureInfo.InvariantCulture) +
                   ", AngularImpulse=" + _angularImpulse.ToString(CultureInfo.InvariantCulture) +
                   ", MaxForce=" + _maxForce.ToString(CultureInfo.InvariantCulture) +
                   ", MaxTorque=" + _maxTorque.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
