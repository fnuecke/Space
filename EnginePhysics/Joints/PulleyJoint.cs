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
    public sealed class PulleyJoint : Joint
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

        public WorldPoint GroundAnchorA
        {
            get { return _groundAnchorA; }
        }

        public WorldPoint GroundAnchorB
        {
            get { return _groundAnchorB; }
        }

        public float LengthA
        {
            get { return _lengthA; }
        }

        public float LengthB
        {
            get { return _lengthB; }
        }

        public float Ratio
        {
            get { return _ratio; }
        }

        public float CurrentLengthA
        {
            get
            {
                var p = BodyA.GetWorldPoint(_localAnchorA);
                var s = _groundAnchorA;
                var d = (Vector2)(p - s);
                return d.Length();
            }
        }

        public float CurrentLengthB
        {
            get
            {
                var p = BodyB.GetWorldPoint(_localAnchorB);
                var s = _groundAnchorB;
                var d = (Vector2)(p - s);
                return d.Length();
            }
        }

        #endregion

        #region Fields

        #region Solver shared

        private WorldPoint _groundAnchorA;

        private WorldPoint _groundAnchorB;

        private float _lengthA;

        private float _lengthB;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _constant;

        private float _ratio;

        private float _impulse;

        #endregion

        #region Solver temp

        private int _indexA;

        private int _indexB;

        private Vector2 _uA;

        private Vector2 _uB;

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
        /// Initializes a new instance of the <see cref="PulleyJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public PulleyJoint() : base(JointType.Pulley)
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

        // Pulley:
        // length1 = norm(p1 - s1)
        // length2 = norm(p2 - s2)
        // C0 = (length1 + ratio * length2)_initial
        // C = C0 - (length1 + ratio * length2)
        // u1 = (p1 - s1) / norm(p1 - s1)
        // u2 = (p2 - s2) / norm(p2 - s2)
        // Cdot = -dot(u1, v1 + cross(w1, r1)) - ratio * dot(u2, v2 + cross(w2, r2))
        // J = -[u1 cross(r1, u1) ratio * u2  ratio * cross(r2, u2)]
        // K = J * invM * JT
        //   = invMass1 + invI1 * cross(r1, u1)^2 + ratio^2 * (invMass2 + invI2 * cross(r2, u2)^2)

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

            // Get the pulley axes.
            _uA = (Vector2)(cA - _groundAnchorA) + _rA;
            _uB = (Vector2)(cB - _groundAnchorB) + _rB;

            var lengthA = _uA.Length();
            var lengthB = _uB.Length();

            if (lengthA > 10.0f * Settings.LinearSlop)
            {
                _uA *= 1.0f / lengthA;
            }
            else
            {
                _uA = Vector2.Zero;
            }

            if (lengthB > 10.0f * Settings.LinearSlop)
            {
                _uB *= 1.0f / lengthB;
            }
            else
            {
                _uB = Vector2.Zero;
            }

            // Compute effective mass.
            float ruA = Vector2Util.Cross(_rA, _uA);
            float ruB = Vector2Util.Cross(_rB, _uB);

            float mA = _inverseMassA + _inverseInertiaA * ruA * ruA;
            float mB = _inverseMassB + _inverseInertiaB * ruB * ruB;

            _mass = mA + _ratio * _ratio * mB;

            if (_mass > 0.0f)
            {
                _mass = 1.0f / _mass;
            }

            if (step.IsWarmStarting)
            {
                // Warm starting.
                var pA = -(_impulse) * _uA;
                var pB = (-_ratio * _impulse) * _uB;

                vA += _inverseMassA * pA;
                wA += _inverseInertiaA * Vector2Util.Cross(_rA, pA);
                vB += _inverseMassB * pB;
                wB += _inverseInertiaB * Vector2Util.Cross(_rB, pB);
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

            var vpA = vA + Vector2Util.Cross(wA, _rA);
            var vpB = vB + Vector2Util.Cross(wB, _rB);

            var cdot = -Vector2.Dot(_uA, vpA) - _ratio * Vector2.Dot(_uB, vpB);
            var impulse = -_mass * cdot;
            _impulse += impulse;

            var pA = -impulse * _uA;
            var pB = -_ratio * impulse * _uB;
            vA += _inverseMassA * pA;
            wA += _inverseInertiaA * Vector2Util.Cross(_rA, pA);
            vB += _inverseMassB * pB;
            wB += _inverseInertiaB * Vector2Util.Cross(_rB, pB);

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

            // Get the pulley axes.
            var uA = (Vector2)(cA - _groundAnchorA) + rA;
            var uB = (Vector2)(cB - _groundAnchorB) + rB;

            var lengthA = uA.Length();
            var lengthB = uB.Length();

            if (lengthA > 10.0f * Settings.LinearSlop)
            {
                uA *= 1.0f / lengthA;
            }
            else
            {
                uA = Vector2.Zero;
            }

            if (lengthB > 10.0f * Settings.LinearSlop)
            {
                uB *= 1.0f / lengthB;
            }
            else
            {
                uB = Vector2.Zero;
            }

            // Compute effective mass.
            var ruA = Vector2Util.Cross(rA, uA);
            var ruB = Vector2Util.Cross(rB, uB);

            var mA = _inverseMassA + _inverseInertiaA * ruA * ruA;
            var mB = _inverseMassB + _inverseInertiaB * ruB * ruB;

            var mass = mA + _ratio * _ratio * mB;

            if (mass > 0.0f)
            {
                mass = 1.0f / mass;
            }

            var c = _constant - lengthA - _ratio * lengthB;
            var linearError = System.Math.Abs(c);

            var impulse = -mass * c;

            var pA = -impulse * uA;
            var pB = -_ratio * impulse * uB;

            cA += _inverseMassA * pA;
            aA += _inverseInertiaA * Vector2Util.Cross(rA, pA);
            cB += _inverseMassB * pB;
            aB += _inverseInertiaB * Vector2Util.Cross(rB, pB);

            positions[_indexA].Point = cA;
            positions[_indexA].Angle = aA;
            positions[_indexB].Point = cB;
            positions[_indexB].Angle = aB;

            return linearError < Settings.LinearSlop;
        }

        #endregion
    }
}
