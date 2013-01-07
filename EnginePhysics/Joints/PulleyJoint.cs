using System;
using Engine.Physics.Math;
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

namespace Engine.Physics.Joints
{
    /// <summary>
    /// The pulley joint is connected to two bodies and two fixed ground points.
    /// The pulley supports a ratio such that:
    /// length1 + ratio * length2 &lt;= constant
    /// Thus, the force transmitted is scaled by the ratio.
    /// Warning: the pulley joint can get a bit squirrelly by itself. They often
    /// work better when combined with prismatic joints. You should also cover the
    /// the anchor points with static shapes to prevent one side from going to
    /// zero length.
    /// </summary>
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

        /// <summary>
        /// Get the first ground anchor.
        /// </summary>
        public WorldPoint GroundAnchorA
        {
            get { return _groundAnchorA; }
        }

        /// <summary>
        /// Get the second ground anchor.
        /// </summary>
        public WorldPoint GroundAnchorB
        {
            get { return _groundAnchorB; }
        }

        /// <summary>
        /// Get the original length of the segment attached to the first body.
        /// </summary>
        public float LengthA
        {
            get { return _lengthA; }
        }

        /// <summary>
        /// Get the original length of the segment attached to the second body.
        /// </summary>
        public float LengthB
        {
            get { return _lengthB; }
        }

        /// <summary>
        /// Get the pulley ratio.
        /// </summary>
        public float Ratio
        {
            get { return _ratio; }
        }

        /// <summary>
        /// Get the current length of the segment attached to the first body.
        /// </summary>
        public float CurrentLengthA
        {
            get { return WorldPoint.Distance(_groundAnchorA, BodyA.GetWorldPoint(_localAnchorA)); }
        }

        /// <summary>
        /// Get the current length of the segment attached to the second body.
        /// </summary>
        public float CurrentLengthB
        {
            get { return WorldPoint.Distance(_groundAnchorB, BodyB.GetWorldPoint(_localAnchorB)); }
        }

        #endregion

        #region Fields

        private WorldPoint _groundAnchorA;

        private WorldPoint _groundAnchorB;

        private float _lengthA;

        private float _lengthB;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private float _constant;

        private float _ratio;

        private float _impulse;

        private struct SolverTemp
        {
            public int IndexA;

            public int IndexB;

            public Vector2 AxisA;

            public Vector2 AxisB;

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

        [CopyIgnore, PacketizerIgnore]
        private SolverTemp _tmp;

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
        internal void Initialize(WorldPoint groundAnchorA, WorldPoint groundAnchorB, WorldPoint anchorA, WorldPoint anchorB, float ratio)
        {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (ratio == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                throw new ArgumentException("Ratio must not be zero.", "ratio");
            }

            _groundAnchorA = groundAnchorA;
            _groundAnchorB = groundAnchorB;
            _localAnchorA = BodyA.GetLocalPoint(anchorA);
            _localAnchorB = BodyB.GetLocalPoint(anchorB);
            _lengthA = WorldPoint.Distance(groundAnchorA, anchorA);
            _lengthB = WorldPoint.Distance(groundAnchorB, anchorB);

            _ratio = ratio;

            _constant = _lengthA + _ratio * _lengthB;
            _impulse = 0;
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

            // Get the pulley axes.
// ReSharper disable RedundantCast Necessary for FarPhysics.
            _tmp.AxisA = (Vector2)(cA - _groundAnchorA) + _tmp.RotA;
            _tmp.AxisB = (Vector2)(cB - _groundAnchorB) + _tmp.RotB;
// ReSharper restore RedundantCast

            var lengthA = _tmp.AxisA.Length();
            var lengthB = _tmp.AxisB.Length();

            if (lengthA > 10.0f * Settings.LinearSlop)
            {
                _tmp.AxisA *= 1.0f / lengthA;
            }
            else
            {
                _tmp.AxisA = Vector2.Zero;
            }

            if (lengthB > 10.0f * Settings.LinearSlop)
            {
                _tmp.AxisB *= 1.0f / lengthB;
            }
            else
            {
                _tmp.AxisB = Vector2.Zero;
            }

            // Compute effective mass.
            float ruA = Vector2Util.Cross(_tmp.RotA, _tmp.AxisA);
            float ruB = Vector2Util.Cross(_tmp.RotB, _tmp.AxisB);

            float mA = _tmp.InverseMassA + _tmp.InverseInertiaA * ruA * ruA;
            float mB = _tmp.InverseMassB + _tmp.InverseInertiaB * ruB * ruB;

            _tmp.Mass = mA + _ratio * _ratio * mB;

            if (_tmp.Mass > 0.0f)
            {
                _tmp.Mass = 1.0f / _tmp.Mass;
            }

            var pA = -(_impulse) * _tmp.AxisA;
            var pB = (-_ratio * _impulse) * _tmp.AxisB;
            vA += _tmp.InverseMassA * pA;
            wA += _tmp.InverseInertiaA * Vector2Util.Cross(_tmp.RotA, pA);
            vB += _tmp.InverseMassB * pB;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(_tmp.RotB, pB);

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

            var vpA = vA + Vector2Util.Cross(wA, _tmp.RotA);
            var vpB = vB + Vector2Util.Cross(wB, _tmp.RotB);

            var cdot = -Vector2.Dot(_tmp.AxisA, vpA) - _ratio * Vector2.Dot(_tmp.AxisB, vpB);
            var impulse = -_tmp.Mass * cdot;
            _impulse += impulse;

            var pA = -impulse * _tmp.AxisA;
            var pB = -_ratio * impulse * _tmp.AxisB;
            vA += _tmp.InverseMassA * pA;
            wA += _tmp.InverseInertiaA * Vector2Util.Cross(_tmp.RotA, pA);
            vB += _tmp.InverseMassB * pB;
            wB += _tmp.InverseInertiaB * Vector2Util.Cross(_tmp.RotB, pB);

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

            var rA = qA * (_localAnchorA - _tmp.LocalCenterA);
            var rB = qB * (_localAnchorB - _tmp.LocalCenterB);

            // Get the pulley axes.
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var uA = (Vector2)(cA - _groundAnchorA) + rA;
            var uB = (Vector2)(cB - _groundAnchorB) + rB;
// ReSharper restore RedundantCast

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

            var mA = _tmp.InverseMassA + _tmp.InverseInertiaA * ruA * ruA;
            var mB = _tmp.InverseMassB + _tmp.InverseInertiaB * ruB * ruB;

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

            cA += _tmp.InverseMassA * pA;
            aA += _tmp.InverseInertiaA * Vector2Util.Cross(rA, pA);
            cB += _tmp.InverseMassB * pB;
            aB += _tmp.InverseInertiaB * Vector2Util.Cross(rB, pB);

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;

            return linearError < Settings.LinearSlop;
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
