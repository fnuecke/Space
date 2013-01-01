using Engine.ComponentSystem.Components;
using Engine.Physics.Components;
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
    /// Base class for joints. Joint components (when connecting two bodies,
    /// and not a body and the world) always exist in pairs, with one per
    /// attached body.
    /// </summary>
    public abstract class Joint : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Types

        /// <summary>
        /// The available joint types.
        /// </summary>
        public enum JointType
        {
            None,
            Revolute,
            Prismatic,
            Distance,
            Pulley,
            Mouse,
            Gear,
            Wheel,
            Weld,
            Friction,
            Rope,
            Motor
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get the type of the concrete joint.
        /// </summary>
        public JointType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Get the first body attached to this joint.
        /// </summary>
        public Body BodyA
        {
            get { return _bodyAId != 0 ? Manager.GetComponent(_bodyAId, Body.TypeId) as Body : null; }
        }

        /// <summary>
        /// Get the second body attached to this joint.
        /// </summary>
        public Body BodyB
        {
            get { return _bodyBId != 0 ? Manager.GetComponent(_bodyBId, Body.TypeId) as Body : null; }
        }

        /// <summary>
        /// Get the anchor point on the first body in world coordinates.
        /// </summary>
        public abstract WorldPoint AnchorA { get; }

        /// <summary>
        /// Get the anchor point on the second body in world coordinates.
        /// </summary>
        public abstract WorldPoint AnchorB { get; }

        #endregion

        #region Fields

        private readonly JointType _type;

        /// <summary>
        /// The ID of the joint component on the other body's entity.
        /// </summary>
        private int _otherJointId;

        /// <summary>
        /// The IDs of the two bodies this joint is attached to.
        /// </summary>
        private int _bodyAId, _bodyBId;

        /// <summary>
        /// Set this flag to true if the attached bodies should collide.
        /// </summary>
        internal bool _collideConnected;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Joint"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        protected Joint(JointType type)
        {
            _type = type;
        }

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherJoint = (Joint)other;
            _otherJointId = otherJoint._otherJointId;
            _bodyAId = otherJoint._bodyAId;
            _bodyBId = otherJoint._bodyBId;
            _collideConnected = otherJoint._collideConnected;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _otherJointId = 0;
            _bodyAId = 0;
            _bodyBId = 0;
            _collideConnected = false;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Get the reaction force on bodyB at the joint anchor in Newtons.
        /// </summary>
        /// <param name="inverseDeltaT">The inverse time step.</param>
        /// <returns>The reaction force on the second body.</returns>
        internal abstract Vector2 GetReactionForce(float inverseDeltaT);

        /// <summary>
        /// Get the reaction torque on the second body in N*m.
        /// </summary>
        /// <param name="inverseDeltaT">The inverse time step.</param>
        /// <returns>The reaction torque.</returns>
        internal abstract float GetReactionTorque(float inverseDeltaT);

        /// <summary>
        /// Initializes the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal abstract void InitializeVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities);

        /// <summary>
        /// Solves the velocity constraints.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        internal abstract void SolveVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities);

        /// <summary>
        /// This returns true if the position errors are within tolerance, allowing an
        /// early exit from the iteration loop.
        /// </summary>
        /// <param name="step">The time step for this update.</param>
        /// <param name="positions">The positions of the related bodies.</param>
        /// <param name="velocities">The velocities of the related bodies.</param>
        /// <returns><c>true</c> if the position errors are within tolerance.</returns>
        internal abstract bool SolvePositionConstraints(TimeStep step, Position[] positions, Velocity[] velocities);

        #endregion

        #region Serialization / Hashing

        #endregion

        #region ToString

        #endregion
    }
}
