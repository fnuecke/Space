using Engine.ComponentSystem.Components;
using Engine.Physics.Detail.Math;
using Engine.Physics.Systems;
using Engine.Serialization;
using Engine.XnaExtensions;
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
    /// A body represents a physical object, tracking some of its global properties
    /// such as mass, velocity and world transform.
    /// </summary>
    public sealed class Body : Component
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

        public enum BodyType
        {
            /// <summary>
            /// A static body has no mass and no velocity and can only be moved
            /// manually, i.e. from outside the simulation.
            /// </summary>
            Static,

            /// <summary>
            /// A kinematic body has a set velocity, but no mass.
            /// </summary>
            Kinematic,

            /// <summary>
            /// A dynamic body has mass and a velocity determined by other
            /// forces inside the simulation. Moving it manually will lead to
            /// non-physical behavior.
            /// </summary>
            Dynamic
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the component is enabled or not. Disabled components will
        /// not be handled in the component's system's <c>Update()</c> method.
        /// </summary>
        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (Manager != null)
                {
                    var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
                    System.Diagnostics.Debug.Assert(physics != null);
                    if (physics.IsLocked)
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
                        // Mark so that contacts are created the next time step.
                        physics.QueueForContactSearch(Entity);
                    }
                    else
                    {
                        // Free any contacts we're involved in.
                        physics.RemoveContacts(Entity);
                    }
                }
                else
                {
                    base.Enabled = value;
                }
            }
        }

        /// <summary>
        /// Gets the type of this body.
        /// </summary>
        public BodyType Type
        {
            get { return _type; }
            set
            {
                var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
                System.Diagnostics.Debug.Assert(physics != null);
                if (physics.IsLocked)
                {
                    throw new System.InvalidOperationException("Cannot change type during update.");
                }

                // Skip if nothing changed.
                if (_type == value)
                {
                    return;
                }

                _type = value;

                ResetMassData();

                if (_type == BodyType.Static)
                {
                    _linearVelocity = Vector2.Zero;
                    _angularVelocity = 0.0f;
                    Sweep.Angle0 = Sweep.Angle;
                    Sweep.CenterOfMass0 = Sweep.CenterOfMass;
                    SynchronizeFixtures();
                }

                IsAwake = true;

                Force = Vector2.Zero;
                Torque = 0.0f;

                // Delete the attached contacts.
                physics.RemoveContacts(Entity);

                // Mark so that contacts are created the next time step.
                physics.QueueForContactSearch(Entity);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this body is allowed to enter
        /// sleep state when it is not moving.
        /// </summary>
        public bool AllowSleep
        {
            get { return _allowSleep; }
            set
            {
                var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
                System.Diagnostics.Debug.Assert(physics != null);
                if (physics.IsLocked)
                {
                    throw new System.InvalidOperationException("Cannot change sleep allowed flag during update.");
                }
                
                _allowSleep = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is awake.
        /// </summary>
        public bool IsAwake
        {
            get { return _isAwake; }
            internal set
            {
                if (value)
                {
                    if (!_isAwake)
                    {
                        _isAwake = true;
                        SleepTime = 0.0f;
                    }
                }
                else
                {
                    // Forcibly null any velocity when putting a body to sleep.
                    _isAwake = false;
                    _linearVelocity = Vector2.Zero;
                    _angularVelocity = 0.0f;
                    Force = Vector2.Zero;
                    Torque = 0.0f;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is acting as
        /// a bullet. Dynamic bodies do usually not use the TOI solver to solve
        /// against each other (only against static bodies). Bullets on the
        /// other hand also use the TOI solver for processing collisions with
        /// other dynamic bodies (the common example is shooting a ball into a
        /// stack of boxes).
        /// </summary>
        public bool IsBullet
        {
            get { return _isBullet; }
            set
            {
                var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
                System.Diagnostics.Debug.Assert(physics != null);
                if (physics.IsLocked)
                {
                    throw new System.InvalidOperationException("Cannot change bullet flag during update.");
                }
                
                _isBullet = value;
            }
        }

        /// <summary>
        /// This bit mask representing the collision groups this component is
        /// part of. Components sharing at least one group will not be tested
        /// against each other.
        /// </summary>
        public uint CollisionGroups
        {
            get { return _collisionGroups; }
            set
            {
                var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
                System.Diagnostics.Debug.Assert(physics != null);
                if (physics.IsLocked)
                {
                    throw new System.InvalidOperationException("Cannot change collision groups during update.");
                }

                // Skip if nothing changed.
                if (_collisionGroups == value)
                {
                    return;
                }

                _collisionGroups = value;

                // Mark this body as changed to look for new contacts.
                physics.QueueForContactSearch(Entity);
            }
        }

        /// <summary>
        /// Get or set the world body origin position. Setting this breaks any
        /// contacts and wakes the other bodies. Manipulating a body's
        /// transform may cause non-physical behavior.
        /// </summary>
        /// <value>
        /// The world position of the body's local origin.
        /// </value>
        public WorldPoint Position
        {
            get { return Transform.Translation; }
            set { SetTransform(value, Angle); }
        }

        /// <summary>
        /// Get the angle in radians. Setting this breaks any contacts
        /// and wakes the other bodies. Manipulating a body's transform
        /// may cause non-physical behavior.
        /// </summary>
        /// <value>
        /// The current world rotation angle in radians.
        /// </value>
        public float Angle
        {
            get { return Sweep.Angle; }
            set { SetTransform(Position, value); }
        }

        /// <summary>
        /// Get the world position of the center of mass.
        /// </summary>
        public WorldPoint WorldCenter
        {
            get { return Sweep.CenterOfMass; }
        }

        /// <summary>
        /// Get the local position of the center of mass.
        /// </summary>
        public LocalPoint LocalCenter
        {
            get { return Sweep.LocalCenter; }
        }

        /// <summary>
        /// Gets or sets the linear velocity of the center of mass.
        /// </summary>
        /// <value>
        /// The linear velocity of the center of mass.
        /// </value>
        public Vector2 LinearVelocity
        {
            get { return _linearVelocity; }
            set
            {
                if (_type == BodyType.Static)
                {
                    return;
                }

                if (Vector2.Dot(value, value) > 0.0f)
                {
                    IsAwake = true;
                }

                _linearVelocity = value;
            }
        }

        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>
        /// The angular velocity in radians/second.
        /// </value>
        public float AngularVelocity
        {
            get { return _angularVelocity; }
            set
            {
                if (_type == BodyType.Static)
                {
                    return;
                }

                if (value * value > 0.0f)
                {
                    IsAwake = true;
                }

                _angularVelocity = value;
            }
        }

        /// <summary>
        /// Get or sets the linear damping of the body.
        /// </summary>
        public float LinearDamping
        {
            get { return _linearDamping; }
            set { _linearDamping = value; }
        }

        /// <summary>
        /// Get or sets the angular damping of the body.
        /// </summary>
        public float AngularDamping
        {
            get { return _angularDamping; }
            set { _angularDamping = value; }
        }

        /// <summary>
        /// Gets or sets the mass of this body.
        /// </summary>
        public float Mass { get { return _mass; } }

        /// <summary>
        /// Get the rotational inertia of the body about the local origin.
        /// </summary>
        /// <value>
        /// The rotational inertia, usually in kg-m^2.
        /// </value>
        public float Inertia
        {
            get { return _inertia + _mass * Vector2.Dot(Sweep.LocalCenter, Sweep.LocalCenter); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The type of this body.
        /// </summary>
        internal BodyType _type;

        /// <summary>
        /// Whether this body is allowed to sleep.
        /// </summary>
        internal bool _allowSleep = true;

        /// <summary>
        /// Tracks whether this body is awake or not.
        /// </summary>
        internal bool _isAwake = true;

        /// <summary>
        /// Whether this body should act as a bullet, if dynamic.
        /// </summary>
        internal bool _isBullet;

        /// <summary>
        /// The collision groups the body is in, as a bit mask.
        /// </summary>
        private uint _collisionGroups;

        /// <summary>
        /// Used by the solver to look up body data inside the island data
        /// structure.
        /// </summary>
        internal int IslandIndex;

        /// <summary>
        /// The world transform of this body.
        /// </summary>
        internal WorldTransform Transform = WorldTransform.Identity;

        /// <summary>
        /// Representation of this body's movement over time.
        /// </summary>
        internal Sweep Sweep;

        /// <summary>
        /// The linear velocity of the body, i.e. the directed speed at which it moves.
        /// </summary>
        internal Vector2 _linearVelocity;

        /// <summary>
        /// The angular velocity of this body, i.e. how fast it spins.
        /// </summary>
        internal float _angularVelocity;

        /// <summary>
        /// The force to apply to this body in the next update. This is reset
        /// after each update and only serves as an accumulator.
        /// </summary>
        internal Vector2 Force;

        /// <summary>
        /// The torque to apply to this body in the next update. This is reset
        /// after each update and only serves as an accumulator.
        /// </summary>
        internal float Torque;

        /// <summary>
        /// The mass of this body.
        /// </summary>
        private float _mass;

        /// <summary>
        /// The inverse mass of this body (precomputed for performance).
        /// </summary>
        internal float InverseMass;

        /// <summary>
        /// The inertia of this body (rotation around local origin).
        /// </summary>
        private float _inertia;

        /// <summary>
        /// The inverse of the inertia (precomputed for performance).
        /// </summary>
        internal float InverseInertia;

        /// <summary>
        /// The linear damping of the body.
        /// </summary>
        internal float _linearDamping;

        /// <summary>
        /// The angular damping of the body.
        /// </summary>
        internal float _angularDamping;

        /// <summary>
        /// Accumulates the total time the body has not moved (angular and
        /// linear velocity below threshold), to know when to put the body
        /// to sleep.
        /// </summary>
        internal float SleepTime;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            System.Diagnostics.Debug.Assert(!((PhysicsSystem)Manager.GetSystem(PhysicsSystem.TypeId)).IsLocked);

            base.Initialize(other);

            var otherBody = (Body)other;

            _type = otherBody._type;
            _collisionGroups = otherBody._collisionGroups;
            _allowSleep = otherBody._allowSleep;
            _isAwake = otherBody._isAwake;
            _isBullet = otherBody._isBullet;
            _mass = otherBody._mass;
            _inertia = otherBody._inertia;
            InverseMass = otherBody.InverseMass;
            InverseInertia = otherBody.InverseInertia;
            Transform = otherBody.Transform;
            _linearVelocity = otherBody._linearVelocity;
            _angularVelocity = otherBody._angularVelocity;
            _linearDamping = otherBody._linearDamping;
            _angularDamping = otherBody._angularDamping;
            Sweep = otherBody.Sweep;
            SleepTime = otherBody.SleepTime;
            Force = otherBody.Force;
            Torque = otherBody.Torque;

            return this;
        }

        /// <summary>
        /// Initializes the body with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Body Initialize(BodyType type)
        {
            _type = type;

            if (_type == BodyType.Dynamic)
            {
                _mass = 1.0f;
                InverseMass = 1.0f;
            }

            return this;
        }

        /// <summary>
        /// Initializes the body with the specified collision groups.
        /// </summary>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns></returns>
        public Body Initialize(uint collisionGroups)
        {
            _collisionGroups = collisionGroups;

            return this;
        }

        /// <summary>
        /// Initializes the body with the specified flags.
        /// </summary>
        /// <param name="isBullet">Whether to set the body as a bullet.</param>
        /// <param name="allowSleep">Whether to allow the body to sleep.</param>
        /// <returns></returns>
        public Body Initialize(bool isBullet, bool allowSleep = true)
        {
            _isBullet = isBullet;
            _allowSleep = allowSleep;

            return this;
        }

        /// <summary>
        /// Initializes the body with the specified position and angle.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <param name="angle">The angle.</param>
        /// <returns></returns>
        public Body Initialize(WorldPoint position, float angle)
        {
            Transform.Translation = position;
            Transform.Rotation.Set(angle);

            Sweep.CenterOfMass0 = Transform.Translation;
            Sweep.CenterOfMass = Transform.Translation;
            Sweep.Angle0 = angle;
            Sweep.Angle = angle;

            SynchronizeFixtures();

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _collisionGroups = 0;
            _allowSleep = true;
            _isAwake = true;
            _isBullet = false;
            _mass = 0;
            _inertia = 0;
            InverseMass = 0;
            InverseInertia = 0;
            Transform = WorldTransform.Identity;
            _linearVelocity = Vector2.Zero;
            _angularVelocity = 0;
            _linearDamping = 0;
            _angularDamping = 0;
            Sweep = new Sweep();
            SleepTime = 0;
            Force = Vector2.Zero;
            Torque = 0;
            IslandIndex = 0;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Get the world coordinates of a point given the local coordinates.
        /// </summary>
        /// <param name="localPoint">The point on the body measured relative the the body's origin.</param>
        /// <returns>the same point expressed in world coordinates.</returns>
        public WorldPoint GetWorldPoint(LocalPoint localPoint)
        {
            return Transform.ToGlobal(localPoint);
        }

        /// <summary>
        /// Gets the world coordinates of a vector given the local coordinates.
        /// </summary>
        /// <param name="localVector">The vector fixed in the body.</param>
        /// <returns>The same vector expressed in world coordinates.</returns>
        public Vector2 GetWorldVector(Vector2 localVector)
        {
            return Transform.Rotation * localVector;
        }

        /// <summary>
        /// Gets the local point relative to the body's origin given a world point.
        /// </summary>
        /// <param name="worldPoint">The point in world coordinates.</param>
        /// <returns>The corresponding local point relative to the body's origin.</returns>
        public LocalPoint GetLocalPoint(WorldPoint worldPoint)
        {
            return Transform.ToLocal(worldPoint);
        }

        /// <summary>
        /// Gets the local vector given a world vector.
        /// </summary>
        /// <param name="worldVector">The vector in world coordinates.</param>
        /// <returns>The corresponding local vector.</returns>
        public Vector2 GetLocalVector(Vector2 worldVector)
        {
            return -Transform.Rotation * worldVector;
        }

        /// <summary>
        /// Gets the world linear velocity of a world point attached to this body.
        /// </summary>
        /// <param name="worldPoint">The point in world coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromWorldPoint(WorldPoint worldPoint)
        {
            return _linearVelocity + Vector2Util.Cross(_angularVelocity, (Vector2)(worldPoint - Sweep.CenterOfMass));
        }

        /// <summary>
        /// Gets the world velocity of a local point.
        /// </summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromLocalPoint(LocalPoint localPoint)
        {
            return GetLinearVelocityFromWorldPoint(GetWorldPoint(localPoint));
        }

        /// <summary>
        /// Apply a force at a world point. If the force is not
        /// applied at the center of mass, it will generate a torque and
        /// affect the angular velocity. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">if set to <c>true</c> also wake up the body.</param>
        public void ApplyForce(Vector2 force, WorldPoint point, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (_type != BodyType.Dynamic)
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
                Torque += Vector2Util.Cross((Vector2)(point - Sweep.CenterOfMass), force);
            }
        }

        /// <summary>
        /// Apply a force to the center of mass. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="wake">if set to <c>true</c> also wake up the body.</param>
        public void ApplyForceToCenter(Vector2 force, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (_type != BodyType.Dynamic)
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
        /// Apply a torque. This affects the angular velocity
        /// without affecting the linear velocity of the center of mass.
        /// This wakes up the body.
        /// </summary>
        /// <param name="torque">The torque about the z-axis (out of the screen), usually in N-m.</param>
        /// <param name="wake">if set to <c>true</c> also wake up the body.</param>
        public void ApplyTorque(float torque, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (_type != BodyType.Dynamic)
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
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// It also modifies the angular velocity if the point of application
        /// is not at the center of mass. This wakes up the body.
        /// </summary>
        /// <param name="impulse">The the world impulse vector, usually in N-seconds or kg-m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">if set to <c>true</c> also wake up the body.</param>
        public void ApplyLinearImpulse(Vector2 impulse, WorldPoint point, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (_type != BodyType.Dynamic)
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
                _linearVelocity += InverseMass * impulse;
                _angularVelocity += InverseInertia * Vector2Util.Cross((Vector2)(point - Sweep.CenterOfMass), impulse);
            }
	    }

        /// <summary>
        /// Apply an angular impulse.
        /// </summary>
        /// <param name="impulse">The angular impulse in units of kg*m*m/s.</param>
        /// <param name="wake">if set to <c>true</c> also wake up the body.</param>
        public void ApplyAngularImpulse(float impulse, bool wake = true)
        {
            // Skip for non-dynamic bodies.
            if (_type != BodyType.Dynamic)
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
                _angularVelocity += InverseInertia * impulse;
            }
	    }

        /// <summary>
        /// Sets the transform of this body, i.e. its world position and angle.
        /// Use this when you need to set position and angle, as it is more
        /// efficient than setting them separately via the properties.
        /// Setting this breaks any contacts and wakes the other bodies.
        /// Manipulating a body's transform may cause non-physical behavior.
        /// </summary>
        /// <param name="position">The new world position.</param>
        /// <param name="angle">The new world angle.</param>
        public void SetTransform(WorldPoint position, float angle)
        {
            var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
            System.Diagnostics.Debug.Assert(physics != null);
            if (physics.IsLocked)
            {
                throw new System.InvalidOperationException("Cannot manually change position during update.");
            }

            Transform.Translation = position;
            Transform.Rotation.Set(angle);
            Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);
            Sweep.CenterOfMass0 = Sweep.CenterOfMass;
            Sweep.Angle = angle;
            Sweep.Angle0 = angle;

            foreach (Fixture fixture in Manager.GetComponents(Entity, Fixture.TypeId))
            {
                fixture.Synchronize();
            }

            physics.RemoveContacts(Entity);
            physics.QueueForContactSearch(Entity);
        }

        /// <summary>
        /// Sets the mass properties for this body, overriding properties from any
        /// fixtures attached to this body.
        /// </summary>
        /// <param name="mass">The overall mass of the body.</param>
        /// <param name="center">The center of mass, relative to the local origin.</param>
        /// <param name="inertia">The rotational inertia about the local origin.</param>
        public void SetMassData(float mass, Vector2 center, float inertia)
        {
            // Do not allow changing a body's type during an update.
            if (((PhysicsSystem)Manager.GetSystem(PhysicsSystem.TypeId)).IsLocked)
            {
                throw new System.InvalidOperationException("Cannot change mass data during update.");
            }

            // Ignore if we're not a dynamic body.
            if (_type != BodyType.Dynamic)
            {
                return;
            }

            // Make sure we have a positive mass.
            _mass = mass;
            if (_mass <= 0)
            {
                _mass = 1;
                InverseMass = 1;
            }
            else
            {
                InverseMass = 1 / _mass;
            }

            // Make sure we have a positive inertia.
            if (inertia <= 0)
            {
                _inertia = 0;
                InverseInertia = 0;
            }
            else
            {
                _inertia = inertia - _mass * center.LengthSquared();
                InverseInertia = 1 / _inertia;
            }

            // Move center of mass.
            var oldCenter = Sweep.CenterOfMass;
            Sweep.LocalCenter = center;
            Sweep.CenterOfMass0 = Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);

            // Update center of mass velocity.
            _linearVelocity += Vector2Util.Cross(_angularVelocity, (Vector2)(Sweep.CenterOfMass - oldCenter));
        }

        /// <summary>
        /// This resets the mass properties to the sum of the mass properties of the fixtures.
        /// This normally does not need to be called unless you called SetMassData to override
        /// the mass and you later want to reset the mass.
        /// </summary>
        public void ResetMassData()
        {
            // Compute mass data from shapes. Each shape has its own density.
            _mass = 0.0f;
            InverseMass = 0.0f;
            _inertia = 0.0f;
            InverseInertia = 0.0f;
            Sweep.LocalCenter = Vector2.Zero;

            // Static and kinematic bodies have zero mass.
            if (_type == BodyType.Static || _type == BodyType.Kinematic)
            {
                Sweep.CenterOfMass0 = Transform.Translation;
                Sweep.CenterOfMass = Transform.Translation;
                Sweep.Angle0 = Sweep.Angle;
                return;
            }

            System.Diagnostics.Debug.Assert(_type == BodyType.Dynamic);

            // Accumulate mass over all fixtures.
            var localCenter = Vector2.Zero;
            foreach (Fixture f in Manager.GetComponents(Entity, Fixture.TypeId))
            {
                if (f.Density == 0)
                {
                    continue;
                }

                float mass, inertia;
                Vector2 center;
                f.GetMassData(out mass, out center, out inertia);
                _mass += mass;
                localCenter += mass * center;
                _inertia += inertia;
            }

            // Compute center of mass.
            if (_mass <= 0)
            {
                // Force all dynamic bodies to have a positive mass.
                _mass = 1;
                InverseMass = 1;
            }
            else
            {
                InverseMass = 1 / _mass;
                localCenter *= InverseMass;
            }

            if (_inertia <= 0)
            {
                _inertia = 0;
                InverseInertia = 0;
            }
            else
            {
                // Center the inertia about the center of mass.
                _inertia -= _mass * Vector2.Dot(localCenter, localCenter);
                InverseInertia = 1 / _inertia;
            }

            // Move center of mass.
            var oldCenter = Sweep.CenterOfMass;
            Sweep.LocalCenter = localCenter;
            Sweep.CenterOfMass0 = Sweep.CenterOfMass = Transform.ToGlobal(Sweep.LocalCenter);

            // Update center of mass velocity.
            _linearVelocity += Vector2Util.Cross(_angularVelocity, (Vector2)(Sweep.CenterOfMass - oldCenter));
        }

        /// <summary>
        /// Determines whether the two bodies should the collide, based on their
        /// collision groups.
        /// </summary>
        /// <param name="other">The other body.</param>
        /// <returns>Whether to check for collision between the tow.</returns>
        internal bool ShouldCollide(Body other)
        {
            return
                // At least one body should be dynamic.
                (_type == BodyType.Dynamic || other._type == BodyType.Dynamic) &&
                // Things that do not share at least one group collide.
                (_collisionGroups & other._collisionGroups) == 0;
        }

        /// <summary>
        /// Updates all of the bodies fixtures, telling them to update their positions
        /// in the index structure used for the broad phase.
        /// </summary>
        internal void SynchronizeFixtures()
        {
            // Compute previous world transform.
            WorldTransform xf0;
            xf0.Rotation.Sin = (float)System.Math.Sin(Sweep.Angle0);
            xf0.Rotation.Cos = (float)System.Math.Cos(Sweep.Angle0);
            xf0.Translation = Sweep.CenterOfMass0 - xf0.Rotation * Sweep.LocalCenter;

            // Update all fixtures on this body in the index.
            foreach (Fixture fixture in Manager.GetComponents(Entity, Fixture.TypeId))
            {
                fixture.Synchronize(xf0, Transform);
            }
        }

        /// <summary>
        /// Advance to the new safe time. This doesn't sync the broad-phase.
        /// </summary>
        /// <param name="alpha">The time to advance.</param>
        internal void Advance(float alpha)
        {
	        Sweep.Advance(alpha);
	        Sweep.CenterOfMass = Sweep.CenterOfMass0;
	        Sweep.Angle = Sweep.Angle0;
	        Transform.Rotation.Set(Sweep.Angle);
	        Transform.Translation = Sweep.CenterOfMass - Transform.Rotation * Sweep.LocalCenter;
        }

        /// <summary>
        /// Updates the transform of this body by setting it to the end state of the
        /// body's sweep data.
        /// </summary>
        internal void SynchronizeTransform()
        {
            Transform.Rotation.Set(Sweep.Angle);
            Transform.Translation = Sweep.CenterOfMass - Transform.Rotation * Sweep.LocalCenter;
        }

        /// <summary>
        /// Clears the forces currently active on this body.
        /// </summary>
        internal void ClearForces()
        {
            Force = Vector2.Zero;
            Torque = 0;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            System.Diagnostics.Debug.Assert(!((PhysicsSystem)Manager.GetSystem(PhysicsSystem.TypeId)).IsLocked);

            return base.Packetize(packet)
                .Write((byte)_type)
                .Write(_allowSleep)
                .Write(_isAwake)
                .Write(_isBullet)
                .Write(_collisionGroups)
                .Write(Transform)
                .Write(Sweep)
                .Write(_linearVelocity)
                .Write(_angularVelocity)
                .Write(Force)
                .Write(Torque)
                .Write(_mass)
                .Write(_inertia)
                .Write(_linearDamping)
                .Write(_angularDamping)
                .Write(SleepTime);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            System.Diagnostics.Debug.Assert(!((PhysicsSystem)Manager.GetSystem(PhysicsSystem.TypeId)).IsLocked);

            base.Depacketize(packet);

            _type = (BodyType)packet.ReadByte();
            _allowSleep = packet.ReadBoolean();
            _isAwake = packet.ReadBoolean();
            _isBullet = packet.ReadBoolean();
            _collisionGroups = packet.ReadUInt32();
            Transform = packet.ReadWorldTransform();
            Sweep = packet.ReadSweep();
            _linearVelocity = packet.ReadVector2();
            _angularVelocity = packet.ReadSingle();
            Force = packet.ReadVector2();
            Torque = packet.ReadSingle();
            _mass = packet.ReadSingle();
            InverseMass = 1 / _mass;
            _inertia = packet.ReadSingle();
            InverseInertia = 1 / _inertia;
            _linearDamping = packet.ReadSingle();
            _angularDamping = packet.ReadSingle();
            SleepTime = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            System.Diagnostics.Debug.Assert(!((PhysicsSystem)Manager.GetSystem(PhysicsSystem.TypeId)).IsLocked);

            base.Hash(hasher);

            hasher
                .Put((byte)_type)
                .Put(_allowSleep)
                .Put(_isAwake)
                .Put(_isBullet)
                .Put(_collisionGroups)
                .Put(Transform) // TODO
                .Put(Sweep) // TODO
                .Put(_linearVelocity)
                .Put(_angularVelocity)
                .Put(Force)
                .Put(Torque)
                .Put(_mass)
                .Put(_inertia)
                .Put(_linearDamping)
                .Put(_angularDamping)
                .Put(SleepTime);
        }

        #endregion
    }
}
