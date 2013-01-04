using Engine.Physics.Math;
using Engine.Serialization;
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
    public sealed class WeldJoint : Joint
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
        /// Get the reference angle.
        /// </summary>
        public float ReferenceAngle
        {
            get { return _referenceAngle; }
        }

        /// <summary>
        /// Set/get frequency in Hz.
        /// </summary>
        public float Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Set/get damping ratio.
        /// </summary>
        public float DampingRatio
        {
            get { return _dampingRatio; }
            set { _dampingRatio = value; }
        }

        #endregion

        #region Fields

        private float _frequency;

        private float _dampingRatio;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _referenceAngle;

        private Vector3 _impulse;

        private struct SolverTemp
        {
            public float Bias;

            public float Gamma;

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

            public Matrix33 Mass;
        }

        [PacketizerIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="WeldJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public WeldJoint() : base(JointType.Weld)
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
        // C = p2 - p1
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
        // C = angle2 - angle1 - referenceAngle
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

            Matrix33 k;
            k.Column1.X = mA + mB + _tmp.RotA.Y * _tmp.RotA.Y * iA + _tmp.RotB.Y * _tmp.RotB.Y * iB;
            k.Column2.X = -_tmp.RotA.Y * _tmp.RotA.X * iA - _tmp.RotB.Y * _tmp.RotB.X * iB;
            k.Column3.X = -_tmp.RotA.Y * iA - _tmp.RotB.Y * iB;
            k.Column1.Y = k.Column2.X;
            k.Column2.Y = mA + mB + _tmp.RotA.X * _tmp.RotA.X * iA + _tmp.RotB.X * _tmp.RotB.X * iB;
            k.Column3.Y = _tmp.RotA.X * iA + _tmp.RotB.X * iB;
            k.Column1.Z = k.Column3.X;
            k.Column2.Z = k.Column3.Y;
            k.Column3.Z = iA + iB;

            if (_frequency > 0.0f)
            {
                k.GetInverse22(out _tmp.Mass);

                var invM = iA + iB;
                var m = invM > 0.0f ? 1.0f / invM : 0.0f;

                var c = aB - aA - _referenceAngle;

                // Frequency
                var omega = 2.0f * MathHelper.Pi * _frequency;

                // Damping coefficient
                var d = 2.0f * m * _dampingRatio * omega;

                // Spring stiffness
                var s = m * omega * omega;

                // magic formulas
                var h = step.DeltaT;
                _tmp.Gamma = h * (d + h * s);
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (_tmp.Gamma != 0.0f)
                {
                    _tmp.Gamma = 1.0f / _tmp.Gamma;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
                _tmp.Bias = c * h * s * _tmp.Gamma;

                invM += _tmp.Gamma;
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (invM != 0.0f)
                {
                    _tmp.Mass.Column3.Z = 1.0f / invM;
                }
                else
                {
                    _tmp.Mass.Column3.Z = 0.0f;
                }
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
            else
            {
                k.GetSymInverse33(out _tmp.Mass);
                _tmp.Gamma = 0.0f;
                _tmp.Bias = 0.0f;
            }

            if (step.IsWarmStarting)
            {
                var p = new Vector2(_impulse.X, _impulse.Y);

                vA -= mA * p;
                wA -= iA * (Vector2Util.Cross(_tmp.RotA, p) + _impulse.Z);

                vB += mB * p;
                wB += iB * (Vector2Util.Cross(_tmp.RotB, p) + _impulse.Z);
            }
            else
            {
                _impulse = Vector3.Zero;
            }

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
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
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            if (_frequency > 0.0f)
            {
                var cdot2 = wB - wA;

                var impulse2 = -_tmp.Mass.Column3.Z * (cdot2 + _tmp.Bias + _tmp.Gamma * _impulse.Z);
                _impulse.Z += impulse2;

                wA -= iA * impulse2;
                wB += iB * impulse2;

                var cdot1 = vB + Vector2Util.Cross(wB, _tmp.RotB) - vA - Vector2Util.Cross(wA, _tmp.RotA);

                var impulse1 = -(_tmp.Mass * cdot1);
                _impulse.X += impulse1.X;
                _impulse.Y += impulse1.Y;

                var p = impulse1;

                vA -= mA * p;
                wA -= iA * Vector2Util.Cross(_tmp.RotA, p);

                vB += mB * p;
                wB += iB * Vector2Util.Cross(_tmp.RotB, p);
            }
            else
            {
                var cdot1 = vB + Vector2Util.Cross(wB, _tmp.RotB) - vA - Vector2Util.Cross(wA, _tmp.RotA);
                var cdot2 = wB - wA;
                var cdot = new Vector3(cdot1.X, cdot1.Y, cdot2);

                var impulse = -(_tmp.Mass * cdot);
                _impulse += impulse;

                var p = new Vector2(impulse.X, impulse.Y);

                vA -= mA * p;
                wA -= iA * (Vector2Util.Cross(_tmp.RotA, p) + impulse.Z);

                vB += mB * p;
                wB += iB * (Vector2Util.Cross(_tmp.RotB, p) + impulse.Z);
            }

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
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
            var cA = positions[_tmp.IndexA].Point;
            var aA = positions[_tmp.IndexA].Angle;
            var cB = positions[_tmp.IndexB].Point;
            var aB = positions[_tmp.IndexB].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);

            var mA = _tmp.InverseMassA;
            var mB = _tmp.InverseMassB;
            var iA = _tmp.InverseInertiaA;
            var iB = _tmp.InverseInertiaB;

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);

            float positionError, angularError;

            Matrix33 k;
            k.Column1.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
            k.Column2.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
            k.Column3.X = -rA.Y * iA - rB.Y * iB;
            k.Column1.Y = k.Column2.X;
            k.Column2.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
            k.Column3.Y = rA.X * iA + rB.X * iB;
            k.Column1.Z = k.Column3.X;
            k.Column2.Z = k.Column3.Y;
            k.Column3.Z = iA + iB;

            if (_frequency > 0.0f)
            {
                var c1 = (Vector2)(cB - cA) + (rB - rA);

                positionError = c1.Length();
                angularError = 0.0f;

                var p = -k.Solve22(c1);

                cA -= mA * p;
                aA -= iA * Vector2Util.Cross(rA, p);

                cB += mB * p;
                aB += iB * Vector2Util.Cross(rB, p);
            }
            else
            {
                var c1 = (Vector2)(cB - cA) + (rB - rA);
                var c2 = aB - aA - _referenceAngle;

                positionError = c1.Length();
                angularError = System.Math.Abs(c2);

                var c = new Vector3(c1.X, c1.Y, c2);

                var impulse = -k.Solve33(c);
                var p = new Vector2(impulse.X, impulse.Y);

                cA -= mA * p;
                aA -= iA * (Vector2Util.Cross(rA, p) + impulse.Z);

                cB += mB * p;
                aB += iB * (Vector2Util.Cross(rB, p) + impulse.Z);
            }

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return positionError <= Settings.LinearSlop && angularError <= Settings.AngularSlop;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
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
            return base.ToString();
        }

        #endregion
    }
}
