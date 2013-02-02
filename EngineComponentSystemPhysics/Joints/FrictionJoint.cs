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
    public sealed class FrictionJoint : Joint
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

        /// <summary>Get the anchor point on the first body in local coordinates.</summary>
        public LocalPoint LocalAnchorA
        {
            get { return _localAnchorA; }
        }

        /// <summary>Get the anchor point on the second body in local coordinates.</summary>
        public LocalPoint LocalAnchorB
        {
            get { return _localAnchorB; }
        }

        /// <summary>Gets or sets the maximum friction force in N.</summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set { _maxForce = value; }
        }

        /// <summary>Gets or sets the maximum friction torque in N*m.</summary>
        public float MaxTorque
        {
            get { return _maxTorque; }
            set { _maxTorque = value; }
        }

        #endregion

        #region Fields

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _maxForce;

        private float _maxTorque;

        private Vector2 _linearImpulse; // kept across updates for warm starting

        private float _angularImpulse; // kept across updates for warm starting

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public Vector2 RotA;

            public Vector2 RotB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public float InverseMassA;

            public float InverseMassB;

            public float InverseInertiaA;

            public float InverseInertiaB;

            public Matrix22 LinearMass;

            public float AngularMass;
        }

        [CopyIgnore, PacketizeIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrictionJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public FrictionJoint() : base(JointType.Friction) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(WorldPoint anchorA, WorldPoint anchorB)
        {
            _linearImpulse = Vector2.Zero;
            _angularImpulse = 0;
        }

        #endregion

        #region Logic

        // Point-to-point constraint
        // cDot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
        // cDot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2

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

            var aA = positions[_tmp.IndexA].Angle;
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;

            var aB = positions[_tmp.IndexB].Angle;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            // Compute the effective mass matrix.
            _tmp.RotA = qA * (_localAnchorA - _tmp.LocalCenterA);
            _tmp.RotB = qB * (_localAnchorB - _tmp.LocalCenterB);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            Matrix22 k;
            k.Column1.X = mA + mB + iA * _tmp.RotA.Y * _tmp.RotA.Y + iB * _tmp.RotB.Y * _tmp.RotB.Y;
            k.Column1.Y = -iA * _tmp.RotA.X * _tmp.RotA.Y - iB * _tmp.RotB.X * _tmp.RotB.Y;
            k.Column2.X = k.Column1.Y;
            k.Column2.Y = mA + mB + iA * _tmp.RotA.X * _tmp.RotA.X + iB * _tmp.RotB.X * _tmp.RotB.X;

            _tmp.LinearMass = k.GetInverse();

            _tmp.AngularMass = iA + iB;
            if (_tmp.AngularMass > 0.0f)
            {
                _tmp.AngularMass = 1.0f / _tmp.AngularMass;
            }

            var p = new Vector2(_linearImpulse.X, _linearImpulse.Y);
            vA -= mA * p;
            wA -= iA * (Vector2Util.Cross(ref _tmp.RotA, ref p) + _angularImpulse);
            vB += mB * p;
            wB += iB * (Vector2Util.Cross(ref _tmp.RotB, ref p) + _angularImpulse);

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

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            var h = step.DeltaT;

            // Solve angular friction
            {
                var cDot = wB - wA;
                var impulse = -_tmp.AngularMass * cDot;

                var oldImpulse = _angularImpulse;
                var maxImpulse = h * _maxTorque;
                _angularImpulse = MathHelper.Clamp(_angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve linear friction
            {
                var cDot = vB + Vector2Util.Cross(wB, ref _tmp.RotB) - vA - Vector2Util.Cross(wA, ref _tmp.RotA);

                var impulse = -(_tmp.LinearMass * cDot);
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
                wA -= iA * Vector2Util.Cross(ref _tmp.RotA, ref impulse);

                vB += mB * impulse;
                wB += iB * Vector2Util.Cross(ref _tmp.RotB, ref impulse);
            }

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
            return true;
        }

        #endregion
    }
}