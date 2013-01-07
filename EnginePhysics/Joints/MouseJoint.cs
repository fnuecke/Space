using System.Globalization;
using Engine.Physics.Math;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

#if FARMATH
using Engine.FarMath;
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
        /// Gets or sets the update frequency in Hz.
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

        private float _maxForce;

        private Vector2 _impulse;

        private struct SolverTemp
        {
            public float Gamma;

            public float Beta;

            public int IndexB;

            public LocalPoint RotB;

            public LocalPoint LocalCenterB;

            public float InverseMassB;

            public float InverseInertiaB;

            public Matrix22 Mass;

            public Vector2 C;
        }

        [CopyIgnore, PacketizerIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
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
        /// <param name="frequency">The response speed in Hz.</param>
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

            _tmp.Beta = 0.0f;
            _tmp.Gamma = 0.0f;
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
            _tmp.IndexB = BodyB.IslandIndex;
            _tmp.LocalCenterB = BodyB.Sweep.LocalCenter;
            _tmp.InverseMassB = BodyB.InverseMass;
            _tmp.InverseInertiaB = BodyB.InverseInertia;

            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var qB = new Rotation(aB);

            var mass = BodyB.MassInternal;

            // Frequency
            var omega = 2.0f * MathHelper.Pi * _frequency;

            // Damping coefficient
            var d = 2.0f * mass * _dampingRatio * omega;

            // Spring stiffness
            var s = mass * (omega * omega);

            // magic formulas
            // gamma has units of inverse mass.
            // beta has units of inverse time.
            var h = step.DeltaT;
            System.Diagnostics.Debug.Assert(d + h * s > Settings.Epsilon);
            _tmp.Gamma = h * (d + h * s);
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (_tmp.Gamma != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                _tmp.Gamma = 1.0f / _tmp.Gamma;
            }
            _tmp.Beta = h * s * _tmp.Gamma;

            // Compute the effective mass matrix.
            _tmp.RotB = qB * (_localAnchorB - _tmp.LocalCenterB);

            // K    = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
            //      = [1/m1+1/m2     0    ] + invI1 * [r1.Y*r1.Y -r1.X*r1.Y] + invI2 * [r1.Y*r1.Y -r1.X*r1.Y]
            //        [    0     1/m1+1/m2]           [-r1.X*r1.Y r1.X*r1.X]           [-r1.X*r1.Y r1.X*r1.X]
            Matrix22 k;
            k.Column1.X = _tmp.InverseMassB + _tmp.InverseInertiaB * _tmp.RotB.Y * _tmp.RotB.Y + _tmp.Gamma;
            k.Column1.Y = -_tmp.InverseInertiaB * _tmp.RotB.X * _tmp.RotB.Y;
            k.Column2.X = k.Column1.Y;
            k.Column2.Y = _tmp.InverseMassB + _tmp.InverseInertiaB * _tmp.RotB.X * _tmp.RotB.X + _tmp.Gamma;

            _tmp.Mass = k.GetInverse();
            
// ReSharper disable RedundantCast Necessary for FarPhysics.
            _tmp.C = (Vector2)(cB - _targetA) + _tmp.RotB;
// ReSharper restore RedundantCast
            _tmp.C *= _tmp.Beta;

            // Cheat with some damping
            wB *= 0.98f;

            vB += _tmp.InverseMassB * _impulse;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(_tmp.RotB, _impulse);

            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
        }

        /// <summary>
        /// Solves the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal override void SolveVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            // Cdot = v + cross(w, r)
            var cdot = vB + Vector2Util.Cross(wB, _tmp.RotB);
            var impulse = _tmp.Mass * -(cdot + _tmp.C + _tmp.Gamma * _impulse);

            var oldImpulse = _impulse;
            _impulse += impulse;
            var maxImpulse = step.DeltaT * _maxForce;
            if (_impulse.LengthSquared() > maxImpulse * maxImpulse)
            {
                _impulse *= maxImpulse / _impulse.Length();
            }
            impulse = _impulse - oldImpulse;

            vB += _tmp.InverseMassB * impulse;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(_tmp.RotB, impulse);

            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
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
                ", LocalAnchorB=" + _localAnchorB.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorB.Y.ToString(CultureInfo.InvariantCulture) +
#if FARMATH
                ", TargetA=" + _targetA.X.ToString() + ":" + _targetA.Y.ToString() +
#else
                ", TargetA=" + _targetA.X.ToString(CultureInfo.InvariantCulture) + ":" + _targetA.Y.ToString(CultureInfo.InvariantCulture) +
#endif
                ", Frequency=" + _frequency.ToString(CultureInfo.InvariantCulture) +
                ", DamingRatio=" + _dampingRatio.ToString(CultureInfo.InvariantCulture) +
                ", MaxForce=" + _maxForce.ToString(CultureInfo.InvariantCulture) +
                ", Impulse=" + _impulse.X.ToString(CultureInfo.InvariantCulture) + ":" + _impulse.Y.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
