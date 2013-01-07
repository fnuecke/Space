using System.Globalization;
using Engine.ComponentSystem;
using Engine.Physics.Components;
using Engine.Physics.Math;
using Engine.Serialization;
using Engine.Util;
using Engine.XnaExtensions;
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
    /// A gear joint is used to connect two joints together. Either joint
    /// can be a revolute or prismatic joint. You specify a gear ratio
    /// to bind the motions together:
    /// coordinate1 + ratio * coordinate2 = constant
    /// The ratio can be negative or positive. If one joint is a revolute joint
    /// and the other joint is a prismatic joint, then the ratio will have units
    /// of length or units of 1/length.
    /// @warning You have to manually destroy the gear joint if joint1 or joint2
    /// is destroyed.
    /// </summary>
    public sealed class GearJoint : Joint
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
        /// Gets or sets the gear ratio.
        /// </summary>
        public float Ratio
        {
            get { return _ratio; }
            set { _ratio = value; }
        }

        /// <summary>
        /// Body A is connected to body C.
        /// </summary>
        private Body BodyC
        {
            get { return _bodyIdC != 0 ? Manager.GetComponentById(_bodyIdC) as Body : null; }
        }

        /// <summary>
        /// Body B is connected to body D.
        /// </summary>
        private Body BodyD
        {
            get { return _bodyIdD != 0 ? Manager.GetComponentById(_bodyIdD) as Body : null; }
        }

        #endregion

        #region Fields

        private JointType _typeA;

        private JointType _typeB;

        private int _bodyIdC;

        private int _bodyIdD;

        private LocalPoint _localAnchorA;

        private LocalPoint _localAnchorB;

        private LocalPoint _localAnchorC;

        private LocalPoint _localAnchorD;

        private Vector2 _localAxisC;

        private Vector2 _localAxisD;

        private float _referenceAngleA;

        private float _referenceAngleB;

        private float _constant;

        private float _ratio;

        private float _impulse;

        private struct SolverTemp
        {
            public int IndexA, IndexB, IndexC, IndexD;

            public LocalPoint LcA, LcB, LcC, LcD;

            public float InverseMassA, InverseMassB, InverseMassC, InverseMassD;

            public float InverseInertiaA, InverseInertiaB, InverseInertiaC, InverseInertiaD;

            public Vector2 JvAC, JvBD;

            public float JwA, JwB, JwC, JwD;

            public float Mass;
        }

        [CopyIgnore, PacketizerIgnore]
        private SolverTemp _tmp;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="GearJoint"/> class.
        /// </summary>
        /// <remarks>
        /// Use the factory methods in <see cref="JointFactory"/> to create joints.
        /// </remarks>
        public GearJoint() : base(JointType.Gear)
        {
        }

        /// <summary>
        /// Initializes this joint with the specified parameters.
        /// </summary>
        internal void Initialize(IManager manager, Joint jointA, Joint jointB, float ratio)
        {
            Manager = manager;

            _typeA = jointA.Type;
            _typeB = jointB.Type;

            System.Diagnostics.Debug.Assert(_typeA == JointType.Revolute || _typeA == JointType.Prismatic);
            System.Diagnostics.Debug.Assert(_typeB == JointType.Revolute || _typeB == JointType.Prismatic);

            float coordinateA, coordinateB;

            // TODO_ERIN there might be some problem with the joint edges in b2Joint.

            _bodyIdC = jointA.BodyA.Id;
            _bodyIdA = jointA.BodyB.Id;

            // Get geometry of joint1
            var xfA = BodyA.Transform;
            var aA = BodyA.Sweep.Angle;
            var xfC = BodyC.Transform;
            var aC = BodyC.Sweep.Angle;

            if (_typeA == JointType.Revolute)
            {
                var revolute = (RevoluteJoint)jointA;
                _localAnchorC = revolute.LocalAnchorA;
                _localAnchorA = revolute.LocalAnchorB;
                _referenceAngleA = revolute.ReferenceAngle;
                _localAxisC = Vector2.Zero;

                coordinateA = aA - aC - _referenceAngleA;
            }
            else
            {
                var prismatic = (PrismaticJoint)jointA;
                _localAnchorC = prismatic.LocalAnchorA;
                _localAnchorA = prismatic.LocalAnchorB;
                _referenceAngleA = prismatic.ReferenceAngle;
                _localAxisC = prismatic.LocalAxisA;

                var pC = _localAnchorC;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var pA = -xfC.Rotation * ((xfA.Rotation * _localAnchorA) + (Vector2)(xfA.Translation - xfC.Translation));
// ReSharper restore RedundantCast
                coordinateA = Vector2.Dot(pA - pC, _localAxisC);
            }

            _bodyIdD = jointB.BodyA.Id;
            _bodyIdB = jointB.BodyB.Id;

            // Get geometry of joint2
            var xfB = BodyB.Transform;
            var aB = BodyB.Sweep.Angle;
            var xfD = BodyD.Transform;
            var aD = BodyD.Sweep.Angle;

            if (_typeB == JointType.Revolute)
            {
                var revolute = (RevoluteJoint)jointB;
                _localAnchorD = revolute.LocalAnchorA;
                _localAnchorB = revolute.LocalAnchorB;
                _referenceAngleB = revolute.ReferenceAngle;
                _localAxisD = Vector2.Zero;

                coordinateB = aB - aD - _referenceAngleB;
            }
            else
            {
                var prismatic = (PrismaticJoint)jointB;
                _localAnchorD = prismatic.LocalAnchorA;
                _localAnchorB = prismatic.LocalAnchorB;
                _referenceAngleB = prismatic.ReferenceAngle;
                _localAxisD = prismatic.LocalAxisA;

                var pD = _localAnchorD;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var pB = -xfD.Rotation * ((xfB.Rotation * _localAnchorB) + (Vector2)(xfB.Translation - xfD.Translation));
// ReSharper restore RedundantCast
                coordinateB = Vector2.Dot(pB - pD, _localAxisD);
            }

            _ratio = ratio;

            _constant = coordinateA + _ratio * coordinateB;
            _impulse = 0.0f;
        }

        #endregion

        #region Logic

        // Gear Joint:
        // C0 = (coordinate1 + ratio * coordinate2)_initial
        // C = (coordinate1 + ratio * coordinate2) - C0 = 0
        // J = [J1 ratio * J2]
        // K = J * invM * JT
        //   = J1 * invM1 * J1T + ratio * ratio * J2 * invM2 * J2T
        //
        // Revolute:
        // coordinate = rotation
        // Cdot = angularVelocity
        // J = [0 0 1]
        // K = J * invM * JT = invI
        //
        // Prismatic:
        // coordinate = dot(p - pg, ug)
        // Cdot = dot(v + cross(w, r), ug)
        // J = [ug cross(r, ug)]
        // K = J * invM * JT = invMass + invI * cross(r, ug)^2

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
            _tmp.IndexC = BodyC.IslandIndex;
            _tmp.IndexD = BodyD.IslandIndex;
            _tmp.LcA = BodyA.Sweep.LocalCenter;
            _tmp.LcB = BodyB.Sweep.LocalCenter;
            _tmp.LcC = BodyC.Sweep.LocalCenter;
            _tmp.LcD = BodyD.Sweep.LocalCenter;
            _tmp.InverseMassA = BodyA.InverseMass;
            _tmp.InverseMassB = BodyB.InverseMass;
            _tmp.InverseMassC = BodyC.InverseMass;
            _tmp.InverseMassD = BodyD.InverseMass;
            _tmp.InverseInertiaA = BodyA.InverseInertia;
            _tmp.InverseInertiaB = BodyB.InverseInertia;
            _tmp.InverseInertiaC = BodyC.InverseInertia;
            _tmp.InverseInertiaD = BodyD.InverseInertia;

            var aA = positions[_tmp.IndexA].Angle;
            var vA = velocities[_tmp.IndexA].LinearVelocity;
            var wA = velocities[_tmp.IndexA].AngularVelocity;

            var aB = positions[_tmp.IndexB].Angle;
            var vB = velocities[_tmp.IndexB].LinearVelocity;
            var wB = velocities[_tmp.IndexB].AngularVelocity;

            var aC = positions[_tmp.IndexC].Angle;
            var vC = velocities[_tmp.IndexC].LinearVelocity;
            var wC = velocities[_tmp.IndexC].AngularVelocity;

            var aD = positions[_tmp.IndexD].Angle;
            var vD = velocities[_tmp.IndexD].LinearVelocity;
            var wD = velocities[_tmp.IndexD].AngularVelocity;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);
            var qC = new Rotation(aC);
            var qD = new Rotation(aD);

            _tmp.Mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                _tmp.JvAC = Vector2.Zero;
                _tmp.JwA = 1.0f;
                _tmp.JwC = 1.0f;
                _tmp.Mass += _tmp.InverseInertiaA + _tmp.InverseInertiaC;
            }
            else
            {
                var u = qC * _localAxisC;
                var rC = qC * (_localAnchorC - _tmp.LcC);
                var rA = qA * (_localAnchorA - _tmp.LcA);
                _tmp.JvAC = u;
                _tmp.JwC = Vector2Util.Cross(rC, u);
                _tmp.JwA = Vector2Util.Cross(rA, u);
                _tmp.Mass += _tmp.InverseMassC + _tmp.InverseMassA + _tmp.InverseInertiaC * _tmp.JwC * _tmp.JwC + _tmp.InverseInertiaA * _tmp.JwA * _tmp.JwA;
            }

            if (_typeB == JointType.Revolute)
            {
                _tmp.JvBD = Vector2.Zero;
                _tmp.JwB = _ratio;
                _tmp.JwD = _ratio;
                _tmp.Mass += _ratio * _ratio * (_tmp.InverseInertiaB + _tmp.InverseInertiaD);
            }
            else
            {
                var u = qD * _localAxisD;
                var rD = qD * (_localAnchorD - _tmp.LcD);
                var rB = qB * (_localAnchorB - _tmp.LcB);
                _tmp.JvBD = _ratio * u;
                _tmp.JwD = _ratio * Vector2Util.Cross(rD, u);
                _tmp.JwB = _ratio * Vector2Util.Cross(rB, u);
                _tmp.Mass += _ratio * _ratio * (_tmp.InverseMassD + _tmp.InverseMassB) + _tmp.InverseInertiaD * _tmp.JwD * _tmp.JwD + _tmp.InverseInertiaB * _tmp.JwB * _tmp.JwB;
            }

            // Compute effective mass.
            _tmp.Mass = _tmp.Mass > 0.0f ? 1.0f / _tmp.Mass : 0.0f;

            vA += (_tmp.InverseMassA * _impulse) * _tmp.JvAC;
            wA += _tmp.InverseInertiaA * _impulse * _tmp.JwA;
            vB += (_tmp.InverseMassB * _impulse) * _tmp.JvBD;
            wB += _tmp.InverseInertiaB * _impulse * _tmp.JwB;
            vC -= (_tmp.InverseMassC * _impulse) * _tmp.JvAC;
            wC -= _tmp.InverseInertiaC * _impulse * _tmp.JwC;
            vD -= (_tmp.InverseMassD * _impulse) * _tmp.JvBD;
            wD -= _tmp.InverseInertiaD * _impulse * _tmp.JwD;

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
            velocities[_tmp.IndexC].LinearVelocity = vC;
            velocities[_tmp.IndexC].AngularVelocity = wC;
            velocities[_tmp.IndexD].LinearVelocity = vD;
            velocities[_tmp.IndexD].AngularVelocity = wD;
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
            var vC = velocities[_tmp.IndexC].LinearVelocity;
            var wC = velocities[_tmp.IndexC].AngularVelocity;
            var vD = velocities[_tmp.IndexD].LinearVelocity;
            var wD = velocities[_tmp.IndexD].AngularVelocity;

            var cdot = Vector2.Dot(_tmp.JvAC, vA - vC) + Vector2.Dot(_tmp.JvBD, vB - vD);
            cdot += (_tmp.JwA * wA - _tmp.JwC * wC) + (_tmp.JwB * wB - _tmp.JwD * wD);

            var impulse = -_tmp.Mass * cdot;
            _impulse += impulse;

            vA += (_tmp.InverseMassA * impulse) * _tmp.JvAC;
            wA += _tmp.InverseInertiaA * impulse * _tmp.JwA;
            vB += (_tmp.InverseMassB * impulse) * _tmp.JvBD;
            wB += _tmp.InverseInertiaB * impulse * _tmp.JwB;
            vC -= (_tmp.InverseMassC * impulse) * _tmp.JvAC;
            wC -= _tmp.InverseInertiaC * impulse * _tmp.JwC;
            vD -= (_tmp.InverseMassD * impulse) * _tmp.JvBD;
            wD -= _tmp.InverseInertiaD * impulse * _tmp.JwD;

            velocities[_tmp.IndexA].LinearVelocity = vA;
            velocities[_tmp.IndexA].AngularVelocity = wA;
            velocities[_tmp.IndexB].LinearVelocity = vB;
            velocities[_tmp.IndexB].AngularVelocity = wB;
            velocities[_tmp.IndexC].LinearVelocity = vC;
            velocities[_tmp.IndexC].AngularVelocity = wC;
            velocities[_tmp.IndexD].LinearVelocity = vD;
            velocities[_tmp.IndexD].AngularVelocity = wD;
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
            var cC = positions[_tmp.IndexC].Point;
            var aC = positions[_tmp.IndexC].Angle;
            var cD = positions[_tmp.IndexD].Point;
            var aD = positions[_tmp.IndexD].Angle;

            var qA = new Rotation(aA);
            var qB = new Rotation(aB);
            var qC = new Rotation(aC);
            var qD = new Rotation(aD);

            const float linearError = 0.0f;

            float coordinateA, coordinateB;

            Vector2 JvAC, JvBD;
            float JwA, JwB, JwC, JwD;
            var mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                JvAC = Vector2.Zero;
                JwA = 1.0f;
                JwC = 1.0f;
                mass += _tmp.InverseInertiaA + _tmp.InverseInertiaC;

                coordinateA = aA - aC - _referenceAngleA;
            }
            else
            {
                var u = qC * _localAxisC;
                var rC = qC * (_localAnchorC - _tmp.LcC);
                var rA = qA * (_localAnchorA - _tmp.LcA);
                JvAC = u;
                JwC = Vector2Util.Cross(rC, u);
                JwA = Vector2Util.Cross(rA, u);
                mass += _tmp.InverseMassC + _tmp.InverseMassA + _tmp.InverseInertiaC * JwC * JwC + _tmp.InverseInertiaA * JwA * JwA;

                var pC = _localAnchorC - _tmp.LcC;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var pA = -qC * (rA + (Vector2)(cA - cC));
// ReSharper restore RedundantCast
                coordinateA = Vector2.Dot(pA - pC, _localAxisC);
            }

            if (_typeB == JointType.Revolute)
            {
                JvBD = Vector2.Zero;
                JwB = _ratio;
                JwD = _ratio;
                mass += _ratio * _ratio * (_tmp.InverseInertiaB + _tmp.InverseInertiaD);

                coordinateB = aB - aD - _referenceAngleB;
            }
            else
            {
                var u = qD * _localAxisD;
                var rD = qD * (_localAnchorD - _tmp.LcD);
                var rB = qB * (_localAnchorB - _tmp.LcB);
                JvBD = _ratio * u;
                JwD = _ratio * Vector2Util.Cross(rD, u);
                JwB = _ratio * Vector2Util.Cross(rB, u);
                mass += _ratio * _ratio * (_tmp.InverseMassD + _tmp.InverseMassB) + _tmp.InverseInertiaD * JwD * JwD + _tmp.InverseInertiaB * JwB * JwB;

                var pD = _localAnchorD - _tmp.LcD;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                var pB = -qD * (rB + (Vector2)(cB - cD));
// ReSharper restore RedundantCast
                coordinateB = Vector2.Dot(pB - pD, _localAxisD);
            }

            var c = (coordinateA + _ratio * coordinateB) - _constant;

            var impulse = 0.0f;
            if (mass > 0.0f)
            {
                impulse = -c / mass;
            }

            cA += _tmp.InverseMassA * impulse * JvAC;
            aA += _tmp.InverseInertiaA * impulse * JwA;
            cB += _tmp.InverseMassB * impulse * JvBD;
            aB += _tmp.InverseInertiaB * impulse * JwB;
            cC -= _tmp.InverseMassC * impulse * JvAC;
            aC -= _tmp.InverseInertiaC * impulse * JwC;
            cD -= _tmp.InverseMassD * impulse * JvBD;
            aD -= _tmp.InverseInertiaD * impulse * JwD;

            positions[_tmp.IndexA].Point = cA;
            positions[_tmp.IndexA].Angle = aA;
            positions[_tmp.IndexB].Point = cB;
            positions[_tmp.IndexB].Angle = aB;
            positions[_tmp.IndexC].Point = cC;
            positions[_tmp.IndexC].Angle = aC;
            positions[_tmp.IndexD].Point = cD;
            positions[_tmp.IndexD].Angle = aD;

            // TODO_ERIN not implemented
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
            return base.ToString() +
                ", TypeA=" + _typeA +
                ", TypeB=" + _typeB +
                ", BodyC=" + _bodyIdC +
                ", BodyD=" + _bodyIdD +
                ", LocalAnchorA=" + _localAnchorA.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorA.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorB=" + _localAnchorB.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorB.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorC=" + _localAnchorC.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorC.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAnchorD=" + _localAnchorD.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAnchorD.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAxisC=" + _localAxisC.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAxisC.Y.ToString(CultureInfo.InvariantCulture) +
                ", LocalAxisD=" + _localAxisD.X.ToString(CultureInfo.InvariantCulture) + ":" + _localAxisD.Y.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceAngleA=" + _referenceAngleA.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceAngleB=" + _referenceAngleB.ToString(CultureInfo.InvariantCulture) +
                ", Constant=" + _constant.ToString(CultureInfo.InvariantCulture) +
                ", Ratio=" + _ratio.ToString(CultureInfo.InvariantCulture) +
                ", Impulse=" + _impulse.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
