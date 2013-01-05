using System.Collections.Generic;
using Engine.Physics.Components;
using Engine.Physics.Contacts;
using Engine.Physics.Joints;
using Microsoft.Xna.Framework;

namespace Engine.Physics
{
    /// <summary>
    /// This class represents a graph made up from bodies as vertices and the
    /// contacts between them as edges. This is used for more efficient solving,
    /// because it is only necessary to solve inside an island.
    /// </summary>
    internal sealed class Island
    {
        #region Properties

        /// <summary>
        /// Gets the list of all bodies currently in this island.
        /// </summary>
        public IEnumerable<Body> Bodies
        {
            get { return _bodies; }
        }

        /// <summary>
        /// Gets the list of processed bodie, set via <see cref="MarkProcessed(Engine.Physics.Components.Body)"/>.
        /// </summary>
        public IEnumerable<Body> ProcessedBodies
        {
            get { return _processedBodies; }
        }

        /// <summary>
        /// Whether this island is full. This is used during TOI solving, where
        /// we limit the island size (for normal solving the island is always
        /// scaled for the worst case).
        /// </summary>
        public bool IsFull
        {
            get
            {
                return _bodies.Count == 2 * Settings.MaxTOIContacts ||
                       _contacts.Count == Settings.MaxTOIContacts;
            }
        }

        #endregion

        #region Fields

        /// <summary>The solver that does the actual work. We keep a copy that we re-use
        /// every update.</summary>
        private readonly ContactSolver _solver;

        /// <summary>This list is used to flag bodies as processed, to avoid double work
        /// when building islands.</summary>
        private readonly HashSet<Body> _processedBodies = new HashSet<Body>();

        /// <summary>This is used to flag contacts as processed, to avoid double work when
        /// building islands.</summary>
        private readonly HashSet<Contact> _processedContacts = new HashSet<Contact>();

        /// <summary>This is used to flag joints as processed, to avoid double work when
        /// building islands.</summary>
        private readonly HashSet<Joint> _processedJoints = new HashSet<Joint>();

        /// <summary>The list of bodies in this island.</summary>
        private readonly List<Body> _bodies = new List<Body>();

        /// <summary>The list of contacts in this island.</summary>
        private readonly List<Contact> _contacts = new List<Contact>();

        /// <summary>The list of joints in this island.</summary>
        private readonly List<Joint> _joints = new List<Joint>();

        /// <summary>Position buffer for solver.</summary>
        private Position[] _positions = new Position[0];

        /// <summary>Velocity buffer for solver.</summary>
        private Velocity[] _velocities = new Velocity[0];

        #endregion

        #region Initialization

        /// <summary>Initializes a new instance of the <see cref="Island"/> class.</summary>
        public Island()
        {
            _solver = new ContactSolver(_contacts);
        }

        /// <summary>Resets the island and ensures the specified capacities.</summary>
        /// <param name="bodyCapacity">The required body capacity.</param>
        /// <param name="contactCapacity">The required contact capacity.</param>
        /// <param name="jointCapacity">The required joint capacity.</param>
        public void Reset(int bodyCapacity, int contactCapacity, int jointCapacity)
        {
            Clear();
            UnmarkAllBodies();
            UnmarkAllContacts();
            UnmarkAllJoints();

            _bodies.Capacity = System.Math.Max(_bodies.Capacity, bodyCapacity);
            _contacts.Capacity = System.Math.Max(_contacts.Capacity, contactCapacity);
            _joints.Capacity = System.Math.Max(_joints.Capacity, jointCapacity);

            if (bodyCapacity > _positions.Length)
            {
                _positions = new Position[bodyCapacity];
                _velocities = new Velocity[bodyCapacity];
            }

            _solver.Reset(_positions, _velocities, contactCapacity);
        }

        #endregion

        #region Accessors

        /// <summary>Clears the island of all bodies and contacts.</summary>
        public void Clear()
        {
            _bodies.Clear();
            _contacts.Clear();
            _joints.Clear();
        }

        /// <summary>Adds the specified body to the island.</summary>
        /// <param name="body">The body.</param>
        public void Add(Body body)
        {
            body.IslandIndex = _bodies.Count;
            _bodies.Add(body);
        }

        /// <summary>Adds the specified contact to the island.</summary>
        /// <param name="contact">The contact.</param>
        public void Add(Contact contact)
        {
            _contacts.Add(contact);
        }

        /// <summary>Adds the specified joint to the island.</summary>
        /// <param name="joint">The joint.</param>
        public void Add(Joint joint)
        {
            _joints.Add(joint);
        }

        /// <summary>Marks the specified body as processed, to avoid double work.</summary>
        /// <param name="body">The body.</param>
        public void MarkProcessed(Body body)
        {
            _processedBodies.Add(body);
        }

        /// <summary>Marks the contact as processed, to avoid double work.</summary>
        /// <param name="contact">The contact.</param>
        public void MarkProcessed(Contact contact)
        {
            _processedContacts.Add(contact);
        }

        /// <summary>Marks the joint as processed, to avoid double work.</summary>
        /// <param name="joint">The joint.</param>
        public void MarkProcessed(Joint joint)
        {
            _processedJoints.Add(joint);
        }

        /// <summary>Determines whether the specified body is already processed.</summary>
        /// <param name="body">The body.</param>
        /// <returns><c>true</c> if the specified body is processed; otherwise, <c>false</c>.</returns>
        public bool IsProcessed(Body body)
        {
            return _processedBodies.Contains(body);
        }

        /// <summary>Determines whether the specified contact is already processed.</summary>
        /// <param name="contact">The contact.</param>
        /// <returns><c>true</c> if the specified contact is processed; otherwise, <c>false</c>.</returns>
        public bool IsProcessed(Contact contact)
        {
            return _processedContacts.Contains(contact);
        }

        /// <summary>Determines whether the specified joint is already processed.</summary>
        /// <param name="joint">The joint.</param>
        /// <returns><c>true</c> if the specified joint is processed; otherwise, <c>false</c>.</returns>
        public bool IsProcessed(Joint joint)
        {
            return _processedJoints.Contains(joint);
        }

        /// <summary>Clears all bodies from being marked as processed so they can
        /// be processed again with other islands.</summary>
        public void UnmarkAllBodies()
        {
            _processedBodies.Clear();
        }

        /// <summary>
        /// Clears all static bodies from being marked as processed so they
        /// can be processed again with other islands.
        /// </summary>
        public void UnmarkStaticBodies()
        {
            _processedBodies.RemoveWhere(x => x.TypeInternal == Body.BodyType.Static);
        }

        /// <summary>Clears the specified contact from being marked as processed.</summary>
        /// <param name="contact">The contact.</param>
        public void UnmarkContact(Contact contact)
        {
            _processedContacts.Remove(contact);
        }

        /// <summary>Clears all contacts from being marked as processed.</summary>
        private void UnmarkAllContacts()
        {
            _processedContacts.Clear();
        }

        /// <summary>Clears all joints from being marked as processed.</summary>
        private void UnmarkAllJoints()
        {
            _processedJoints.Clear();
        }

        #endregion

        #region Logic

        /// <summary>Perform normal solve step.</summary>
        /// <param name="step">The time step information.</param>
        /// <param name="gravity">The global gravity.</param>
        public void Solve(TimeStep step, Vector2 gravity)
        {
            var h = step.DeltaT;

            // Integrate velocities and apply damping. Initialize the body state.
            for (var i = 0; i < _bodies.Count; ++i)
            {
                var b = _bodies[i];

                var c = b.Sweep.CenterOfMass;
                var a = b.Sweep.Angle;
                var v = b.LinearVelocityInternal;
                var w = b.AngularVelocityInternal;

                // Store positions for continuous collision.
                b.Sweep.CenterOfMass0 = b.Sweep.CenterOfMass;
                b.Sweep.Angle0 = b.Sweep.Angle;

                if (b.TypeInternal == Body.BodyType.Dynamic)
                {
                    // Integrate velocities.
                    v += h * (gravity + b.InverseMass * b.Force);
                    w += h * b.InverseInertia * b.Torque;

                    // Apply damping.
                    // ODE: dv/dt + c * v = 0
                    // Solution: v(t) = v0 * exp(-c * t)
                    // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                    // v2 = exp(-c * dt) * v1
                    // Taylor expansion:
                    // v2 = (1.0f - c * dt) * v1
                    v *= MathHelper.Clamp(1.0f - h * b.LinearDampingInternal, 0.0f, 1.0f);
                    w *= MathHelper.Clamp(1.0f - h * b.AngularDampingInternal, 0.0f, 1.0f);
                }

                _positions[i].Point = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;
            }

            // Initialize velocity constraints.
            _solver.Initialize(step);
            _solver.InitializeVelocityConstraints();

            if (step.IsWarmStarting)
            {
                _solver.WarmStart();
            }

            foreach (var joint in _joints)
            {
                joint.InitializeVelocityConstraints(step, _positions, _velocities);
            }

            // Solve velocity constraints
            for (var i = 0; i < Settings.VelocityIterations; ++i)
            {
                foreach (var joint in _joints)
                {
                    joint.SolveVelocityConstraints(step, _positions, _velocities);
                }
                _solver.SolveVelocityConstraints();
            }

            // Store impulses for warm starting
            _solver.StoreImpulses();

            // Integrate positions.
            for (var i = 0; i < _bodies.Count; ++i)
            {
                var c = _positions[i].Point;
                var a = _positions[i].Angle;
                var v = _velocities[i].LinearVelocity;
                var w = _velocities[i].AngularVelocity;

                // Check for large velocities
                var translation = h * v;
                if (Vector2.Dot(translation, translation) > Settings.MaxTranslationSquared)
                {
                    var ratio = Settings.MaxTranslation / translation.Length();
                    v *= ratio;
                }

                var rotation = h * w;
                if (rotation * rotation > Settings.MaxRotationSquared)
                {
                    var ratio = Settings.MaxRotation / System.Math.Abs(rotation);
                    w *= ratio;
                }

                // Integrate
                c += h * v;
                a += h * w;

                _positions[i].Point = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;
            }

            // Solve position constraints
            var positionsSolved = false;
            for (var i = 0; i < Settings.PositionIterations; ++i)
            {
                var contactsFinished = _solver.SolvePositionConstraints();
                var jointsFinished = true;
                foreach (var joint in _joints)
                {
                    jointsFinished = jointsFinished &&
                                     joint.SolvePositionConstraints(step, _positions, _velocities);
                }
                if (contactsFinished && jointsFinished)
                {
                    // Exit early if the position errors are small.
                    positionsSolved = true;
                    break;
                }
            }

            // Copy state buffers back to the bodies
            for (var i = 0; i < _bodies.Count; ++i)
            {
                var body = _bodies[i];
                body.Sweep.CenterOfMass = _positions[i].Point;
                body.Sweep.Angle = _positions[i].Angle;
                body.LinearVelocityInternal = _velocities[i].LinearVelocity;
                body.AngularVelocityInternal = _velocities[i].AngularVelocity;
                body.SynchronizeTransform();
            }

            var minSleepTime = float.MaxValue;

            const float linTolSqr = Settings.LinearSleepTolerance * Settings.LinearSleepTolerance;
            const float angTolSqr = Settings.AngularSleepTolerance * Settings.AngularSleepTolerance;

            for (var i = 0; i < _bodies.Count; ++i)
            {
                var b = _bodies[i];
                if (b.TypeInternal == Body.BodyType.Static)
                {
                    continue;
                }

                if (!b.IsSleepAllowedInternal || b.AngularVelocityInternal * b.AngularVelocityInternal > angTolSqr ||
                    Vector2.Dot(b.LinearVelocityInternal, b.LinearVelocityInternal) > linTolSqr)
                {
                    b.SleepTime = 0.0f;
                    minSleepTime = 0.0f;
                }
                else
                {
                    b.SleepTime += h;
                    minSleepTime = System.Math.Min(minSleepTime, b.SleepTime);
                }
            }

            if (minSleepTime >= Settings.TimeToSleep && positionsSolved)
            {
                for (var i = 0; i < _bodies.Count; ++i)
                {
                    var b = _bodies[i];
                    b.IsAwake = false;
                }
            }
        }

        /// <summary>Perform continuous solve step.</summary>
        /// <param name="subStep">The sub step information.</param>
        /// <param name="toiIndexA">The island index of the first involved body.</param>
        /// <param name="toiIndexB">The island index of the second involved body.</param>
        public void SolveTOI(TimeStep subStep, int toiIndexA, int toiIndexB)
        {
            System.Diagnostics.Debug.Assert(toiIndexA < _bodies.Count);
            System.Diagnostics.Debug.Assert(toiIndexB < _bodies.Count);

            // Initialize the body state.
            for (var i = 0; i < _bodies.Count; ++i)
            {
                var b = _bodies[i];
                _positions[i].Point = b.Sweep.CenterOfMass;
                _positions[i].Angle = b.Sweep.Angle;
                _velocities[i].LinearVelocity = b.LinearVelocityInternal;
                _velocities[i].AngularVelocity = b.AngularVelocityInternal;
            }

            // Initialize solver for current step.
            _solver.Initialize(subStep);

            // Solve position constraints.
            for (var i = 0; i < Settings.PositionIterationsTOI; ++i)
            {
                if (_solver.SolveTOIPositionConstraints(toiIndexA, toiIndexB))
                {
                    break;
                }
            }

            // Leap of faith to new safe state.
            _bodies[toiIndexA].Sweep.CenterOfMass0 = _positions[toiIndexA].Point;
            _bodies[toiIndexA].Sweep.Angle0 = _positions[toiIndexA].Angle;
            _bodies[toiIndexB].Sweep.CenterOfMass0 = _positions[toiIndexB].Point;
            _bodies[toiIndexB].Sweep.Angle0 = _positions[toiIndexB].Angle;

            // No warm starting is needed for TOI events because warm
            // starting impulses were applied in the discrete solver.
            _solver.InitializeVelocityConstraints();

            // Solve velocity constraints.
            for (var i = 0; i < Settings.VelocityIterations; ++i)
            {
                _solver.SolveVelocityConstraints();
            }

            // Don't store the TOI contact forces for warm starting
            // because they can be quite large.

            var h = subStep.DeltaT;

            // Integrate positions
            for (var i = 0; i < _bodies.Count; ++i)
            {
                var c = _positions[i].Point;
                var a = _positions[i].Angle;
                var v = _velocities[i].LinearVelocity;
                var w = _velocities[i].AngularVelocity;

                // Check for large velocities
                var translation = h * v;
                if (Vector2.Dot(translation, translation) > Settings.MaxTranslationSquared)
                {
                    var ratio = Settings.MaxTranslation / translation.Length();
                    v *= ratio;
                }

                var rotation = h * w;
                if (rotation * rotation > Settings.MaxRotationSquared)
                {
                    var ratio = Settings.MaxRotation / System.Math.Abs(rotation);
                    w *= ratio;
                }

                // Integrate
                c += h * v;
                a += h * w;

                _positions[i].Point = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;

                // Sync bodies
                var body = _bodies[i];
                body.Sweep.CenterOfMass = c;
                body.Sweep.Angle = a;
                body.LinearVelocityInternal = v;
                body.AngularVelocityInternal = w;
                body.SynchronizeTransform();
            }
        }

        #endregion
    }
}
