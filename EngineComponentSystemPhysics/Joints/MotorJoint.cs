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
    /// <summary>
    ///     A motor joint is used to control the relative motion between two bodies. A typical usage is to control the
    ///     movement of a dynamic body with respect to the ground.
    /// </summary>
    public sealed class MotorJoint : Joint
    {
        #region Properties

        /// <summary>Get the anchor point on the first body in world coordinates.</summary>
        public override WorldPoint AnchorA
        {
            get { return BodyA.Position; }
        }

        /// <summary>Get the anchor point on the second body in world coordinates.</summary>
        public override WorldPoint AnchorB
        {
            get { return BodyB.Position; }
        }

        /// <summary>Set/Get the target linear offset, in frame A, in meters.</summary>
        public Vector2 LinearOffset
        {
            get { return _linearOffset; }
            set
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (value.X != _linearOffset.X || value.Y != _linearOffset.Y)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _linearOffset = value;
                }
            }
        }

        /// <summary>Set/Get the target angular offset, in radians.</summary>
        public float AngularOffset
        {
            get { return _angularOffset; }
            set
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (value != _angularOffset)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    BodyA.IsAwake = true;
                    BodyB.IsAwake = true;
                    _angularOffset = value;
                }
            }
        }

        /// <summary>Set/Get the maximum friction force in N.</summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set { _maxForce = value; }
        }

        /// <summary>Set/Get the maximum friction torque in N*m.</summary>
        public float MaxTorque
        {
            get { return _maxTorque; }
            set { _maxTorque = value; }
        }

        #endregion

        #region Fields

        private Vector2 _linearOffset;

        private float _angularOffset;

        private Vector2 _linearImpulse;

        private float _angularImpulse;

        private float _maxForce;

        private float _maxTorque;

        private float _correctionFactor;

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public Vector2 RotA;

            public Vector2 RotB;

            public LocalPoint LocalCenterA;

            public LocalPoint LocalCenterB;

            public Vector2 LinearError;

            public float AngularError;

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
        ///     Initializes a new instance of the <see cref="MotorJoint"/> class.
        /// </summary>
        /// <remarks>
        ///     Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public MotorJoint() : base(JointType.Motor) {}

        /// <summary>Initializes this joint with the specified parameters.</summary>
        internal void Initialize(float maxForce, float maxTorque, float correctionFactor)
        {
            _linearOffset = BodyA.GetLocalPoint(BodyB.Position);
            _angularOffset = BodyB.Angle - BodyA.Angle;

            _maxForce = maxForce;
            _maxTorque = maxTorque;
            _correctionFactor = correctionFactor;

            _linearImpulse = Vector2.Zero;
            _angularImpulse = 0.0f;
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

            // Compute the effective mass matrix.
            _tmp.RotA = qA * -_tmp.LocalCenterA;
            _tmp.RotB = qB * -_tmp.LocalCenterB;

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

// ReSharper disable RedundantCast Necessary for FarPhysics.
            _tmp.LinearError = (Vector2) (cB - cA) + (_tmp.RotB - _tmp.RotA) - qA * _linearOffset;
// ReSharper restore RedundantCast
            _tmp.AngularError = aB - aA - _angularOffset;

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
            var invH = step.InverseDeltaT;

            // Solve angular friction
            {
                var cdot = wB - wA + invH * _correctionFactor * _tmp.AngularError;
                var impulse = -_tmp.AngularMass * cdot;

                var oldImpulse = _angularImpulse;
                var maxImpulse = h * _maxTorque;
                _angularImpulse = MathHelper.Clamp(_angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve linear friction
            {
                var cdot = vB + Vector2Util.Cross(wB, ref _tmp.RotB) - vA - Vector2Util.Cross(wA, ref _tmp.RotA) +
                           invH * _correctionFactor * _tmp.LinearError;

                var impulse = -(_tmp.LinearMass * cdot);
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