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
    public sealed class RopeJoint : Joint
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
        /// Set/Get the maximum length of the rope.
        /// </summary>
        public float MaxLength
        {
            get { return _maxLength; }
            set { _maxLength = value; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _maxLength;

        private float _length;

        private float _impulse;

        #endregion

        #region Solver temp

        private int _indexA;

        private int _indexB;

        private Vector2 _u;

        private Vector2 _rA;

        private Vector2 _rB;

        private LocalPoint _localCenterA;

        private LocalPoint _localCenterB;

        private float _inverseMassA;

        private float _inverseMassB;

        private float _inverseInertiaA;

        private float _inverseInertiaB;

        private float _mass;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="RopeJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public RopeJoint() : base(JointType.Rope)
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

        // Limit:
        // C = norm(pB - pA) - L
        // u = (pB - pA) / norm(pB - pA)
        // Cdot = dot(u, vB + cross(wB, rB) - vA - cross(wA, rA))
        // J = [-u -cross(rA, u) u cross(rB, u)]
        // K = J * invM * JT
        //   = invMassA + invIA * cross(rA, u)^2 + invMassB + invIB * cross(rB, u)^2

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

            _rA = qA * (_localAnchorA - _localCenterA);
            _rB = qB * (_localAnchorB - _localCenterB);
            _u = (Vector2)(cB - cA) + (_rB - _rA);

            _length = _u.Length();

            if (_length > Settings.LinearSlop)
            {
                _u *= 1.0f / _length;
            }
            else
            {
                _u = Vector2.Zero;
                _mass = 0.0f;
                _impulse = 0.0f;
                return;
            }

            // Compute effective mass.
            var crA = Vector2Util.Cross(_rA, _u);
            var crB = Vector2Util.Cross(_rB, _u);
            var invMass = _inverseMassA + _inverseInertiaA * crA * crA + _inverseMassB + _inverseInertiaB * crB * crB;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (invMass != 0.0f)
            {
                _mass = 1.0f / invMass;
            }
            else
            {
                _mass = 0.0f;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            if (step.IsWarmStarting)
            {
                var p = _impulse * _u;
                vA -= _inverseMassA * p;
                wA -= _inverseInertiaA * Vector2Util.Cross(_rA, p);
                vB += _inverseMassB * p;
                wB += _inverseInertiaB * Vector2Util.Cross(_rB, p);
            }
            else
            {
                _impulse = 0.0f;
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

            // Cdot = dot(u, v + cross(w, r))
            var vpA = vA + Vector2Util.Cross(wA, _rA);
            var vpB = vB + Vector2Util.Cross(wB, _rB);
            var c = _length - _maxLength;
            var cdot = Vector2.Dot(_u, vpB - vpA);

            // Predictive constraint.
            if (c < 0.0f)
            {
                cdot += step.InverseDeltaT * c;
            }

            var impulse = -_mass * cdot;
            var oldImpulse = _impulse;
            _impulse = System.Math.Min(0.0f, _impulse + impulse);
            impulse = _impulse - oldImpulse;

            var p = impulse * _u;
            vA -= _inverseMassA * p;
            wA -= _inverseInertiaA * Vector2Util.Cross(_rA, p);
            vB += _inverseMassB * p;
            wB += _inverseInertiaB * Vector2Util.Cross(_rB, p);

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
            var u = (Vector2)(cB - cA) + (rB - rA);

            var length = u.Length();
            u /= length;
            var c = length - _maxLength;

            c = MathHelper.Clamp(c, 0.0f, Settings.MaxLinearCorrection);

            var impulse = -_mass * c;
            var p = impulse * u;

            cA -= _inverseMassA * p;
            aA -= _inverseInertiaA * Vector2Util.Cross(rA, p);
            cB += _inverseMassB * p;
            aB += _inverseInertiaB * Vector2Util.Cross(rB, p);

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;

            return length - _maxLength < Settings.LinearSlop;
        }

        #endregion
    }
}
