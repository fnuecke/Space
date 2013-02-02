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
    public sealed class DistanceJoint : Joint
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

        /// <summary>Gets or sets the length of the joint.</summary>
        public float Length
        {
            get { return _length; }
            set { _length = value; }
        }

        /// <summary>Gets or sets the frequency in Hz.</summary>
        public float Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>Gets or sets the damping ratio.</summary>
        public float DampingRatio
        {
            get { return _dampingRatio; }
            set { _dampingRatio = value; }
        }

        #endregion

        #region Fields

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _length;

        private float _frequency;

        private float _dampingRatio;

        private float _impulse; // kept across updates for warm starting

        private struct SolverTemp
        {
            public float Bias;

            public float Gamma;

            public int IndexA;

            public int IndexB;

            public Vector2 U;

            public Vector2 RotA;

            public Vector2 RotB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public float InverseMassA;

            public float InverseMassB;

            public float InverseInertiaA;

            public float InverseInertiaB;

            public float Mass;
        }

        [CopyIgnore, PacketizeIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistanceJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public DistanceJoint() : base(JointType.Distance) {}

        /// <summary>Initializes the specified local anchor A.</summary>
        /// <param name="anchorA">The world anchor point for the first body.</param>
        /// <param name="anchorB">The world anchor point for the second body.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. A value of 0 disables softness.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        internal void Initialize(
            WorldPoint anchorA,
            WorldPoint anchorB,
            float frequency,
            float dampingRatio)
        {
            _localAnchorA = BodyA.GetLocalPoint(anchorA);
            _localAnchorB = BodyB.GetLocalPoint(anchorB);
            _length = System.Math.Max(0.1f, WorldPoint.Distance(anchorA, anchorB));
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
        // K = J * invM * JTB
        //   = invMass1 + invI1 * cross(r1, u)^2 + invMass2 + invI2 * cross(r2, u)^2

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

            _tmp.RotA = qA * (_localAnchorA - _tmp.LocalCenterA);
            _tmp.RotB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            _tmp.U = (Vector2) (cB - cA) + (_tmp.RotB - _tmp.RotA);
// ReSharper restore RedundantCast

            // Handle singularity.
            var length = _tmp.U.Length();
            if (length > Settings.LinearSlop)
            {
                _tmp.U *= 1.0f / length;
            }
            else
            {
                _tmp.U = Vector2.Zero;
            }

            var crAu = Vector2Util.Cross(ref _tmp.RotA, ref _tmp.U);
            var crBu = Vector2Util.Cross(ref _tmp.RotB, ref _tmp.U);
            var inverseMass = _tmp.InverseMassA + _tmp.InverseInertiaA * crAu * crAu + _tmp.InverseMassB +
                              _tmp.InverseInertiaB * crBu * crBu;

            // Compute the effective mass matrix.
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (inverseMass != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                _tmp.Mass = 1.0f / inverseMass;
            }
            else
            {
                _tmp.Mass = 0.0f;
            }

            if (_frequency > 0.0f)
            {
                var c = length - _length;

                // Frequency
                var omega = 2.0f * MathHelper.Pi * _frequency;

                // Damping coefficient
                var d = 2.0f * _tmp.Mass * _dampingRatio * omega;

                // Spring stiffness
                var k = _tmp.Mass * omega * omega;

                // magic formulas
                var h = step.DeltaT;
                _tmp.Gamma = h * (d + h * k);
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (_tmp.Gamma != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    _tmp.Gamma = 1.0f / _tmp.Gamma;
                }
                _tmp.Bias = c * h * k * _tmp.Gamma;

                inverseMass += _tmp.Gamma;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (inverseMass != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    _tmp.Mass = 1.0f / inverseMass;
                }
                else
                {
                    _tmp.Mass = 0.0f;
                }
            }
            else
            {
                _tmp.Gamma = 0.0f;
                _tmp.Bias = 0.0f;
            }

            var p = _impulse * _tmp.U;
            vA -= _tmp.InverseMassA * p;
            wA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref _tmp.RotA, ref p);
            vB += _tmp.InverseMassB * p;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(ref _tmp.RotB, ref p);

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
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            // cDot = dot(u, v + cross(w, r))
            var vpA = vA + Vector2Util.Cross(wA, ref _tmp.RotA);
            var vpB = vB + Vector2Util.Cross(wB, ref _tmp.RotB);
            var cDot = Vector2Util.Dot(_tmp.U, vpB - vpA);

            var impulse = -_tmp.Mass * (cDot + _tmp.Bias + _tmp.Gamma * _impulse);
            _impulse += impulse;

            var p = impulse * _tmp.U;
            vA -= _tmp.InverseMassA * p;
            wA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref _tmp.RotA, ref p);
            vB += _tmp.InverseMassB * p;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(ref _tmp.RotB, ref p);

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
            if (_frequency > 0.0f)
            {
                // There is no position correction for soft distance constraints.
                return true;
            }

            var cA = positions[_tmp.IndexA].Point;
            var aA = positions[_tmp.IndexA].Angle;
            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var u = (Vector2) (cB - cA) + (rB - rA);
// ReSharper restore RedundantCast

            var length = u.Length();
            u /= length;
            var c = length - _length;
            c = MathHelper.Clamp(c, -Settings.MaxLinearCorrection, Settings.MaxLinearCorrection);

            var impulse = -_tmp.Mass * c;
            var p = impulse * u;

            cA -= _tmp.InverseMassA * p;
            aA -= _tmp.InverseInertiaA * Vector2Util.Cross(ref rA, ref p);
            cB += _tmp.InverseMassB * p;
            aB += _tmp.InverseInertiaB * Vector2Util.Cross(ref rB, ref p);

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return System.Math.Abs(c) < Settings.LinearSlop;
        }

        #endregion
    }
}