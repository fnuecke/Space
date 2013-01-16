using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.Physics.Joints;
using Engine.Physics.Math;
using Engine.Physics.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Components
{
    /// <summary>
    ///     A body represents a physical object, tracking some of its global properties such as mass, velocity and world
    ///     transform.
    /// </summary>
    public sealed class Body : Component, ITransform, IVelocity
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

        /// <summary>The available body types.</summary>
        public enum BodyType
        {
            /// <summary>A static body has no mass and no velocity and can only be moved manually, i.e. from outside the simulation.</summary>
            Static,

            /// <summary>A kinematic body has a set velocity, but no mass.</summary>
            Kinematic,

            /// <summary>
            ///     A dynamic body has mass and a velocity determined by other forces inside the simulation. Moving it manually
            ///     will lead to non-physical behavior.
            /// </summary>
            Dynamic
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Whether the component is enabled or not. Disabled components will not be handled in the component's system's
        ///     <c>Update()</c> method.
        /// </summary>
        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (Manager != null)
                {
                    if (Simulation.IsLocked)
                    {
                        throw new System.InvalidOperationException("Cannot change enabled state during update.");
                    }

                    // Skip if nothing changed.
                    if (base.Enabled == value)
                    {
                        return;
                    }

                    base.Enabled = value;

                    if (value)
                    {
                        // Add our fixtures back to the index.
                        foreach (Fixture fixture in Fixtures)
                        {
                            fixture.IndexGroupsMask |= PhysicsSystem.IndexGroupMask;
                        }

                        // Contacts are created in the next update.
                    }
                    else
                    {
                        // Remove all our fixtures from the index.
                        foreach (Fixture fixture in Fixtures)
                        {
                            fixture.IndexGroupsMask &= ~PhysicsSystem.IndexGroupMask;
                        }

                        // Free any contacts we're involved in.
                        Simulation.RemoveContacts(this);
                    }
                }
                else
                {
                    base.Enabled = value;
                }
            }
        }

        /// <summary>Gets the type of this body.</summary>
        public BodyType Type
        {
            get { return TypeInternal; }
            set
            {
                if (Simulation.IsLocked)
                {
                    throw new System.InvalidOperationException("Cannot change type during update.");
                }

                // Skip if nothing changed.
                if (TypeInternal == value)
                {
                    return;
                }

                TypeInternal = value;

                ResetMassData();

                if (TypeInternal == BodyType.Static)
                {
                    LinearVelocityInternal = Vector2.Zero;
                    AngularVelocityInternal = 0.0f;
                    Sweep.Angle0 = Sweep.Angle;
                    Sweep.CenterOfMass0 = Sweep.CenterOfMass;
                    SynchronizeFixtures();
                }

                IsAwake = true;

                Force = Vector2.Zero;
                Torque = 0.0f;

                // Delete the attached contacts and mark so that contacts are created
                // the next time step.
                Simulation.RemoveContacts(this);
                Simulation.TouchFixtures(this);
            }
        }

        /// <summary>Gets or sets a value indicating whether this body is allowed to enter sleep state when it is not moving.</summary>
        public bool IsSleepAllowed
        {
            get { return IsSleepAllowedInternal; }
            set
            {
                IsSleepAllowedInternal = value;
                if (!value)
                {
                    IsAwake = true;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether this instance is awake.</summary>
        public bool IsAwake
        {
            get { return IsAwakeInternal; }
            internal set
            {
                if (value)
                {
                    if (!IsAwakeInternal)
                    {
                        IsAwakeInternal = true;
                        SleepTime = 0.0f;
                    }
                }
                else
                {
                    // Forcibly null any velocity when putting a body to sleep.
                    IsAwakeInternal = false;
                    LinearVelocityInternal = Vector2.Zero;
                    AngularVelocityInternal = 0.0f;
                    Force = Vector2.Zero;
                    Torque = 0.0f;
                }
            }
        }

        /// <summary>Get or set whether this body has a fixed rotation. Changing this value causes the mass to be reset.</summary>
        public bool IsRotationFixed
        {
            get { return _isRotationFixed; }
            set
            {
                if (_isRotationFixed == value)
                {
                    return;
                }

                _isRotationFixed = value;

                AngularVelocityInternal = 0.0f;

                ResetMassData();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is acting as a bullet. Dynamic bodies do usually not use
        ///     the TOI solver to solve against each other (only against static bodies). Bullets on the other hand also use the TOI
        ///     solver for processing collisions with other dynamic bodies (the common example is shooting a ball into a stack of
        ///     boxes).
        /// </summary>
        public bool IsBullet
        {
            get { return IsBulletInternal; }
            set { IsBulletInternal = value; }
        }

        /// <summary>
        ///     Get or set the world body origin position. Setting this breaks any contacts and wakes the other bodies.
        ///     Manipulating a body's transform may cause non-physical behavior.
        /// </summary>
        /// <value>The world position of the body's local origin.</value>
        public WorldPoint Position
        {
            get { return Transform.Translation; }
            set { SetTransform(value, Angle); }
        }

        /// <summary>
        ///     Get the angle in radians. Setting this breaks any contacts and wakes the other bodies. Manipulating a body's
        ///     transform may cause non-physical behavior.
        /// </summary>
        /// <value>The current world rotation angle in radians.</value>
        public float Angle
        {
            get { return Sweep.Angle; }
            set { SetTransform(Position, value); }
        }

        /// <summary>Get the world position of the center of mass.</summary>
        public WorldPoint WorldCenter
        {
            get { return Sweep.CenterOfMass; }
        }

        /// <summary>Get the local position of the center of mass.</summary>
        public LocalPoint LocalCenter
        {
            get { return Sweep.LocalCenter; }
        }

        /// <summary>Gets or sets the linear velocity of the center of mass.</summary>
        /// <value>The linear velocity of the center of mass.</value>
        public Vector2 LinearVelocity
        {
            get { return LinearVelocityInternal; }
            set
            {
                if (TypeInternal == BodyType.Static)
                {
                    return;
                }

                if (Vector2Util.Dot(ref value, ref value) > 0.0f)
                {
                    IsAwake = true;
                }

                LinearVelocityInternal = value;
            }
        }

        /// <summary>Gets or sets the angular velocity.</summary>
        /// <value>The angular velocity in radians/second.</value>
        public float AngularVelocity
        {
            get { return AngularVelocityInternal; }
            set
            {
                if (TypeInternal == BodyType.Static)
                {
                    return;
                }

                if (value * value > 0.0f)
                {
                    IsAwake = true;
                }

                AngularVelocityInternal = value;
            }
        }

        /// <summary>Get or sets the linear damping of the body.</summary>
        public float LinearDamping
        {
            get { return LinearDampingInternal; }
            set { LinearDampingInternal = value; }
        }

        /// <summary>Get or sets the angular damping of the body.</summary>
        public float AngularDamping
        {
            get { return AngularDampingInternal; }
            set { AngularDampingInternal = value; }
        }

        /// <summary>Gets or sets the mass of this body.</summary>
        public float Mass
        {
            get { return MassInternal; }
        }

        /// <summary>Get the rotational inertia of the body about the local origin.</summary>
        /// <value>The rotational inertia, usually in kg-m^2.</value>
        public float Inertia
        {
            get { return _inertia + MassInternal * Vector2Util.Dot(ref Sweep.LocalCenter, ref Sweep.LocalCenter); }
        }

        /// <summary>Gets the list of fixtures attached to this body.</summary>
        public IEnumerable<Component> Fixtures
        {
            get { return Manager.GetComponents(Entity, Fixture.TypeId); }
        }

        /// <summary>Gets the list of all joints attached to this body.</summary>
        public IEnumerable<Joint> Joints
        {
            get { return Simulation.GetJoints(this); }
        }

        /// <summary>Gets the physics system driving the simulation we're part of.</summary>
        private PhysicsSystem Simulation
        {
            get { return Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem; }
        }

        #endregion

        #region Fields

        /// <summary>The type of this body.</summary>
        internal BodyType TypeInternal;

        /// <summary>Whether this body is allowed to sleep.</summary>
        internal bool IsSleepAllowedInternal = true;

        /// <summary>Tracks whether this body is awake or not.</summary>
        internal bool IsAwakeInternal = true;

        /// <summary>Whether the rotation for this body is fixed or not.</summary>
        private bool _isRotationFixed;

        /// <summary>Whether this body should act as a bullet, if dynamic.</summary>
        internal bool IsBulletInternal;

        /// <summary>Used by the solver to look up body data inside the island data structure.</summary>
        internal int IslandIndex;

        /// <summary>The world transform of this body.</summary>
        internal WorldTransform Transform = WorldTransform.Identity;

        /// <summary>Representation of this body's movement over time.</summary>
        internal Sweep Sweep;

        /// <summary>The linear velocity of the body, i.e. the directed speed at which it moves.</summary>
        internal Vector2 LinearVelocityInternal;

        /// <summary>The angular velocity of this body, i.e. how fast it spins.</summary>
        internal float AngularVelocityInternal;

        /// <summary>
        ///     The force to apply to this body in the next update. This is reset after each update and only serves as an
        ///     accumulator.
        /// </summary>
        internal Vector2 Force;

        /// <summary>
        ///     The torque to apply to this body in the next update. This is reset after each update and only serves as an
        ///     accumulator.
        /// </summary>
        internal float Torque;

        /// <summary>Start of the list of joints attached to this body.</summary>
        internal int JointList = -1;

        /// <summary>Start of the list of contacts this body is involved in (i.e. any of its fixtures are involved in).</summary>
        internal int ContactList = -1;

        /// <summary>The mass of this body.</summary>
        internal float MassInternal;

        /// <summary>The inverse mass of this body (precomputed for performance).</summary>
        internal float InverseMass;

        /// <summary>The inertia of this body (rotation around local origin).</summary>
        private float _inertia;

        /// <summary>The inverse of the inertia (precomputed for performance).</summary>
        internal float InverseInertia;

        /// <summary>The linear damping of the body.</summary>
        internal float LinearDampingInternal;

        /// <summary>The angular damping of the body.</summary>
        internal float AngularDampingInternal;

        /// <summary>
        ///     Accumulates the total time the body has not moved (angular and linear velocity below threshold), to know when
        ///     to put the body to sleep.
        /// </summary>
        internal float SleepTime;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes the body with the specified type. This will not trigger mass recomputation, you will have to call
        ///     <see cref="ResetMassData"/>
        ///     to trigger that yourself.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="type">The type.</param>
        /// <param name="fixedRotation">Whether the rotation of this body is fixed.</param>
        /// <param name="isBullet">Whether to set the body as a bullet.</param>
        /// <param name="allowSleep">Whether to allow the body to sleep.</param>
        /// <returns></returns>
        public Body Initialize(
            WorldPoint position,
            float angle = 0,
            BodyType type = BodyType.Static,
            bool fixedRotation = false,
            bool isBullet = false,
            bool allowSleep = true)
        {
            Transform.Translation = position;
            Transform.Rotation.Set(angle);

            Sweep.CenterOfMass0 = Transform.Translation;
            Sweep.CenterOfMass = Transform.Translation;
            Sweep.Angle0 = angle;
            Sweep.Angle = angle;

            TypeInternal = type;

            if (TypeInternal == BodyType.Dynamic)
            {
                MassInternal = 1.0f;
                InverseMass = 1.0f;
            }

            _isRotationFixed = fixedRotation;
            IsBulletInternal = isBullet;
            IsSleepAllowedInternal = allowSleep;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            IsSleepAllowedInternal = true;
            IsAwakeInternal = true;
            _isRotationFixed = false;
            IsBulletInternal = false;
            IslandIndex = 0;
            Transform = WorldTransform.Identity;
            Sweep = new Sweep();
            LinearVelocityInternal = Vector2.Zero;
            AngularVelocityInternal = 0;
            Force = Vector2.Zero;
            Torque = 0;
            JointList = -1;
            ContactList = -1;
            MassInternal = 0;
            InverseMass = 0;
            _inertia = 0;
            InverseInertia = 0;
            LinearDampingInternal = 0;
            AngularDampingInternal = 0;
            SleepTime = 0;
        }

        #endregion

        #region Accessors

        /// <summary>Get the world coordinates of a point given the local coordinates.</summary>
        /// <param name="localPoint">The point on the body measured relative the the body's origin.</param>
        /// <returns>the same point expressed in world coordinates.</returns>
        public WorldPoint GetWorldPoint(LocalPoint localPoint)
        {
            return Transform.ToGlobal(localPoint);
        }

        /// <summary>Gets the world coordinates of a vector given the local coordinates.</summary>
        /// <param name="localVector">The vector fixed in the body.</param>
        /// <returns>The same vector expressed in world coordinates.</returns>
        public Vector2 GetWorldVector(Vector2 localVector)
        {
            return Transform.Rotation * localVector;
        }

        /// <summary>Gets the local point relative to the body's origin given a world point.</summary>
        /// <param name="worldPoint">The point in world coordinates.</param>
        /// <returns>The corresponding local point relative to the body's origin.</returns>
        public LocalPoint GetLocalPoint(WorldPoint worldPoint)
        {
            return Transform.ToLocal(worldPoint);
        }

        /// <summary>Gets the local vector given a world vector.</summary>
        /// <param name="worldVector">The vector in world coordinates.</param>
        /// <returns>The corresponding local vector.</returns>
        public Vector2 GetLocalVector(Vector2 worldVector)
        {
            return -Transform.Rotation * worldVector;
        }

        /// <summary>Gets the world linear velocity of a world point attached to this body.</summary>
        /// <param name="worldPoint">The point in world coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromWorldPoint(WorldPoint worldPoint)
        {
// ReSharper disable RedundantCast Necessary for FarPhysics.
            return LinearVelocityInternal +
                   Vector2Util.Cross(AngularVelocityInternal, (Vector2) (worldPoint - Sweep.CenterOfMass));
// ReSharper restore RedundantCast
        }

        /// <summary>Gets the world velocity of a local point.</summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromLocalPoint(LocalPoint localPoint)
        {
            return GetLinearVelocityFromWorldPoint(GetWorldPoint(localPoint));
        }

        /// <summary>
        ///     Apply a force at a world point. If the force is not applied at the center of mass, it will generate a torque
        ///     and affect the angular velocity. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">
        ///     if set to <c>true</c> also wake up the body.
        /// </param>
        public void ApplyForce(Vector2 force, WorldPoint point, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Wake up as necessary.
            if (wake && !IsAwake)
            {
                IsAwake = true;
            }

            // Only apply when awake.
            if (IsAwake)
            {
                Force += force;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                Torque += Vector2Util.Cross((Vector2) (point - Sweep.CenterOfMass), force);
// ReSharper restore RedundantCast
            }
        }

        /// <summary>Apply a force to the center of mass. This wakes up the body.</summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="wake">
        ///     if set to <c>true</c> also wake up the body.
        /// </param>
        public void ApplyForceToCenter(Vector2 force, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Wake up as necessary.
            if (wake && !IsAwake)
            {
                IsAwake = true;
            }

            // Only apply when awake.
            if (IsAwake)
            {
                Force += force;
            }
        }

        /// <summary>
        ///     Apply a torque. This affects the angular velocity without affecting the linear velocity of the center of mass.
        ///     This wakes up the body.
        /// </summary>
        /// <param name="torque">The torque about the z-axis (out of the screen), usually in N-m.</param>
        /// <param name="wake">
        ///     if set to <c>true</c> also wake up the body.
        /// </param>
        public void ApplyTorque(float torque, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Wake up as necessary.
            if (wake && !IsAwake)
            {
                IsAwake = true;
            }

            // Only apply when awake.
            if (IsAwake)
            {
                Torque += torque;
            }
        }

        /// <summary>
        ///     Apply an impulse at a point. This immediately modifies the velocity. It also modifies the angular velocity if
        ///     the point of application is not at the center of mass. This wakes up the body.
        /// </summary>
        /// <param name="impulse">The the world impulse vector, usually in N-seconds or kg-m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">
        ///     if set to <c>true</c> also wake up the body.
        /// </param>
        public void ApplyLinearImpulse(Vector2 impulse, WorldPoint point, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Wake up as necessary.
            if (wake && !IsAwake)
            {
                IsAwake = true;
            }

            // Only apply when awake.
            if (IsAwake)
            {
                LinearVelocityInternal += InverseMass * impulse;
// ReSharper disable RedundantCast Necessary for FarPhysics.
                AngularVelocityInternal += InverseInertia *
                                           Vector2Util.Cross((Vector2) (point - Sweep.CenterOfMass), impulse);
// ReSharper restore RedundantCast
            }
        }

        /// <summary>Apply an angular impulse.</summary>
        /// <param name="impulse">The angular impulse in units of kg*m*m/s.</param>
        /// <param name="wake">
        ///     if set to <c>true</c> also wake up the body.
        /// </param>
        public void ApplyAngularImpulse(float impulse, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Wake up as necessary.
            if (wake && !IsAwake)
            {
                IsAwake = true;
            }

            // Only apply when awake.
            if (IsAwake)
            {
                AngularVelocityInternal += InverseInertia * impulse;
            }
        }

        /// <summary>
        ///     Sets the transform of this body, i.e. its world position and angle. Use this when you need to set position and
        ///     angle, as it is more efficient than setting them separately via the properties. Setting this breaks any contacts
        ///     and wakes the other bodies. Manipulating a body's transform may cause non-physical behavior.
        /// </summary>
        /// <param name="position">The new world position.</param>
        /// <param name="angle">The new world angle.</param>
        public void SetTransform(WorldPoint position, float angle)
        {
            if (Simulation.IsLocked)
            {
                throw new System.InvalidOperationException("Cannot manually change position during update.");
            }

            Transform.Translation = position;
            Transform.Rotation.Set(angle);
            Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);
            Sweep.CenterOfMass0 = Sweep.CenterOfMass;
            Sweep.Angle = angle;
            Sweep.Angle0 = angle;

            foreach (Fixture fixture in Fixtures)
            {
                fixture.Synchronize();
            }

            Simulation.FindContactsBeforeNextUpdate();
        }

        /// <summary>Sets the mass properties for this body, overriding properties from any fixtures attached to this body.</summary>
        /// <param name="mass">The overall mass of the body.</param>
        /// <param name="center">The center of mass, relative to the local origin.</param>
        /// <param name="inertia">The rotational inertia about the local origin.</param>
        public void SetMassData(float mass, Vector2 center, float inertia)
        {
            // Do not allow changing a body's type during an update.
            if (Simulation.IsLocked)
            {
                throw new System.InvalidOperationException("Cannot change mass data during update.");
            }

            // Ignore if we're not a dynamic body.
            if (TypeInternal != BodyType.Dynamic)
            {
                return;
            }

            // Make sure we have a positive mass.
            MassInternal = mass;
            if (MassInternal <= 0)
            {
                MassInternal = 1;
                InverseMass = 1;
            }
            else
            {
                InverseMass = 1 / MassInternal;
            }

            // Make sure we have a positive inertia.
            if (inertia <= 0)
            {
                _inertia = 0;
                InverseInertia = 0;
            }
            else
            {
                _inertia = inertia - MassInternal * center.LengthSquared();
                InverseInertia = 1 / _inertia;
            }

            // Move center of mass.
            var oldCenter = Sweep.CenterOfMass;
            Sweep.LocalCenter = center;
            Sweep.CenterOfMass0 = Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);

            // Update center of mass velocity.
// ReSharper disable RedundantCast Necessary for FarPhysics.
            LinearVelocityInternal += Vector2Util.Cross(
                AngularVelocityInternal, (Vector2) (Sweep.CenterOfMass - oldCenter));
// ReSharper restore RedundantCast
        }

        /// <summary>
        ///     This resets the mass properties to the sum of the mass properties of the fixtures. This normally does not need
        ///     to be called unless you called SetMassData to override the mass and you later want to reset the mass.
        /// </summary>
        public void ResetMassData()
        {
            // Compute mass data from shapes. Each shape has its own density.
            MassInternal = 0.0f;
            InverseMass = 0.0f;
            _inertia = 0.0f;
            InverseInertia = 0.0f;
            Sweep.LocalCenter = Vector2.Zero;

            // Static and kinematic bodies have zero mass.
            if (TypeInternal == BodyType.Static || TypeInternal == BodyType.Kinematic)
            {
                Sweep.CenterOfMass0 = Transform.Translation;
                Sweep.CenterOfMass = Transform.Translation;
                Sweep.Angle0 = Sweep.Angle;
                return;
            }

            System.Diagnostics.Debug.Assert(TypeInternal == BodyType.Dynamic);

            // Accumulate mass over all fixtures.
            var localCenter = Vector2.Zero;
            foreach (Fixture fixture in Fixtures)
            {
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (fixture.Density == 0)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    continue;
                }

                float mass, inertia;
                Vector2 center;
                fixture.GetMassData(out mass, out center, out inertia);
                MassInternal += mass;
                localCenter += mass * center;
                _inertia += inertia;
            }

            // Compute center of mass.
            if (MassInternal <= 0)
            {
                // Force all dynamic bodies to have a positive mass.
                MassInternal = 1;
                InverseMass = 1;
            }
            else
            {
                InverseMass = 1 / MassInternal;
                localCenter *= InverseMass;
            }

            if (_isRotationFixed || _inertia <= 0)
            {
                _inertia = 0;
                InverseInertia = 0;
            }
            else
            {
                // Center the inertia about the center of mass.
                _inertia -= MassInternal * Vector2Util.Dot(ref localCenter, ref localCenter);
                InverseInertia = 1 / _inertia;
            }

            // Move center of mass.
            var oldCenter = Sweep.CenterOfMass;
            Sweep.LocalCenter = localCenter;
            Sweep.CenterOfMass0 = Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);

            // Update center of mass velocity.
// ReSharper disable RedundantCast Necessary for FarPhysics.
            LinearVelocityInternal += Vector2Util.Cross(
                AngularVelocityInternal, (Vector2) (Sweep.CenterOfMass - oldCenter));
// ReSharper restore RedundantCast
        }

        /// <summary>
        ///     Updates all of the bodies fixtures, telling them to update their positions in the index structure used for the
        ///     broad phase.
        /// </summary>
        internal void SynchronizeFixtures()
        {
            // Compute previous world transform.
            WorldTransform xf0;
            xf0.Rotation.Sin = (float) System.Math.Sin(Sweep.Angle0);
            xf0.Rotation.Cos = (float) System.Math.Cos(Sweep.Angle0);
            xf0.Translation = Sweep.CenterOfMass0 - xf0.Rotation * Sweep.LocalCenter;

            // Update all fixtures on this body in the index.
            foreach (Fixture fixture in Fixtures)
            {
                fixture.Synchronize(xf0, Transform);
            }
        }

        /// <summary>Advance to the new safe time. This doesn't sync the broad-phase.</summary>
        /// <param name="alpha">The time to advance.</param>
        internal void Advance(float alpha)
        {
            Sweep.Advance(alpha);
            Sweep.CenterOfMass = Sweep.CenterOfMass0;
            Sweep.Angle = Sweep.Angle0;
            Transform.Rotation.Set(Sweep.Angle);
            Transform.Translation = Sweep.CenterOfMass - Transform.Rotation * Sweep.LocalCenter;
        }

        /// <summary>Updates the transform of this body by setting it to the end state of the body's sweep data.</summary>
        internal void SynchronizeTransform()
        {
            Transform.Rotation.Set(Sweep.Angle);
            Transform.Translation = Sweep.CenterOfMass - Transform.Rotation * Sweep.LocalCenter;
        }

        /// <summary>Clears the forces currently active on this body.</summary>
        internal void ClearForces()
        {
            Force = Vector2.Zero;
            Torque = 0;
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