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
    public sealed class DistanceJoint : Joint
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
        /// Gets or sets the length of the joint.
        /// </summary>
        public float Length
        {
            get { return _length; }
            set { _length = value; }
        }

        /// <summary>
        /// Gets or sets the frequency in Hz.
        /// </summary>
        public float Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Gets or sets the damping ratio.
        /// </summary>
        public float DampingRatio
        {
            get { return _dampingRatio; }
            set { _dampingRatio = value; }
        }

        #endregion

        #region Fields

        #region Solver shared

        private float _frequency;

        private float _dampingRatio;

        private float _bias;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _gamma;

        private float _impulse;

        private float _length;

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
        /// Initializes a new instance of the <see cref="DistanceJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public DistanceJoint() : base(JointType.Distance)
        {
        }

        /// <summary>
        /// Initializes the specified local anchor A.
        /// </summary>
        /// <param name="localAnchorA">The local anchor point relative to the first body's origin.</param>
        /// <param name="localAnchorB">The local anchor point relative to the second body's origin.</param>
        /// <param name="length">The natural length between the anchor points.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. A value of 0 disables softness.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        internal void Initialize(LocalPoint localAnchorA, LocalPoint localAnchorB,
                                 float length, float frequency, float dampingRatio)
        {
            _localAnchorA = localAnchorA;
            _localAnchorB = localAnchorB;
            _length = System.Math.Max(0.1f, length);
            _frequency = System.Math.Max(0, frequency);
            _dampingRatio = System.Math.Max(0, dampingRatio);
        }

        #endregion

        #region Logic

        // 1-D constrained system
        // m (v2 - v1) = lambda
        // v2 + (beta/h) * x1 + gamma * lambda = 0, gamma has units of inverse mass.
        // x2 = x1 + h * v2

        // 1-D mass-damper-spring system
        // m (v2 - v1) + h * d * v2 + h * k * 

        // C = norm(p2 - p1) - L
        // u = (p2 - p1) / norm(p2 - p1)
        // Cdot = dot(u, v2 + cross(w2, r2) - v1 - cross(w1, r1))
        // J = [-u -cross(r1, u) u cross(r2, u)]
        // K = J * invM * JT
        //   = invMass1 + invI1 * cross(r1, u)^2 + invMass2 + invI2 * cross(r2, u)^2

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

            // Handle singularity.
            var length = _u.Length();
            if (length > Settings.LinearSlop)
            {
                _u *= 1.0f / length;
            }
            else
            {
                _u = Vector2.Zero;
            }

            var crAu = Vector2Util.Cross(_rA, _u);
            var crBu = Vector2Util.Cross(_rB, _u);
            var invMass = _inverseMassA + _inverseInertiaA * crAu * crAu + _inverseMassB +
                          _inverseInertiaB * crBu * crBu;

            // Compute the effective mass matrix.
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

            if (_frequency > 0.0f)
            {
                var c = length - _length;

                // Frequency
                var omega = 2.0f * MathHelper.Pi * _frequency;

                // Damping coefficient
                var d = 2.0f * _mass * _dampingRatio * omega;

                // Spring stiffness
                var k = _mass * omega * omega;

                // magic formulas
                var h = step.DeltaT;
                _gamma = h * (d + h * k);
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (_gamma != 0.0f)
                {
                    _gamma = 1.0f / _gamma;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
                _bias = c * h * k * _gamma;

                invMass += _gamma;
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
            }
            else
            {
                _gamma = 0.0f;
                _bias = 0.0f;
            }

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
            var cdot = Vector2.Dot(_u, vpB - vpA);

            var impulse = -_mass * (cdot + _bias + _gamma * _impulse);
            _impulse += impulse;

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
            if (_frequency > 0.0f)
            {
                // There is no position correction for soft distance constraints.
                return true;
            }

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
            var c = length - _length;
            c = MathHelper.Clamp(c, -Settings.MaxLinearCorrection, Settings.MaxLinearCorrection);

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

            return System.Math.Abs(c) < Settings.LinearSlop;
        }

        #endregion
    }
}
