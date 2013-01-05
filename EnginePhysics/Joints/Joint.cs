using System;
using Engine.ComponentSystem;
using Engine.Physics.Components;
using Engine.Physics.Systems;
using Engine.Serialization;
using Engine.Util;

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
    /// attached body. Joints are not components, because they do not belong
    /// to one entity alone in all cases. Often they will belong to two,
    /// that being the two entities (bodies) they are attached to.
    /// </summary>
    public abstract class Joint : ICopyable<Joint>, IPacketizable, IHashable
    {
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

        /// <summary>
        /// Limit states for joints that have limits.
        /// </summary>
        protected enum LimitState
        {
            Inactive,
            AtLower,
            AtUpper,
            Equal
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get the first body this joint is attached to.
        /// </summary>
        public Body BodyA
        {
            get { return _bodyIdA != 0 ? Manager.GetComponentById(_bodyIdA) as Body : null; }
        }

        /// <summary>
        /// Get the second body this joint is attached to.
        /// </summary>
        public Body BodyB
        {
            get { return _bodyIdB != 0 ? Manager.GetComponentById(_bodyIdB) as Body : null; }
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

        /// <summary>
        /// The type of this joint.
        /// </summary>
        [PacketizerIgnore]
        internal readonly JointType Type;

        /// <summary>
        /// The manager of the component system the bodies of this joint live in.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        internal IManager Manager;

        /// <summary>
        /// Used for the global doubly linked list of joints.
        /// </summary>
        internal int Next, Previous;

        /// <summary>
        /// The IDs of the two bodies this joint is attached to.
        /// </summary>
        protected int _bodyIdA, _bodyIdB;

        /// <summary>
        /// Set this flag to true if the attached bodies should collide.
        /// </summary>
        internal bool CollideConnected = true;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Joint"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        protected Joint(JointType type)
        {
            Type = type;
        }

        /// <summary>
        /// Initializes the joint to the specified properties.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body to attach to.</param>
        /// <param name="bodyB">The second body to attach to.</param>
        /// <param name="collideConnected">if set to <c>true</c> [collide connected].</param>
        internal void Initialize(IManager manager, Body bodyA, Body bodyB, bool collideConnected)
        {
            Manager = manager;
            _bodyIdA = bodyA != null ? bodyA.Id : 0;
            _bodyIdB = bodyB != null ? bodyB.Id : 0;
            CollideConnected = collideConnected;
        }

        public void Destroy()
        {
            if (Manager == null)
            {
                throw new InvalidOperationException("Joint was already removed.");
            }

            var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
            System.Diagnostics.Debug.Assert(physics != null);
            physics.DestroyJoint(this);

            Manager = null;
            _bodyIdA = 0;
            _bodyIdB = 0;
            CollideConnected = true;
        }

        #endregion

        #region Logic

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

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher
                .Put(Next)
                .Put(Previous)
                .Put(_bodyIdA)
                .Put(_bodyIdB)
                .Put(CollideConnected);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public virtual Joint NewInstance()
        {
            var copy = (Joint)MemberwiseClone();

            copy.Manager = null;

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual void CopyInto(Joint into)
        {
            Copyable.CopyInto(this, into);
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
            return GetType().Name + ": JointType=" + Type +
                ", Next=" + Next +
                ", Previous=" + Previous +
                ", BodyA=" + _bodyIdA +
                ", BodyB=" + _bodyIdB +
                ", CollideConnected=" + CollideConnected;
        }

        #endregion
    }

    /// <summary>
    /// Represents a connection between up to two entities a joint is attached to.
    /// If the joint is only attached to one real entity, the other end is usually
    /// attached to the "world", and <see cref="Other"/> will be zero.
    /// </summary>
    internal sealed class JointEdge : ICopyable<JointEdge>, IPacketizable, IHashable
    {
        #region Fields

        /// <summary>
        /// The index of the actual joint.
        /// </summary>
        public int Joint;

        /// <summary>
        /// The id of the other entity this edge's joint is attached to.
        /// </summary>
        public int Other;

        /// <summary>
        /// The index of the previous joint edge, for the entity this
        /// edge belongs to.
        /// </summary>
        public int Previous;

        /// <summary>
        /// The index of the next joint edge, for the entity this
        /// edge belongs to.
        /// </summary>
        public int Next;

        #endregion

        #region Serialization

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher
                .Put(Joint)
                .Put(Other)
                .Put(Previous)
                .Put(Next);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public JointEdge NewInstance()
        {
            return new JointEdge();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(JointEdge into)
        {
            Copyable.CopyInto(this, into);
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
            return "JointEdge: Joint=" + Joint +
                ", Other=" + Other +
                ", Previous=" + Previous +
                ", Next=" + Next;
        }

        #endregion
    }
}
