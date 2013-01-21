using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Messages;
using Engine.ComponentSystem.Physics.Math;
using Engine.ComponentSystem.Physics.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Physics.Components
{
    /// <summary>
    ///     A fixture is used to define the actual shape of a body. Each body can have multiple fixtures attached to it,
    ///     where each fixture has an implementation of its own shape (e.g. circle, polygon...) Fixtures only track their
    ///     position relative to the body they belong to.
    /// </summary>
    public abstract class Fixture : Component, IIndexable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Types

        /// <summary>Existing fixture types.</summary>
        public enum FixtureType
        {
            /// <summary>Circle fixture.</summary>
            Circle,

            /// <summary>Edge fixture.</summary>
            Edge,

            /// <summary>Polygon fixture.</summary>
            Polygon
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the component is enabled or not. Disabled components will not be handled in the component's system's
        /// <c>Update()</c> method.
        /// </summary>
        public override bool Enabled
        {
            get { return Body.Enabled; }
            set { /* silently ignore */ }
        }

        /// <summary>The index group mask determining which indexes the component will be tracked by.</summary>
        public ulong IndexGroupsMask
        {
            get { return _indexGroupsMask; }
            set
            {
                if (value == _indexGroupsMask)
                {
                    return;
                }

                var oldMask = _indexGroupsMask;
                _indexGroupsMask = value;

                if (Manager == null)
                {
                    return;
                }

                if (Enabled)
                {
                    IndexGroupsChanged message;
                    message.Component = this;
                    message.AddedIndexGroups = value & ~oldMask;
                    message.RemovedIndexGroups = oldMask & ~value;
                    Manager.SendMessage(message);
                }
            }
        }

        /// <summary>Gets the body this fixture is attached to.</summary>
        public Body Body
        {
            get { return Manager.GetComponent(Entity, Body.TypeId) as Body; }
        }

        /// <summary>
        ///     This bit mask representing the collision groups this component is part of. Components sharing at least one
        ///     group will not be tested against each other.
        /// </summary>
        public uint CollisionGroups
        {
            get { return CollisionGroupsInternal; }
            set
            {
                // Skip if nothing changed.
                if (CollisionGroupsInternal == value)
                {
                    return;
                }

                CollisionGroupsInternal = value;

                // Mark this fixture as changed to look for new contacts.
                Simulation.Refilter(this);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is sensor or not (solid or not). Changing this value
        ///     will wake up the body.
        /// </summary>
        public bool IsSensor
        {
            get { return IsSensorInternal; }
            set
            {
                if (IsSensorInternal != value)
                {
                    IsSensorInternal = value;
                    Body.IsAwake = true;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the density of this fixture. This will not automatically adjust the mass of the body. You must call
        ///     <see cref="Components.Body.ResetMassData"/>
        ///     to update the body's mass.
        /// </summary>
        public float Density
        {
            get { return _density; }
            set
            {
                System.Diagnostics.Debug.Assert(value >= 0);

                if (Simulation.IsLocked)
                {
                    throw new InvalidOperationException("Cannot change density during update.");
                }

                _density = value;
            }
        }

        /// <summary>
        ///     Gets or sets the coefficient of friction of this fixture. Note that this will not change the friction of
        ///     existing contacts.
        /// </summary>
        public float Friction
        {
            get { return _friction; }
            set
            {
                if (Simulation.IsLocked)
                {
                    throw new InvalidOperationException("Cannot change friction during update.");
                }

                _friction = value;
            }
        }

        /// <summary>
        ///     Gets or sets the coefficient of restitution of this fixture. Note that this will not change the restitution of
        ///     existing contacts.
        /// </summary>
        public float Restitution
        {
            get { return _restitution; }
            set
            {
                if (Simulation.IsLocked)
                {
                    throw new InvalidOperationException("Cannot change restitution during update.");
                }

                _restitution = value;
            }
        }

        /// <summary>Gets the physics system driving the simulation we're part of.</summary>
        private PhysicsSystem Simulation
        {
            get { return Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem; }
        }

        #endregion

        #region Fields

        /// <summary>The type of this fixture, for fast evaluation method lookup in contact creation.</summary>
        [PacketizerIgnore]
        internal readonly FixtureType Type;

        /// <summary>
        ///     A bounding radius. For circles this is the actual radius, for edges and polygons it's a buffer area around the
        ///     actual shape, for improved stability (constant value).
        /// </summary>
        internal float Radius;

        /// <summary>The collision groups the body is in, as a bit mask.</summary>
        internal uint CollisionGroupsInternal;

        /// <summary>Whether this fixture is a sensor or not (solver won't try to handle collisions).</summary>
        internal bool IsSensorInternal;

        /// <summary>The density of the shape this fixture represents.</summary>
        private float _density;

        /// <summary>The surface friction of this fixture.</summary>
        private float _friction = 0.2f;

        /// <summary>The restitution (bounciness) of this fixture.</summary>
        private float _restitution;

        /// <summary>The bit mask of our index group.</summary>
        private ulong _indexGroupsMask = PhysicsSystem.IndexGroupMask;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="Fixture"/> class.
        /// </summary>
        /// <param name="type">The type of this fixture.</param>
        protected Fixture(FixtureType type)
        {
            Type = type;
        }

        /// <summary>
        ///     Initialize the component with the specified values. Note that this does not automatically change the mass of the
        ///     body. You have to call
        ///     <see cref="Components.Body.ResetMassData"/> to update it.
        /// </summary>
        /// <param name="density">The density.</param>
        /// <param name="friction">The friction.</param>
        /// <param name="restitution">The restitution.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> this fixture is marked as a sensor.
        /// </param>
        /// <param name="collisionGroups">The collision groups for this fixture.</param>
        /// <returns></returns>
        public virtual Fixture Initialize(
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            CollisionGroupsInternal = collisionGroups;
            IsSensorInternal = isSensor;
            _density = density;
            _friction = friction;
            _restitution = restitution;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Radius = 0;
            CollisionGroupsInternal = 0;
            IsSensorInternal = false;
            _density = 0;
            _friction = 0.2f;
            _restitution = 0;
        }

        #endregion

        #region Methods

        /// <summary>Test the specified point for containment in this fixture.</summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>Whether the point is contained in this fixture or not.</returns>
        public abstract bool TestPoint(LocalPoint localPoint);

        /// <summary>
        ///     Get the mass data for this fixture. The mass data is based on the density and the shape. The rotational
        ///     inertia is about the shape's origin. This operation may be expensive.
        /// </summary>
        /// <param name="mass">The mass of this fixture.</param>
        /// <param name="center">The center of mass relative to the local origin.</param>
        /// <param name="inertia">The inertia about the local origin.</param>
        internal abstract void GetMassData(out float mass, out LocalPoint center, out float inertia);
        
        /// <summary>Computes the global bounds of this fixture based on the current body transform.</summary>
        /// <returns>The global bounds of this fixture.</returns>
        public WorldBounds ComputeWorldBounds()
        {
            return ComputeBounds(Body.Transform);
        }

        /// <summary>Computes the global bounds of this fixture given the specified body transform.</summary>
        /// <param name="transform">The world transform of the body.</param>
        /// <returns>The global bounds of this fixture.</returns>
        internal abstract WorldBounds ComputeBounds(WorldTransform transform);

        /// <summary>Updates this fixtures position in the index structure used for the broad phase.</summary>
        /// <param name="transform1">The previous world transform of the body.</param>
        /// <param name="transform2">The new world transform of the body.</param>
        internal void Synchronize(WorldTransform transform1, WorldTransform transform2)
        {
            // Compute an AABB that covers the swept shape (may miss some rotation effect).
            var bounds = WorldBounds.Union(ComputeBounds(transform1), ComputeBounds(transform2));

            // Compute the displacement of the shape.
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var delta = (Vector2) (transform2.Translation - transform1.Translation);
// ReSharper restore RedundantCast

            // Update the index.
            System.Diagnostics.Debug.Assert(Enabled);
            IndexBoundsChanged message;
            message.Component = this;
            message.Bounds = bounds;
            message.Delta = delta;
            Manager.SendMessage(message);
        }

        /// <summary>Updates this fixtures position in the index structure used for the broad phase.</summary>
        internal void Synchronize()
        {
            if (Enabled)
            {
                IndexBoundsChanged message;
                message.Component = this;
                message.Bounds = ComputeBounds(Body.Transform);
                message.Delta = Vector2.Zero;
                Manager.SendMessage(message);
            }
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            System.Diagnostics.Debug.Assert(!Simulation.IsLocked);

            return base.Packetize(packet);
        }

        #endregion
    }
}