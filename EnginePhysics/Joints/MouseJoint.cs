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
    /// A mouse joint is used to make a point on a body track a
    /// specified world point. This a soft constraint with a maximum
    /// force. This allows the constraint to stretch and without
    /// applying huge forces.
    /// </summary>
    public sealed class MouseJoint : Joint
    {
        #region Properties
        
        /// <summary>
        /// Get the anchor point on the first body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorA
        {
            get { return _targetA; }
        }

        /// <summary>
        /// Get the anchor point on the second body in world coordinates.
        /// </summary>
        public override WorldPoint AnchorB
        {
            get { return BodyB.GetWorldPoint(_localAnchorB); }
        }

        /// <summary>
        /// Gets or sets the target world point. This wakes up the body.
        /// </summary>
        public WorldPoint Target
        {
            get { return _targetA; }
            set
            {
                _targetA = value;
                BodyB.IsAwake = true;
            }
        }

        /// <summary>
        /// Gets or sets the maximum force we apply to the body.
        /// </summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set { _maxForce = value; }
        }

        /// <summary>
        /// Gets or sets the update frequency.
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

        private LocalPoint _localAnchorB;
        private WorldPoint _targetA;
        private float _frequency;
        private float _dampingRatio;
        private float _beta;

        // Solver shared
        private Vector2 _impulse;
        private float _maxForce;
        private float _gamma;

        // Solver temp
        private int _indexB;
        private LocalPoint _rotatedB;
        private LocalPoint _localCenterB;
        private float _inverseMassB;
        private float _inverseInertiaB;
        private Mat22 _mass;
        private Vector2 _c;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseJoint"/> class.
        /// </summary>
        public MouseJoint() : base(JointType.Mouse)
        {
        }

        /// <summary>
        /// Initializes the joint with the specified properties.
        /// </summary>
        /// <param name="target">The initial world target point. This is assumed
        /// to coincide with the body anchor initially.
        /// </param>
        /// <param name="maxForce">The maximum constraint force that can be exerted
        /// to move the candidate body. Usually you will express as some multiple
        /// of the weight (multiplier * mass * gravity).</param>
        /// <param name="frequency">The response speed.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        internal void Initialize(WorldPoint target, float maxForce, float frequency, float dampingRatio)
        {
            System.Diagnostics.Debug.Assert(maxForce >= 0.0f);
            System.Diagnostics.Debug.Assert(frequency >= 0.0f);
            System.Diagnostics.Debug.Assert(dampingRatio >= 0.0f);

            _targetA = target;
            _localAnchorB = BodyB.GetLocalPoint(_targetA);

            _maxForce = System.Math.Max(0, maxForce);
            _impulse = Vector2.Zero;

            _frequency = System.Math.Max(0, frequency);
            _dampingRatio = System.Math.Max(0, dampingRatio);

            _beta = 0.0f;
            _gamma = 0.0f;
        }

        #endregion

        #region Logic

        // p = attached point, m = mouse point
        // C = p - m
        // Cdot = v
        //      = v + cross(w, r)
        // J = [I r_skew]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        /// <summary>
        /// Initializes the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void InitializeVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            _indexB = BodyB.IslandIndex;
            _localCenterB = BodyB.Sweep.LocalCenter;
            _inverseMassB = BodyB.InverseMass;
            _inverseInertiaB = BodyB.InverseInertia;

            var cB = positions[_indexB].Point;
            var aB = positions[_indexB].Angle;
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            var qB = new Rotation(aB);

            var mass = BodyB.MassInternal;

            // Frequency
            var omega = 2.0f * MathHelper.Pi * _frequency;

            // Damping coefficient
            var d = 2.0f * mass * _dampingRatio * omega;

            // Spring stiffness
            var k = mass * (omega * omega);

            // magic formulas
            // gamma has units of inverse mass.
            // beta has units of inverse time.
            var h = step.DeltaT;
            System.Diagnostics.Debug.Assert(d + h * k > Settings.Epsilon);
            _gamma = h * (d + h * k);
            if (_gamma != 0.0f)
            {
                _gamma = 1.0f / _gamma;
            }
            _beta = h * k * _gamma;

            // Compute the effective mass matrix.
            _rotatedB = qB * (_localAnchorB - _localCenterB);

            // K    = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
            //      = [1/m1+1/m2     0    ] + invI1 * [r1.y*r1.y -r1.x*r1.y] + invI2 * [r1.y*r1.y -r1.x*r1.y]
            //        [    0     1/m1+1/m2]           [-r1.x*r1.y r1.x*r1.x]           [-r1.x*r1.y r1.x*r1.x]
            Mat22 K;
            K.Column1.X = _inverseMassB + _inverseInertiaB * _rotatedB.Y * _rotatedB.Y + _gamma;
            K.Column1.Y = -_inverseInertiaB * _rotatedB.X * _rotatedB.Y;
            K.Column2.X = K.Column1.Y;
            K.Column2.Y = _inverseMassB + _inverseInertiaB * _rotatedB.X * _rotatedB.X + _gamma;

            _mass = K.GetInverse();

            _c = (Vector2)(cB - _targetA) + _rotatedB;
            _c *= _beta;

            // Cheat with some damping
            wB *= 0.98f;

            if (step.IsWarmStarting)
            {
                vB += _inverseMassB * _impulse;
                wB += _inverseInertiaB * Vector2Util.Cross(_rotatedB, _impulse);
            }
            else
            {
                _impulse = Vector2.Zero;
            }

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
            var vB = velocities[_indexB].LinearVelocity;
            var wB = velocities[_indexB].AngularVelocity;

            // Cdot = v + cross(w, r)
            var dot = vB + Vector2Util.Cross(wB, _rotatedB);
            var impulse = _mass * -(dot + _c + _gamma * _impulse);

            var oldImpulse = _impulse;
            _impulse += impulse;
            var maxImpulse = step.DeltaT * _maxForce;
            if (_impulse.LengthSquared() > maxImpulse * maxImpulse)
            {
                _impulse *= maxImpulse / _impulse.Length();
            }
            impulse = _impulse - oldImpulse;

            vB += _inverseMassB * impulse;
            wB += _inverseInertiaB * Vector2Util.Cross(_rotatedB, impulse);

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
    }
}
