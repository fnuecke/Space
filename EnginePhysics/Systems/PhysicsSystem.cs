using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Contacts;
using Engine.Physics.Joints;
using Engine.Physics.Messages;
using Engine.Serialization;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.Physics.Systems
{
    /// <summary>
    /// This system implements the stepping and solving logic of a physical simulation.
    /// It uses all bodies registered in the local component system. Note that no
    /// bodies/fixtures must be added or removed during an update step of this system
    /// (e.g. in collision message handlers).
    /// <para>
    /// The whole implementation is more or less a port of Box2D, adjusted to the
    /// component system architecture, and with some artistic license regarding code
    /// structuring. This architecture may indeed be slower than other more literal
    /// ports (e.g. FarSeer Physics) but the much improved usability and readability
    /// is worth it in my opinion.
    /// </para>
    /// </summary>
    [DebuggerDisplay("Bodies = {BodyCount}, Contacts = {ContactCount}")]
    public sealed class PhysicsSystem : AbstractComponentSystem<Body>, IUpdatingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Gets or sets the global gravity vector.</summary>
        public Vector2 Gravity
        {
            get { return _gravity; }
            set
            {
                Debug.Assert(!IsLocked);

                _gravity = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to allow bodies to sleep.
        /// Bodies that are asleep require nearly no CPU cycles per update,
        /// allowing for a lot better performance. You'll probably never want
        /// to disable this, unless it's for testing purposes.
        /// </summary>
        public bool AllowSleep
        {
            get { return _allowSleep; }
            set
            {
                if (value == _allowSleep)
                {
                    return;
                }

                _allowSleep = value;

                if (!_allowSleep)
                {
                    foreach (var body in Components)
                    {
                        body.IsAwake = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is locked. The system will lock
        /// itself during an update, disallowing changes to its state from the outside.
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>Gets all bodies in the simulation.</summary>
        public IEnumerable<Body> Bodies
        {
            get { return Components; }
        }

        /// <summary>Gets all active the contacts.</summary>
        public IEnumerable<Contact> Contacts
        {
            get
            {
                for (var i = _usedContacts; i >= 0; i = _contacts[i].Next)
                {
                    if (_contacts[i].IsTouching)
                    {
                        yield return _contacts[i];
                    }
                }
            }
        }

        /// <summary>Gets all joints in the simulation.</summary>
        public IEnumerable<Joint> Joints
        {
            get
            {
                for (var i = _usedJoints; i >= 0; i = _joints[i].Next)
                {
                    yield return _joints[i];
                }
            }
        }

        /// <summary>Gets the number of bodies in this simulation.</summary>
        public int BodyCount
        {
            get { return Components.Count; }
        }
        
        /// <summary>Gets the number of (active) fixtures in this simulation.</summary>
        public int FixtureCount
        {
            get { return _index.Count; }
        }

        /// <summary>Gets the number of currently active contacts in the simulation.</summary>
        public int ContactCount
        {
            get { return _contactCount; }
        }

        /// <summary>Gets the number of joints in this simulation.</summary>
        public int JointCount
        {
            get { return _jointCount; }
        }

        /// <summary>
        /// Profiling data for this simulation. This will be updated each time <see cref="Update"/>
        /// is called and will hold the time in milliseconds that different parts of the simulation
        /// took to run.
        /// </summary>
        public Profile Profile
        {
            get { return _profile; }
        }

        /// <summary>Gets the fixture bounding boxes for the debug renderer.</summary>
        internal IEnumerable<WorldBounds> FixtureBounds
        {
            get
            {
                foreach (var entry in _index)
                {
                    yield return entry.Item1;
                }
            }
        }

        #endregion

        #region Fields

        /// <summary>The settings for this simulation.</summary>
        private float _timestep = 1 / 60f;

        /// <summary>The directed global gravitational vector.</summary>
        private Vector2 _gravity = -Vector2.UnitY;

        /// <summary>Whether to allow putting bodies to sleep or not.</summary>
        private bool _allowSleep = true;

        /// <summary>
        /// The number of used contacts. This is used to reserve all the memory
        /// we might need in our islands used for solving we may need in advance.
        /// </summary>
        private int _contactCount;

        /// <summary>List of active contacts between bodies (i.e. current active collisions).</summary>
        [DeepCopy, PacketizerIgnore]
        private Contact[] _contacts = new Contact[0];

        /// <summary>List of contact edges, two per contact.</summary>
        [DeepCopy, PacketizerIgnore]
        private ContactEdge[] _contactEdges = new ContactEdge[0];

        /// <summary>The start of the linked list of used contacts.</summary>
        private int _usedContacts = -1;

        /// <summary>
        /// The start of the linked list of available contacts.
        /// It is important to note that the list of free contacts is maintained
        /// using the 'Previous' reference, to allow keeping the 'Next' reference
        /// intact when deleting contacts during interation of the active contact
        /// list.
        /// </summary>
        private int _freeContacts = -1;

        /// <summary>The number of joints in the simulation.</summary>
        private int _jointCount;

        /// <summary>List of all joints in the simulation.</summary>
        [DeepCopy, PacketizerIgnore]
        private Joint[] _joints = new Joint[0];

        /// <summary>List of joint edges, two per joint (although sometimes only one might
        /// actually be used).</summary>
        [DeepCopy, PacketizerIgnore]
        private JointEdge[] _jointEdges = new JointEdge[0];

        /// <summary>Start of the linked list of used joints.</summary>
        private int _usedJoints = -1;

        /// <summary>Start of the linked list of available joints.</summary>
        private int _freeJoints = -1;

        /// <summary>
        /// We keep track of our gear joints by mapping the joints they are attached to
        /// to the ids of the gear joints attached to them. This way we can quickly
        /// remove gear joints when one of the joints they are attached to is removed.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<int, HashSet<int>> _gearJoints = new Dictionary<int, HashSet<int>>();

        /// <summary>
        /// The index structure we use for our broad phase. We don't use the index
        /// system because we want to track the individual fixtures, not the complete
        /// body (and the index system only tracks complete entities).
        /// </summary>
#if FARMATH
        private FarCollections.SpatialHashedQuadTree<int> _index =
            new FarCollections.SpatialHashedQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier,
                (packet, i) => packet.Write(i), packet => packet.ReadInt32());
#else
        private Collections.DynamicQuadTree<int> _index =
            new Collections.DynamicQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier,
                (packet, i) => packet.Write(i), packet => packet.ReadInt32());
#endif

        /// <summary>
        /// The list of fixtures that have changed since the last update, i.e. for
        /// which we need to scan for new/lost contacts.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private ISet<int> _touched = new HashSet<int>();

        /// <summary>
        /// Tracks whether a new fixture was added since the last update. This will
        /// trigger a search for new contacts at the beginning of the next update.
        /// </summary>
        private bool _findContactsBeforeNextUpdate;

        /// <summary>Reused every update for solving simulation constraints.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Island _island;

        /// <summary>
        /// Proxies for fixtures used in time of impact computation. We keep those two
        /// instances alive to avoid producing garbage.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private Algorithms.DistanceProxy _proxyA = new Algorithms.DistanceProxy(),
                                         _proxyB = new Algorithms.DistanceProxy();

        /// <summary>
        /// Profiling data.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private Profile _profile = new Profile();

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="PhysicsSystem"/> class.</summary>
        /// <param name="timestep">The timestep.</param>
        /// <param name="gravity">The global gravity vector.</param>
        public PhysicsSystem(float timestep, Vector2 gravity)
        {
            _timestep = timestep;
            _gravity = gravity;
        }

        /// <summary>Initializes a new instance of the <see cref="PhysicsSystem"/> class.</summary>
        /// <param name="timestep">The timestep.</param>
        public PhysicsSystem(float timestep) : this(timestep, Vector2.Zero)
        {
        }

        #endregion

        #region Logic

        /// <summary>Updates the system.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            _profile.BeginStep();

            // Lock system to avoid adding/removal of bodies and fixtures and
            // modifying critical properties during an update (in message handlers).
            IsLocked = true;

            // If new fixtures were added we want to look for contacts with that
            // fixtures before jumping into the actual update.
            if (_findContactsBeforeNextUpdate)
            {
                FindContacts();
                _findContactsBeforeNextUpdate = false;
            }

            // Build our time step information.
            TimeStep step;
            step.DeltaT = _timestep;
            step.InverseDeltaT = 1.0f / step.DeltaT;

            _profile.BeginCollide();
            {
                // Check all contacts for validity (may delete some).
                UpdateContacts();
            }
            _profile.EndCollide();

            _profile.BeginSolve();
            {
                // Find islands, integrate and solve constraints.
                Solve(step);
            }
            _profile.EndSolve();
            
            _profile.BeginBroadphase();
            {
                // Synchronize fixtures, check for out of range bodies.
                // If a body was not in an island then it did not move.
                foreach (var body in _island.ProcessedBodies)
                {
                    // Update fixtures (for broad-phase).
                    body.SynchronizeFixtures();
                }

                // Checks for new contacts for changed entities.
                FindContacts();
            }
            _profile.EndBroadphase();

            _profile.BeginSolveTOI();
            {
                // Find TOI contacts and solve them.
                SolveTOI(step);
            }
            _profile.EndSolveTOI();

            // Clear forces for next iteration.
            foreach (var body in Components)
            {
                body.ClearForces();
            }

            // Done, unlock the system.
            IsLocked = false;

            _profile.EndStep();
        }

        /// <summary>
        /// Converts a point in simulation space to screen space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The point in screen space.</returns>
        public static float ToScreenUnits(float point)
        {
            return point * 100f;
        }

        /// <summary>
        /// Converts a point in simulation space to screen space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The point in screen space.</returns>
        public static Vector2 ToScreenUnits(Vector2 point)
        {
            return point * 100f;
        }

        /// <summary>
        /// Converts a point in screen space to simulation space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The point in simulation space.</returns>
        public static float ToSimulationUnits(float point)
        {
            return point * (1f / 100f);
        }

        /// <summary>
        /// Converts a point in screen space to simulation space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The point in simulation space.</returns>
        public static Vector2 ToSimulationUnits(Vector2 point)
        {
            return point * (1f / 100f);
        }

        /// <summary>
        /// Gets the fixture at the specified world point. If there are multiple
        /// fixtures (due to overlap e.g. from non-colliding groups) the first
        /// best fixture is returned.
        /// </summary>
        /// <param name="worldPoint">The world point.</param>
        /// <returns></returns>
        public Fixture GetFixtureAt(WorldPoint worldPoint)
        {
            Fixture result = null;
            _index.Find(worldPoint, 0, value =>
            {
                // Get the fixture and its body.
                var fixture = Manager.GetComponentById(value) as Fixture;
                Debug.Assert(fixture != null);
                var body = fixture.Body;

                // Check if it's an actual hit.
                if (fixture.TestPoint(body.GetLocalPoint(worldPoint)))
                {
                    // Yep, we're done.
                    result = fixture;
                    return false;
                }
                return true;
            });
            return result;
        }

        #endregion

        #region Joint accessors

        /// <summary>Determines whether the specified joint is valid/exists.</summary>
        /// <param name="jointId">The ID of the joint.</param>
        /// <returns><c>true</c> if the specified joint exists; otherwise, <c>false</c>.</returns>
        public bool HasJoint(int jointId)
        {
            return jointId >= 0 && jointId < _joints.Length && _joints[jointId].Manager != null;
        }

        /// <summary>Gets a joint by its ID.</summary>
        /// <param name="jointId">The joint id.</param>
        /// <returns>A reference to the joint with the specified ID.</returns>
        public Joint GetJointById(int jointId)
        {
            if (!HasJoint(jointId))
            {
                throw new ArgumentException("No such joint.", "jointId");
            }
            return _joints[jointId];
        }

        /// <summary>Gets all joints attached to the specified body.</summary>
        /// <param name="body">The body to check for.</param>
        /// <returns>A list of all joints attached to that body.</returns>
        public IEnumerable<Joint> GetJoints(Body body)
        {
            for (var edge = body.JointList; edge >= 0; edge = _jointEdges[edge].Next)
            {
                yield return _joints[_jointEdges[edge].Joint];
            }
        }

        #endregion

        #region Callbacks for Bodies/Fixtures

        /// <summary>Sets a flag to do a search for new contacts before next update.</summary>
        internal void FindContactsBeforeNextUpdate()
        {
            _findContactsBeforeNextUpdate = true;
        }

        /// <summary>Adds the fixtures of the specified body to the index used in the broadphase.</summary>
        /// <param name="body">The body whose fixtures to add.</param>
        internal void AddFixturesToIndex(Body body)
        {
            foreach (Fixture fixture in body.Fixtures)
            {
                _index.Add(fixture.ComputeBounds(body.Transform), fixture.Id);
                _touched.Add(fixture.Id);
            }
        }

        /// <summary>Removes the fixtures of the specified body from the index.</summary>
        /// <param name="body">The body.</param>
        internal void RemoveFixturesFromIndex(Body body)
        {
            foreach (Fixture fixture in body.Fixtures)
            {
                _index.Remove(fixture.Id);
            }
        }

        /// <summary>
        /// Marks the specified body as changed, forcing a search for new/lost
        /// contacts with this body in the next update.
        /// </summary>
        /// <param name="body">The body for which to mark the fixtures.</param>
        internal void TouchFixtures(Body body)
        {
            foreach (Fixture fixture in body.Fixtures)
            {
                _touched.Add(fixture.Id);
            }
        }

        /// <summary>
        /// Frees the contacts the specified body is involved in. This is used to
        /// invalidate contacts when a body significantly changes (e.g. its type
        /// or enabled state). It also removes the fixtures of the body from the
        /// list of changed fixtures, to avoid updating them.
        /// </summary>
        /// <param name="body">The entity for which to remove the contacts.</param>
        internal void RemoveContacts(Body body)
        {
            while (body.ContactList >= 0)
            {
                var contact = _contactEdges[body.ContactList].Contact;
                _touched.Remove(_contacts[contact].FixtureIdA);
                _touched.Remove(_contacts[contact].FixtureIdB);
                DestroyContact(contact);
            }

            // Check for contacts before the actual update next time.
            FindContactsBeforeNextUpdate();
        }

        /// <summary>Marks all contacts of the specified fixture for re-filtering.</summary>
        /// <param name="fixture">The fixture.</param>
        internal void Refilter(Fixture fixture)
        {
            for (var edge = fixture.Body.ContactList; edge >= 0; edge = _contactEdges[edge].Next)
            {
                var contact = _contacts[_contactEdges[edge].Contact];
                if (contact.FixtureIdA == fixture.Id || contact.FixtureIdB == fixture.Id)
                {
                    contact.ShouldFilter = true;
                }
            }
            // Touch fixture to allow new contact generation.
            _touched.Add(fixture.Id);
        }

        /// <summary>
        /// Updates the specified fixture's position in the index structure
        /// to the specified new bounds, within the context of the specified
        /// displacement.
        /// </summary>
        /// <param name="bounds">The new bounds of the fixture.</param>
        /// <param name="delta">How much the fixture has moved.</param>
        /// <param name="fixtureId">The id of the fixture.</param>
        internal void Synchronize(WorldBounds bounds, Vector2 delta, int fixtureId)
        {
            if (_index.Update(bounds, delta, fixtureId))
            {
                // The index changed, mark the fixture for contact updates.
                _touched.Add(fixtureId);
            }
        }

        #endregion

        #region Message handling (Body/Fixture creation/destruction)

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            var fixture = component as Fixture;
            var body = component as Body;
            if ((body != null || fixture != null) && IsLocked)
            {
                throw new InvalidOperationException("Must not add bodies, fixtures or joints during update.");
            }

            if (body != null)
            {
                if (Manager.GetComponents(body.Entity, Body.TypeId).Count() > 1)
                {
                    throw new InvalidOperationException("There must only be one body component per entity.");
                }

                // All green, we can save our base class some work by just
                // passing this on in here (because it'll check again if
                // the component is a body).
                base.OnComponentAdded(component);
            }
            else if (fixture != null)
            {
                body = fixture.Body;
                if (body == null)
                {
                    throw new InvalidOperationException("Fixtures must be added to entities that already have a body.");
                }

                if (body.Enabled)
                {
                    // Add it to our index.
                    _index.Add(fixture.ComputeBounds(body.Transform), fixture.Id);

                    // Mark it for a first contact check.
                    _touched.Add(component.Id);

                    // Make sure the body is awake.
                    body.IsAwake = true;

                    // Remember to check for new contacts before the next update.
                    FindContactsBeforeNextUpdate();
                }
            }
        }

        /// <summary>Called by the manager when a component was removed.</summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            var fixture = component as Fixture;
            var body = component as Body;
            if ((body != null || fixture != null) && IsLocked)
            {
                throw new InvalidOperationException("Must not remove bodies, fixtures or joints during update.");
            }

            if (body != null)
            {
                // Remove all joints attached to this body.
                var edge = body.JointList;
                while (edge >= 0)
                {
                    var joint = _jointEdges[edge].Joint;
                    edge = _jointEdges[edge].Next;
                    DestroyJoint(joint);
                    Debug.Assert(body.JointList == edge);
                }
                Debug.Assert(body.JointList == -1);

                // Remove all contacts in one go (fixture removal would take care
                // of that, too, but this way it's faster because we don't need to
                // search for the contacts involving the currently being removed
                // fixture).
                edge = body.ContactList;
                while (edge >= 0)
                {
                    var contact = _contactEdges[edge].Contact;
                    edge = _contactEdges[edge].Next;
                    DestroyContact(contact);
                    Debug.Assert(body.ContactList == edge);
                }
                Debug.Assert(body.ContactList == -1);

                // Remove all fixtures if a body is removed (to list because the
                // enumeration otherwise changes while being enumerated). This
                // will in turn delete all contacts with this body.
                body.Fixtures.ToList().ForEach(Manager.RemoveComponent);

                // Remove from parent list. As with the added handler, we save
                // our base class some work because it only cares for body
                // components anyway, so we just pass this on in here.
                base.OnComponentRemoved(component);
            }
            else if (fixture != null)
            {
                body = fixture.Body;

                // Remove all contacts involving the fixture.
                var edge = body.ContactList;
                while (edge >= 0)
                {
                    var contact = _contactEdges[edge].Contact;
                    edge = _contactEdges[edge].Next;
                    if (_contacts[contact].FixtureA == fixture ||
                        _contacts[contact].FixtureB == fixture)
                    {
                        DestroyContact(contact);
                    }
                }

                // Remove from index.
                _index.Remove(fixture.Id);

                // Remove it from the list of fixtures to update.
                _touched.Remove(fixture.Id);

                // Recompute the body's mass data.
                if (fixture.Density > 0)
                {
                    body.ResetMassData();
                }
            }
        }

        #endregion

        #region Contact/Joint allocation
        
        /// <summary>Make sure we have enough contacts allocated to push a new one back.</summary>
        /// <returns></returns>
        private int AllocateContact()
        {
            // When we hit the last contact, allocate some more.
            if (_freeContacts < 0)
            {
                // Remember where the new segment starts and set to new list.
                var startOfNewSegment = _contacts.Length;

                // Actual allocation and copying of old data.
                var newContacts = new Contact[_contacts.Length * 3 / 2 + 1];
                var newEdges = new ContactEdge[newContacts.Length * 2];
                _contacts.CopyTo(newContacts, 0);
                _contactEdges.CopyTo(newEdges, 0);
                _contacts = newContacts;
                _contactEdges = newEdges;

                // Initialize the new segment by making it available in the linked
                // list of available contacts.
                InitializeContacts(startOfNewSegment);
            }

            // Tentatively give out the next free joint id.
            Debug.Assert(_freeContacts >= 0);
            return _freeContacts;
        }

        /// <summary>
        /// Initializes the contact buffer starting from the specified index.
        /// This is used after the contact buffer was resized to build the
        /// linked list of free contacts.
        /// </summary>
        /// <param name="start">The starting index.</param>
        private void InitializeContacts(int start)
        {
            // Initialize all new entries.
            for (var i = start; i < _contacts.Length; i++)
            {
                Debug.Assert(_contacts[i] == null);
                _contacts[i] = new Contact
                {
                    Manager = Manager,
                    Previous = i + 1
                };
            }
            for (var i = start * 2; i < _contactEdges.Length; i++)
            {
                Debug.Assert(_contactEdges[i] == null);
                _contactEdges[i] = new ContactEdge();
            }

            // Prepend to existing list.
            _contacts[_contacts.Length - 1].Previous = -1;
            _freeContacts = start;
        }
        
        /// <summary>
        /// Updates the global contact lists by popping the next free one and
        /// appending it to the list of used contacts.
        /// </summary>
        /// <param name="contact">The contact.</param>
        private void UpdateContactList(int contact)
        {
            
            Debug.Assert(contact == _freeContacts);
            
            // Remove it from the linked list of available joints.
            _freeContacts = _contacts[contact].Previous;
            
            // Adjust global linked list.
            if (_usedContacts >= 0)
            {
                // Prepend to list.
                _contacts[_usedContacts].Previous = contact;
            }
            _contacts[contact].Next = _usedContacts;
            _contacts[contact].Previous = -1;
            _usedContacts = contact;

            // Increment counter used for island allocation.
            ++_contactCount;
        }

        /// <summary>Allocates a new contact between the two specified fixtures and initializes it.</summary>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        private void CreateContact(Fixture fixtureA, Fixture fixtureB)
        {
            // Remember index of contact we will return, then remove it from
            // the linked list of available contacts.
            var contact = AllocateContact();

            // Initialize with the basics. This may swap the fixture order.
            _contacts[contact].Initialize(ref fixtureA, ref fixtureB);

            var bodyA = fixtureA.Body;
            var bodyB = fixtureB.Body;

            var edgeA = contact * 2;
            var edgeB = contact * 2 + 1;

            Debug.Assert(bodyA.ContactList != edgeA);
            Debug.Assert(bodyB.ContactList != edgeB);

            // Set up edge from A to B.
            _contactEdges[edgeA].Contact = contact;
            _contactEdges[edgeA].Other = bodyB.Entity;
            _contactEdges[edgeA].Next = bodyA.ContactList;
            _contactEdges[edgeA].Previous = -1;

            // Adjust local linked list.
            if (bodyA.ContactList >= 0)
            {
                _contactEdges[bodyA.ContactList].Previous = edgeA;
            }
            bodyA.ContactList = edgeA;

            // Set up edge from B to A.
            _contactEdges[edgeB].Contact = contact;
            _contactEdges[edgeB].Other = bodyA.Entity;
            _contactEdges[edgeB].Next = bodyB.ContactList;
            _contactEdges[edgeB].Previous = -1;

            // Adjust local linked list.
            if (bodyB.ContactList >= 0)
            {
                _contactEdges[bodyB.ContactList].Previous = edgeB;
            }
            bodyB.ContactList = edgeB;

            // Lock it in. This is the acutal "allocation", which we want to do
            // at the very end to avoid entering an invalid state if any of the
            // initialization stuff throws an exception.
            UpdateContactList(contact);
        }

        /// <summary>Frees the specified contact.</summary>
        /// <param name="contact">The contact.</param>
        private void DestroyContact(int contact)
        {
            // Remove from global list.
            {
                var previous = _contacts[contact].Previous;
                var next = _contacts[contact].Next;
                if (previous >= 0)
                {
                    _contacts[previous].Next = next;
                }
                if (next >= 0)
                {
                    _contacts[next].Previous = previous;
                }

                // If we removed the head of our active list update it.
                if (contact == _usedContacts)
                {
                    _usedContacts = next;
                }
            }

            // Get the actual components.
            var fixtureA = _contacts[contact].FixtureA;
            var fixtureB = _contacts[contact].FixtureB;
            var bodyA = fixtureA.Body;
            var bodyB = fixtureB.Body;

            // Send message if this contact was active.
            if (_contacts[contact].IsTouching)
            {
                EndContact message;
                message.Contact = _contacts[contact];
                Manager.SendMessage(message);

                // Wake up the bodies, if there's no sensor.
                if (!fixtureA.IsSensorInternal && !fixtureB.IsSensorInternal)
                {
                    bodyA.IsAwake = true;
                    bodyB.IsAwake = true;
                }
            }

            // Remove from local edge lists.
            {
                var edgeA = contact * 2;
                var previous = _contactEdges[edgeA].Previous;
                var next = _contactEdges[edgeA].Next;
                if (previous >= 0)
                {
                    _contactEdges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _contactEdges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (bodyA.ContactList == edgeA)
                {
                    bodyA.ContactList = next;
                }
            }
            {
                var edgeB = contact * 2 + 1;
                var previous = _contactEdges[edgeB].Previous;
                var next = _contactEdges[edgeB].Next;
                if (previous >= 0)
                {
                    _contactEdges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _contactEdges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (bodyB.ContactList == edgeB)
                {
                    bodyB.ContactList = next;
                }
            }

            // Push contact back to list of free contacts.
            _contacts[contact].Previous = _freeContacts;
            _freeContacts = contact;

            // Decrement counter used for island allocation.
            --_contactCount;
        }
        
        /// <summary>Make sure we have enough joints allocated to push a new one back.</summary>
        /// <returns></returns>
        private int AllocateJoint()
        {
            // When we hit the last joint, allocate some more.
            if (_freeJoints < 0)
            {
                // Remember where the new segment starts and set to new list.
                var startOfNewSegment = _joints.Length;

                // Actual allocation and copying of old data.
                var newJoints = new Joint[_joints.Length * 3 / 2 + 1];
                var newEdges = new JointEdge[newJoints.Length * 2];
                _joints.CopyTo(newJoints, 0);
                _jointEdges.CopyTo(newEdges, 0);
                _joints = newJoints;
                _jointEdges = newEdges;

                // Initialize the new segment by making it available in the linked
                // list of available joints.
                InitializeJoints(startOfNewSegment);
            }

            // Tentatively give out the next free joint id.
            Debug.Assert(_freeJoints >= 0);
            return _freeJoints;
        }

        /// <summary>
        /// Initializes the joint buffer starting from the specified index.
        /// This is used after the joint buffer was resized to build the
        /// linked list of free contacts.
        /// </summary>
        /// <param name="start">The starting index.</param>
        private void InitializeJoints(int start)
        {
            // Initialize all new entries.
            for (var i = start; i < _joints.Length; i++)
            {
                Debug.Assert(_joints[i] == null);
                _joints[i] = new NullJoint {Previous = i + 1};
            }
            for (var i = start * 2; i < _jointEdges.Length; i++)
            {
                Debug.Assert(_jointEdges[i] == null);
                _jointEdges[i] = new JointEdge();
            }

            // Prepend to existing list.
            _joints[_joints.Length - 1].Previous = -1;
            _freeJoints = start;
        }
        
        /// <summary>
        /// Updates the global joint lists by popping the next free one and
        /// appending it to the list of used joints.
        /// </summary>
        /// <param name="joint">The joint.</param>
        private void UpdateJointList(int joint)
        {
            Debug.Assert(joint == _freeJoints);

            // Remove it from the linked list of available joints.
            _freeJoints = _joints[joint].Previous;

            // Update global list.
            if (_usedJoints >= 0)
            {
                // Prepend to list.
                _joints[_usedJoints].Previous = joint;
            }
            _joints[joint].Next = _usedJoints;
            _joints[joint].Previous = -1;
            _usedJoints = joint;

            // Increment counter used for island allocation.
            ++_jointCount;
        }

        /// <summary>Allocates a new joint attached to the two specified bodies and initializes it.</summary>
        /// <param name="type">The type of joint.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="collideConnected">Whether to collide the connected bodies.</param>
        /// <returns>The allocated joint.</returns>
        internal Joint CreateJoint(Joint.JointType type, Body bodyA = null, Body bodyB = null, bool collideConnected = true)
        {
            // We need at least one body to attach the joint to.
            Debug.Assert(bodyA != null || bodyB != null);

            // Get a new joint id.
            var joint = AllocateJoint();

            // Allocate actual instance, if what we have is the wrong type.
            Joint newJoint = null;
            if (_joints[joint].Type != type)
            {
                switch (type)
                {
                    case Joint.JointType.Revolute:
                        newJoint = new RevoluteJoint();
                        break;
                    case Joint.JointType.Prismatic:
                        newJoint = new PrismaticJoint();
                        break;
                    case Joint.JointType.Distance:
                        newJoint = new DistanceJoint();
                        break;
                    case Joint.JointType.Pulley:
                        newJoint = new PulleyJoint();
                        break;
                    case Joint.JointType.Mouse:
                        newJoint = new MouseJoint();
                        break;
                    case Joint.JointType.Gear:
                        throw new ArgumentException(
                            "Gear joints must be created using the CreateGearJoint method.", "type");
                    case Joint.JointType.Wheel:
                        newJoint = new WheelJoint();
                        break;
                    case Joint.JointType.Weld:
                        newJoint = new WeldJoint();
                        break;
                    case Joint.JointType.Friction:
                        newJoint = new FrictionJoint();
                        break;
                    case Joint.JointType.Rope:
                        newJoint = new RopeJoint();
                        break;
                    case Joint.JointType.Motor:
                        newJoint = new MotorJoint();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }
            }
            // We had to create a new joint because the type didn't match. Make
            // sure to copy the linked list data back over for proper updating
            // later / not breaking in case initialization or anything else throws.
            if (newJoint != null)
            {
                // The free list only needs the previous pointer.
                newJoint.Index = joint;
                newJoint.Previous = _joints[joint].Previous;
                _joints[joint] = newJoint;
            }

            // Initialize with the basics.
            _joints[joint].Initialize(Manager, bodyA, bodyB, collideConnected);

            // Set up edge from A to B.
            if (bodyA != null)
            {
                var edgeA = joint * 2;
                Debug.Assert(bodyA.JointList != edgeA);
                _jointEdges[edgeA].Joint = joint;
                _jointEdges[edgeA].Other = bodyB != null ? bodyB.Entity : 0;
                _jointEdges[edgeA].Next = bodyA.JointList;
                _jointEdges[edgeA].Previous = -1;

                // Adjust local linked list.
                if (bodyA.JointList >= 0)
                {
                    _jointEdges[bodyA.JointList].Previous = edgeA;
                }
                bodyA.JointList = edgeA;
            }

            // Set up edge from B to A.
            if (bodyB != null)
            {
                var edgeB = joint * 2 + 1;
                Debug.Assert(bodyB.JointList != edgeB);
                _jointEdges[edgeB].Joint = joint;
                _jointEdges[edgeB].Other = bodyA != null ? bodyA.Entity : 0;
                _jointEdges[edgeB].Next = bodyB.JointList;
                _jointEdges[edgeB].Previous = -1;

                // Adjust local linked list.
                if (bodyB.JointList >= 0)
                {
                    _jointEdges[bodyB.JointList].Previous = edgeB;
                }
                bodyB.JointList = edgeB;
            }

            // If the joint prevents collision, then mark any contacts for filtering.
            if (!collideConnected && bodyA != null && bodyB != null)
            {
                for (var edge = bodyA.ContactList; edge >= 0; edge = _contactEdges[edge].Next)
                {
                    if (_contactEdges[edge].Other == bodyB.Entity)
                    {
                        // Flag the contact for filtering at the next time step (where either
                        // body is awake).
                        _contacts[_contactEdges[edge].Other].ShouldFilter = true;
                    }
                }
            }
            
            // Lock it in. This is the acutal "allocation", which we want to do
            // at the very end to avoid entering an invalid state if any of the
            // initialization stuff throws an exception.
            UpdateJointList(joint);

            // Return the joint for further initialization.
            return _joints[joint];
        }

        /// <summary>Allocates a new gear joint and initializes it to the two speified joints.</summary>
        /// <param name="jointA">The first joint.</param>
        /// <param name="jointB">The second joint.</param>
        /// <param name="ratio">The gear ratio.</param>
        /// <returns>The new gear joint.</returns>
        internal GearJoint CreateGearJoint(Joint jointA, Joint jointB, float ratio)
        {
            ValidateJoint(jointA);
            ValidateJoint(jointB);
            int jointIdA = jointA.Index, jointIdB = jointB.Index;

            // Get a free joint id.
            var joint = AllocateJoint();

            // Create the joint if necessary and store it.
            var gearJoint = _joints[joint] as GearJoint;
            if (gearJoint == null)
            {
                // Make sure we keep the linked list pointer valid.
                _joints[joint] = gearJoint = new GearJoint
                {
                    Index = joint,
                    Previous = _joints[joint].Previous
                };
            }

            // Initialize with the basics.
            gearJoint.Initialize(Manager, jointA, jointB, ratio);

            // Keep track of our gears so the user doesn't have to remove them manually.
            if (!_gearJoints.ContainsKey(jointIdA))
            {
                _gearJoints[jointIdA] = new HashSet<int>();
            }
            _gearJoints[jointIdA].Add(joint);
            if (!_gearJoints.ContainsKey(jointIdB))
            {
                _gearJoints[jointIdB] = new HashSet<int>();
            }
            _gearJoints[jointIdB].Add(joint);
            
            // Lock it in. This is the acutal "allocation", which we want to do
            // at the very end to avoid entering an invalid state if any of the
            // initialization stuff throws an exception.
            UpdateJointList(joint);

            // Return the joint for further initialization.
            return gearJoint;
        }

        /// <summary>Destroys the joint between the two specified joints.</summary>
        /// <param name="joint">The joint to remove.</param>
        internal void DestroyJoint(Joint joint)
        {
            ValidateJoint(joint);
            DestroyJoint(joint.Index);
        }

        /// <summary>Frees the specified joint.</summary>
        /// <param name="joint">The contact.</param>
        private void DestroyJoint(int joint)
        {
            // Remove from global list.
            {
                var previous = _joints[joint].Previous;
                var next = _joints[joint].Next;
                if (previous >= 0)
                {
                    _joints[previous].Next = next;
                }
                if (next >= 0)
                {
                    _joints[next].Previous = previous;
                }

                // If we removed the head of our active list update it.
                if (joint == _usedJoints)
                {
                    _usedJoints = next;
                }
            }

            // Push contact back to list of free contacts.
            _joints[joint].Previous = _freeJoints;
            _joints[joint].Next = -1;
            _freeJoints = joint;

            // Decrement counter used for island allocation.
            --_jointCount;

            // Depending on whether this is a gear joint or not we proceed differently.
            // This is because gear joints don't have edges and no "own" bodies.
            if (_joints[joint] is GearJoint)
            {
                // Clean up gear tracking data, remove it from the mapping.
                foreach (var gearJoints in _gearJoints)
                {
                    gearJoints.Value.Remove(joint);
                }
            }
            else
            {
                // Get the actual components.
                var bodyA = _joints[joint].BodyA;
                var bodyB = _joints[joint].BodyB;

                // Wake up the first body and update the local edge lists.
                if (bodyA != null)
                {
                    bodyA.IsAwake = true;
                    var edgeA = joint * 2;
                    var previous = _jointEdges[edgeA].Previous;
                    var next = _jointEdges[edgeA].Next;
                    if (previous >= 0)
                    {
                        _jointEdges[previous].Next = next;
                    }
                    if (next >= 0)
                    {
                        _jointEdges[next].Previous = previous;
                    }
                    // Adjust list pointer as necessary.
                    if (bodyA.JointList == edgeA)
                    {
                        bodyA.JointList = next;
                    }
                }

                // Wake up the second body and update the local edge lists.
                if (bodyB != null)
                {
                    bodyB.IsAwake = true;

                    var edgeB = joint * 2 + 1;
                    var previous = _jointEdges[edgeB].Previous;
                    var next = _jointEdges[edgeB].Next;
                    if (previous >= 0)
                    {
                        _jointEdges[previous].Next = next;
                    }
                    if (next >= 0)
                    {
                        _jointEdges[next].Previous = previous;
                    }
                    // Adjust list pointer as necessary.
                    if (bodyB.JointList == edgeB)
                    {
                        bodyB.JointList = next;
                    }
                }

                // If the joint prevented collision then mark any contacts for refiltering.
                if (!_joints[joint].CollideConnected && bodyA != null && bodyB != null)
                {
                    for (var edge = bodyA.ContactList; edge >= 0; edge = _contactEdges[edge].Next)
                    {
                        if (_contactEdges[edge].Other == bodyB.Entity)
                        {
                            // Flag the contact for filtering at the next time step (where either
                            // body is awake).
                            _contacts[_contactEdges[edge].Other].ShouldFilter = true;
                        }
                    }
                }

                // See if there were any gears attached to this joint, and if so
                // remove those gear joints from the simulation.
                if (_gearJoints.ContainsKey(joint))
                {
                    var gears = _gearJoints[joint];
                    _gearJoints.Remove(joint);
                    foreach (var gear in gears)
                    {
                        DestroyJoint(gear);
                    }
                }
            }
        }

        /// <summary>
        /// Validates the specified joint, checking if its in the system and it's ID is valud.
        /// </summary>
        /// <param name="joint">The joint to check.</param>
        private void ValidateJoint(Joint joint)
        {
            // Make sure the joint is in the simulation.
            if (joint.Manager == null)
            {
                throw new ArgumentException("Joint is not in the simulation.", "joint");
            }

            // Make sure the joint is valid.
            if (joint.Index < 0 || joint.Index >= _joints.Length || _joints[joint.Index] != joint)
            {
                throw new ArgumentException("Bad joint.", "joint");
            }
        }

        /// <summary>
        /// This is a dummy joint implementation, used to fill the linked list
        /// of free joints.
        /// </summary>
        private sealed class NullJoint : Joint
        {
            public NullJoint() : base(JointType.None)
            {
            }

            public override WorldPoint AnchorA
            {
                get { throw new NotSupportedException(); }
            }

            public override WorldPoint AnchorB
            {
                get { throw new NotSupportedException(); }
            }

            internal override void InitializeVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
            {
                throw new NotSupportedException();
            }

            internal override void SolveVelocityConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
            {
                throw new NotSupportedException();
            }

            internal override bool SolvePositionConstraints(TimeStep step, Position[] positions, Velocity[] velocities)
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Contact updates

        /// <summary>
        /// Updates all contacts, checking if they're still valid and if they
        /// result in actual intersections.
        /// </summary>
        private void UpdateContacts()
        {
            // Check the list of contacts for actual collisions.
            for (var contactId = _usedContacts; contactId >= 0;)
            {
                var contact = _contacts[contactId];

                // Get the actual components.
                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                // Get the actual collidable information for more filtering.
                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;

                Debug.Assert(bodyA.Enabled);
                Debug.Assert(bodyB.Enabled);

                // Is this contact flagged for filtering?
                if (contact.ShouldFilter)
                {
                    // See if the collision is still valid.
                    if (// Don't collide non-dynamic bodies against each other.
                        (bodyA.TypeInternal != Body.BodyType.Dynamic && bodyB.TypeInternal != Body.BodyType.Dynamic) ||
                        // Things that share at least one group do not collide.
                        (fixtureA.CollisionGroupsInternal & fixtureB.CollisionGroupsInternal) != 0 ||
                        // See if we have any joints that prevent collision.
                        JointSupressesCollision(bodyA.JointList, bodyB.Entity))
                    {
                        // No longer valid, free this contact.
                        var oldContactId = contactId;
                        contactId = contact.Next;
                        DestroyContact(oldContactId);
                        continue;
                    }

                    contact.ShouldFilter = false;
                }

                // Skip contacts for sleeping or static bodies and keep them
                // alive (until the non-static body wakes up -- there are no
                // contacts between two static bodies).
                var activeA = bodyA.IsAwake && bodyA.TypeInternal != Body.BodyType.Static;
                var activeB = bodyB.IsAwake && bodyB.TypeInternal != Body.BodyType.Static;
                if (!activeA && !activeB)
                {
                    // Continue with next contact.
                    contactId = contact.Next;
                    continue;
                }

                // Test for actual collision. First check via the index.
                if (!_index[fixtureA.Id].Intersects(_index[fixtureB.Id]))
                {
                    // Contact stopped being valid, free it.
                    var oldContact = contactId;
                    contactId = contact.Next;
                    DestroyContact(oldContact);
                    continue;
                }

                // Contact is still valid, continue with narrow phase, which tests
                // for detailed collision based on the shapes of the fixtures, and
                // computes the contact manifold. We pass the proxy singletons along
                // case we have sensors, which use the distance function to test for
                // overlap. This way we don't have to keep static instances around
                // yet also don't have to create new instance all the time -- which
                // we want to avoid to allow running multiple simulations in parallel
                // on different threads for example.
                contact.Update(fixtureA, fixtureB, bodyA, bodyB, _proxyA, _proxyB);

                // Continue with next contact.
                contactId = contact.Next;
            }
        }

        /// <summary>
        /// Checks for new contacts for all entities that moved significantly in the index.
        /// </summary>
        private void FindContacts()
        {
            // Check the list of entities that moved in the index.
            ISet<int> neighbors = new HashSet<int>();
            foreach (var fixture in _touched)
            {
                // Find contacts (possible collisions based on fattened bounds).
                _index.Find(_index[fixture], neighbors);
                foreach (var neighbor in neighbors)
                {
                    // Skip self collision.
                    if (neighbor == fixture)
                    {
                        continue;
                    }

                    // Get sorted ids of involved fixtures.
                    var fixtureIdA = System.Math.Min(fixture, neighbor);
                    var fixtureIdB = System.Math.Max(fixture, neighbor);

                    // Get the actual components.
                    var fixtureA = Manager.GetComponentById(fixtureIdA) as Fixture;
                    var fixtureB = Manager.GetComponentById(fixtureIdB) as Fixture;

                    Debug.Assert(fixtureA != null);
                    Debug.Assert(fixtureB != null);

                    // Get the actual collidable information for more filtering.
                    var bodyA = fixtureA.Body;
                    var bodyB = fixtureB.Body;

                    // Skip if fixtures belong to the same body.
                    if (bodyA.Id == bodyB.Id)
                    {
                        continue;
                    }

                    // Skip if not enabled.
                    if (!bodyA.Enabled || !bodyB.Enabled)
                    {
                        continue;
                    }

                    // See if the two bodies should collide.
                    if (// Don't collide non-dynamic bodies against each other.
                        (bodyA.TypeInternal != Body.BodyType.Dynamic && bodyB.TypeInternal != Body.BodyType.Dynamic) ||
                        // Things that share at least one group do not collide.
                        (fixtureA.CollisionGroupsInternal & fixtureB.CollisionGroupsInternal) != 0 ||
                        // See if we have any joints that prevent collision.
                        JointSupressesCollision(bodyA.JointList, bodyB.Entity))
                    {
                        continue;
                    }

                    // Check if the contact is already known.
                    if (ContactExists(bodyA.ContactList, fixtureIdA, fixtureIdB, bodyB.Entity))
                    {
                        continue;
                    }

                    // Not known, create new contact.
                    CreateContact(fixtureA, fixtureB);

                    // Make sure the two involved bodies are awake if there's no sensor.
                    if (!fixtureA.IsSensorInternal && !fixtureB.IsSensorInternal)
                    {
                        bodyA.IsAwake = true;
                        bodyB.IsAwake = true;
                    }
                }
                neighbors.Clear();
            }
            _touched.Clear();
        }

        /// <summary>
        /// Checks if a contact exists in the specified contact edge list for the
        /// two specified entities. The entity ids are expected to be sorted (i.e.
        /// entityA &lt; entityB).
        /// </summary>
        /// <param name="edgeList">The edge list offset of a fixture.</param>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        /// <param name="entityB">The second entity.</param>
        /// <returns></returns>
        private bool ContactExists(int edgeList, int fixtureA, int fixtureB, int entityB)
        {
            for (var i = edgeList; i >= 0; i = _contactEdges[i].Next)
            {
                // Only consider edges connected to the other body.
                if (_contactEdges[i].Other != entityB)
                {
                    continue;
                }

                // Get the actual contact for that edge.
                var contact = _contacts[_contactEdges[i].Contact];

                // Compare ids of involved fixtures to check if the contact
                // represents the one we're create now.
                if (contact.FixtureIdA == fixtureA &&
                    contact.FixtureIdB == fixtureB)
                {
                    return true;
                }
                if (contact.FixtureIdA == fixtureB &&
                    contact.FixtureIdB == fixtureA)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any joint on a body (given its joint list) suppresses
        /// collision with the specified other body.
        /// </summary>
        /// <param name="jointList">The joint list on the body.</param>
        /// <param name="other">The entity id of the other body.</param>
        /// <returns><c>true</c> if the collision should be suppressed.</returns>
        private bool JointSupressesCollision(int jointList, int other)
        {
            for (var edge = jointList; edge >= 0; edge = _jointEdges[edge].Next)
            {
                if (_jointEdges[edge].Other == other &&
                    !_joints[_jointEdges[edge].Joint].CollideConnected)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Solver logic

        /// <summary>
        /// Advances the simulation by finding islands (graph defined by bodies that
        /// have connections between them) and solving the collisions between the
        /// individual bodies. This uses the normal island solver.
        /// </summary>
        private void Solve(TimeStep step)
        {
            // Make sure we have an island (may be lost during copying).
            if (_island == null)
            {
                _island = new Island(_profile);
            }

            // Size the island for the worst case.
            _island.Reset(Components.Count, _contactCount, _jointCount);

            // Build and simulate all awake islands.
            var stack = new Stack<Body>(Components.Count);
            // We explicitly want to traverse the body list backwards,
            // to somewhat emulate Box2D's behavior (because the component
            // list is sorted by the individual components' ids this will
            // fall apart after a couple of deletions, but some tests won't
            // work right otherwise (e.g. sphere stack, where the order does
            // in fact matter, because the solver otherwise has to solve the
            // stack bottom to top, resulting in a very slow solving process).
            for (var seedIndex = Components.Count - 1; seedIndex >= 0; --seedIndex)
            {
                var seed = Components[seedIndex];
                // Skip disabled, sleeping, already processed and static bodies.
                if (!seed.Enabled ||
                    !seed.IsAwake ||
                    _island.IsProcessed(seed) ||
                    seed.TypeInternal == Body.BodyType.Static)
                {
                    continue;
                }

                // Reset island and stack.
                _island.Clear();
                stack.Clear();

                // Push seed to the stack.
                stack.Push(seed);
                _island.MarkProcessed(seed);

                // Perform a depth first search (DFS) on the constraint graph.
                while (stack.Count > 0)
                {
                    // Grab the next body off the stack.
                    var body = stack.Pop();

                    // Enabled: checked for seed and disabled bodies should not have contacts.
                    Debug.Assert(body.Enabled);

                    // And add it to the island.
                    _island.Add(body);

                    // Make sure the body is awake.
                    body.IsAwake = true;

                    // To keep islands as small as possible, we don't
                    // propagate islands across static bodies.
                    if (body.TypeInternal == Body.BodyType.Static)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body.
                    for (var edge = body.ContactList; edge >= 0; edge = _contactEdges[edge].Next)
                    {
                        // Get the actual contact for that edge.
                        var contact = _contacts[_contactEdges[edge].Contact];

                        // Skip disabled, non-touching and already processed contacts.
                        if (!contact.IsEnabled ||
                            !contact.IsTouching ||
                            _island.IsProcessed(contact))
                        {
                            continue;
                        }

                        // Skip sensors.
                        if (contact.FixtureA.IsSensorInternal ||
                            contact.FixtureB.IsSensorInternal)
                        {
                            continue;
                        }

                        // Add the contact to the island.
                        _island.Add(contact);
                        _island.MarkProcessed(contact);

                        // Get the other party involved in this contact.
                        var other = Manager.GetComponent(_contactEdges[edge].Other, Body.TypeId) as Body;

                        Debug.Assert(other != null);

                        // Was the other body already added to this island?
                        if (_island.IsProcessed(other))
                        {
                            continue;
                        }

                        // OK, mark it for processing and flag it as being in an island.
                        stack.Push(other);
                        _island.MarkProcessed(other);
                    }

                    // Search all joints attached to this body.
                    for (var edge = body.JointList; edge >= 0; edge = _jointEdges[edge].Next)
                    {
                        // Get the actual joint for that edge.
                        var joint = _joints[_jointEdges[edge].Joint];

                        // Check if we already handled this one.
                        if (_island.IsProcessed(joint))
                        {
                            continue;
                        }

                        // See if there is something on the other side.
                        if (_jointEdges[edge].Other > 0)
                        {
                            // Get the other body this contact is attached to.
                            var other = Manager.GetComponent(_jointEdges[edge].Other, Body.TypeId) as Body;

                            Debug.Assert(other != null);

                            // Don't simulate joints connected to inactive bodies.
                            if (!other.Enabled)
                            {
                                continue;
                            }

                            // Add the joint to the island.
                            _island.Add(joint);
                            _island.MarkProcessed(joint);

                            // Was the other body already added to this island?
                            if (_island.IsProcessed(other))
                            {
                                continue;
                            }

                            // OK, mark it for processing and flag it as being in an island.
                            stack.Push(other);
                            _island.MarkProcessed(other);
                        }
                        else
                        {
                            // Otherwise we just add the joint, as it's only attached
                            // to one body (e.g. stuff that has a connection to the
                            // world on the other side).
                            _island.Add(joint);
                            _island.MarkProcessed(joint);
                        }
                    }
                }

                // Done building our island, solve it.
                _island.Solve(step, _gravity, _allowSleep);

                // Post solve cleanup: allow static bodies to participate in other islands.
                _island.UnmarkStaticBodies();
            }
        }

        /// <summary>
        /// Post-processing step after the simple solver, checking for possibly skipped
        /// collosions (tunneling effect).
        /// </summary>
        private void SolveTOI(TimeStep step)
        {
            // Resize island; we limit the number of considered contacts and bodies
            // to avoid excessive substepping.
            _island.Reset(2 * Settings.MaxTOIContacts, Settings.MaxTOIContacts, 0);

            foreach (var body in Components)
            {
                body.Sweep.Alpha0 = 0;
            }

            for (var i = _usedContacts; i >= 0; i = _contacts[i].Next)
            {
                var contact = _contacts[i];
                // Invalidate TOI
                contact.HasCachedTOI = false;
                contact.ToiCount = 0;
                contact.TOI = 1;
            }

            // Find TOI events and solve them.
            for (;;)
            {
                // Find the first TOI.
                Contact minContact = null;
                var minAlpha = 1f;

                for (var i = _usedContacts; i >= 0; i = _contacts[i].Next)
                {
                    // Get reference to the contact.
                    var c = _contacts[i];

                    // Is this contact disabled?
                    if (!c.IsEnabled)
                    {
                        continue;
                    }

                    // Prevent excessive sub-stepping.
                    if (c.ToiCount > Settings.MaxSubSteps)
                    {
                        continue;
                    }

                    float alpha;
                    if (c.HasCachedTOI)
                    {
                        // This contact has a valid cached TOI.
                        alpha = c.TOI;
                    }
                    else
                    {
                        // Get actual fixture instances to get the actual bodies.
                        var fA = c.FixtureA;
                        var fB = c.FixtureB;

                        // Skip sensors.
                        if (fA.IsSensorInternal || fB.IsSensorInternal)
                        {
                            continue;
                        }

                        var bA = fA.Body;
                        var bB = fB.Body;

                        var typeA = bA.TypeInternal;
                        var typeB = bB.TypeInternal;

                        Debug.Assert(typeA == Body.BodyType.Dynamic || typeB == Body.BodyType.Dynamic);

                        // Is at least one body active (awake and dynamic or kinematic)?
                        var activeA = bA.IsAwakeInternal && typeA != Body.BodyType.Static;
                        var activeB = bB.IsAwakeInternal && typeB != Body.BodyType.Static;
                        if (!activeA && !activeB)
                        {
                            continue;
                        }

                        // Are these two non-bullet dynamic bodies?
                        var collideA = bA.IsBulletInternal || typeA != Body.BodyType.Dynamic;
                        var collideB = bB.IsBulletInternal || typeB != Body.BodyType.Dynamic;
                        if (!collideA && !collideB)
                        {
                            continue;
                        }

                        // Compute the TOI for this contact.
                        // Put the sweeps onto the same time interval.
                        float alpha0;
                        if (bA.Sweep.Alpha0 < bB.Sweep.Alpha0)
                        {
                            alpha0 = bB.Sweep.Alpha0;
                            bA.Sweep.Advance(alpha0);
                        }
                        else if (bB.Sweep.Alpha0 < bA.Sweep.Alpha0)
                        {
                            alpha0 = bA.Sweep.Alpha0;
                            bB.Sweep.Advance(alpha0);
                        }
                        else // Already equal.
                        {
                            alpha0 = bA.Sweep.Alpha0;
                        }

                        Debug.Assert(alpha0 < 1.0f);

                        // Compute the time of impact in interval [0, minTOI].
                        _proxyA.Set(fA);
                        _proxyB.Set(fB);
                        // Beta is the fraction of the remaining portion of the overlap.
                        float beta;
                        if (Algorithms.TimeOfImpact(_proxyA, _proxyB, bA.Sweep, bB.Sweep, 1.0f, out beta))
                        {
                            alpha = System.Math.Min(alpha0 + (1.0f - alpha0) * beta, 1.0f);
                        }
                        else
                        {
                            alpha = 1.0f;
                        }

                        // Cache the time of impact for this contact.
                        c.TOI = alpha;
                        c.HasCachedTOI = true;
                    }

                    if (alpha < minAlpha)
                    {
                        // This is the minimum TOI found so far.
                        minContact = c;
                        minAlpha = alpha;
                    }
                }

                if (minContact == null || 1.0f - 10.0f * Settings.Epsilon < minAlpha)
                {
                    // No more TOI events. Done!
                    break;
                }

                {
                    // Advance the bodies to the TOI.
                    var fA = minContact.FixtureA;
                    var fB = minContact.FixtureB;

                    var bA = fA.Body;
                    var bB = fB.Body;

                    var backupA = bA.Sweep;
                    var backupB = bB.Sweep;

                    bA.Advance(minAlpha);
                    bB.Advance(minAlpha);

                    // The TOI contact likely has some new contact points.
                    minContact.Update(fA, fB, bA, bB, _proxyA, _proxyB);
                    minContact.HasCachedTOI = false;
                    ++minContact.ToiCount;

                    // Is the contact solid?
                    if (!minContact.IsEnabled || !minContact.IsTouching)
                    {
                        // Restore the sweeps.
                        minContact.IsEnabled = false;
                        bA.Sweep = backupA;
                        bB.Sweep = backupB;
                        bA.SynchronizeTransform();
                        bB.SynchronizeTransform();
                        continue;
                    }

                    bA.IsAwake = true;
                    bB.IsAwake = true;

                    // Build the island
                    _island.Clear();
                    _island.Add(bA);
                    _island.Add(bB);
                    _island.Add(minContact);

                    _island.MarkProcessed(bA);
                    _island.MarkProcessed(bB);
                    _island.MarkProcessed(minContact);

                    // Get contacts on bodyA and bodyB.
                    AddConnectedBodiesForTOI(bA, minAlpha);
                    if (!_island.IsFull) // can save some component lookup work
                    {
                        AddConnectedBodiesForTOI(bB, minAlpha);
                    }

                    TimeStep subStep;
                    subStep.DeltaT = (1.0f - minAlpha) * step.DeltaT;
                    subStep.InverseDeltaT = 1.0f / subStep.DeltaT;
                    _island.SolveTOI(subStep, bA.IslandIndex, bB.IslandIndex);

                    // Reset island flags and synchronize broad-phase proxies.
                    _island.UnmarkAllBodies();
                    foreach (var body in _island.Bodies)
                    {
                        // If it's not a dynamic body, we didn't move it.
                        if (body.TypeInternal != Body.BodyType.Dynamic)
                        {
                            continue;
                        }

                        body.SynchronizeFixtures();

                        // Invalidate all contact TOIs on this displaced body.
                        for (var edge = body.ContactList; edge >= 0; edge = _contactEdges[edge].Next)
                        {
                            var contact = _contacts[_contactEdges[edge].Contact];
                            contact.HasCachedTOI = false;
                            _island.UnmarkContact(contact);
                        }
                    }

                    // Commit fixture proxy movements to the broad-phase so that new contacts are created.
                    // Also, some contacts can be destroyed.
                    FindContacts();
                }
            }
        }

        /// <summary>
        /// Utility method checking all contact edges of a body for other bodies.
        /// </summary>
        private void AddConnectedBodiesForTOI(Body body, float minAlpha)
        {
            // We only handle TOI from the dynamic side.
            if (body.TypeInternal != Body.BodyType.Dynamic)
            {
                return;
            }

            // Loop the list of contact edges of the body.
            for (var i = body.ContactList; i >= 0; i = _contactEdges[i].Next)
            {
                // Stop if we have reached our limits.
                if (_island.IsFull)
                {
                    return;
                }

                var edge = _contactEdges[i];

                // Get the actual contact for that edge.
                var contact = _contacts[edge.Contact];

                // Has this contact already been added to the island?
                if (_island.IsProcessed(contact))
                {
                    continue;
                }

                // Get the other party involved in this contact.
                var other = Manager.GetComponent(edge.Other, Body.TypeId) as Body;

                Debug.Assert(other != null);
                Debug.Assert(other.Enabled, "Contact to disabled body.");

                // Only add static, kinematic, or bullet bodies.
                if (other.TypeInternal == Body.BodyType.Dynamic &&
                    !body.IsBullet && !other.IsBullet)
                {
                    continue;
                }

                var fA = contact.FixtureA;
                var fB = contact.FixtureB;

                // Skip sensors.
                if (fA.IsSensorInternal || fB.IsSensorInternal)
                {
                    continue;
                }

                // Tentatively advance the body to the TOI.
                var backup = other.Sweep;
                if (!_island.IsProcessed(other))
                {
                    other.Advance(minAlpha);
                }

                // Update the contact points.
                contact.Update(fA, fB, fA.Body, fB.Body, _proxyA, _proxyB);

                // Was the contact disabled by the user?
                if (!contact.IsEnabled)
                {
                    other.Sweep = backup;
                    other.SynchronizeTransform();
                    continue;
                }

                // Are there contact points?
                if (!contact.IsTouching)
                {
                    other.Sweep = backup;
                    other.SynchronizeTransform();
                    continue;
                }

                // Add the contact to the island
                _island.Add(contact);
                _island.MarkProcessed(contact);

                // Has the other body already been added to the island?
                if (_island.IsProcessed(other))
                {
                    continue;
                }

                // Wake it up if necessary.
                if (other.TypeInternal != Body.BodyType.Static)
                {
                    other.IsAwake = true;
                }

                // Add the other body to the island.
                _island.Add(other);
                _island.MarkProcessed(other);
            }
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
            Debug.Assert(!IsLocked);

            base.Packetize(packet);

            packet.Write(_contacts.Length);
            for (var contact = _usedContacts; contact >= 0; contact = _contacts[contact].Next)
            {
                packet.Write(contact);
                packet.Write(_contacts[contact]);
                packet.Write(_contactEdges[contact * 2]);
                packet.Write(_contactEdges[contact * 2 + 1]);
            }

            packet.Write(_joints.Length);
            for (var joint = _usedJoints; joint >= 0; joint = _joints[joint].Next)
            {
                packet.Write(joint);
                packet.WriteWithTypeInfo(_joints[joint]);
                packet.Write(_jointEdges[joint * 2]);
                packet.Write(_jointEdges[joint * 2 + 1]);
            }

            packet.Write(_gearJoints.Count);
            foreach (var pair in _gearJoints)
            {
                packet.Write(pair.Key);
                packet.Write(pair.Value.Count);
                foreach (var gearJoint in pair.Value)
                {
                    packet.Write(gearJoint);
                }
            }

            packet.Write(_touched.Count);
            foreach (var entry in _touched)
            {
                packet.Write(entry);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _contacts = new Contact[packet.ReadInt32()];
            _contactEdges = new ContactEdge[_contacts.Length * 2];
            for (var i = 0; i < _contactCount; ++i)
            {
                var contact = packet.ReadInt32();
                _contacts[contact] = packet.ReadPacketizable<Contact>();
                _contacts[contact].Manager = Manager;
                _contactEdges[contact * 2] = packet.ReadPacketizable<ContactEdge>();
                _contactEdges[contact * 2 + 1] = packet.ReadPacketizable<ContactEdge>();
            }
            _freeContacts = -1;
            for (var contact = 0; contact < _contacts.Length; contact++)
            {
                if (_contacts[contact] == null)
                {
                    _contacts[contact] = new Contact
                    {
                        Manager = Manager,
                        Previous = _freeContacts
                    };
                    _contactEdges[contact * 2] = new ContactEdge();
                    _contactEdges[contact * 2 + 1] = new ContactEdge();
                    _freeContacts = contact;
                }
            }

            _joints = new Joint[packet.ReadInt32()];
            _jointEdges = new JointEdge[_joints.Length * 2];
            for (var i = 0; i < _jointCount; ++i)
            {
                var joint = packet.ReadInt32();
                _joints[joint] = packet.ReadPacketizableWithTypeInfo<Joint>();
                _joints[joint].Manager = Manager;
                _jointEdges[joint * 2] = packet.ReadPacketizable<JointEdge>();
                _jointEdges[joint * 2 + 1] = packet.ReadPacketizable<JointEdge>();
            }
            _freeJoints = -1;
            for (var joint = 0; joint < _joints.Length; joint++)
            {
                if (_joints[joint] == null)
                {
                    _joints[joint] = new NullJoint
                    {
                        Manager = Manager,
                        Previous = _freeJoints
                    };
                    _jointEdges[joint * 2] = new JointEdge();
                    _jointEdges[joint * 2 + 1] = new JointEdge();
                    _freeJoints = joint;
                }
            }

            _gearJoints.Clear();
            var jointWithGearCount = packet.ReadInt32();
            for (var i = 0; i < jointWithGearCount; i++)
            {
                var joint = packet.ReadInt32();
                var gearCount = packet.ReadInt32();
                var gears = new HashSet<int>();
                for (var j = 0; j < gearCount; j++)
                {
                    gears.Add(packet.ReadInt32());
                }
                _gearJoints.Add(joint, gears);
            }

            _touched.Clear();
            var touchedCount = packet.ReadInt32();
            for (var i = 0; i < touchedCount; ++i)
            {
                _touched.Add(packet.ReadInt32());
            }

            _island = null;
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            Debug.Assert(!IsLocked);

            base.Hash(hasher);

            hasher.Put(_timestep);
            hasher.Put(_gravity);

            hasher.Put(_contactCount);
            for (var contact = _usedContacts; contact >= 0; contact = _contacts[contact].Next)
            {
                _contacts[contact].Hash(hasher);
                _contactEdges[contact * 2].Hash(hasher);
                _contactEdges[contact * 2 + 1].Hash(hasher);
            }

            hasher.Put(_jointCount);
            for (var joint = _usedJoints; joint >= 0; joint = _joints[joint].Next)
            {
                _joints[joint].Hash(hasher);
                _jointEdges[joint * 2].Hash(hasher);
                _jointEdges[joint * 2 + 1].Hash(hasher);
            }

            hasher.Put(_index.Count);
            hasher.Put(_touched.Count);
            hasher.Put(_findContactsBeforeNextUpdate);
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable
        /// references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            Debug.Assert(!IsLocked);

            var copy = (PhysicsSystem)base.NewInstance();

            copy._contacts = new Contact[0];
            copy._contactEdges = new ContactEdge[0];

            copy._joints = new Joint[0];
            copy._jointEdges = new JointEdge[0];

            copy._gearJoints = new Dictionary<int, HashSet<int>>();

            copy._index = _index.NewInstance();
            copy._touched = new HashSet<int>();

            copy._island = null;
            copy._proxyA = new Algorithms.DistanceProxy();
            copy._proxyB = new Algorithms.DistanceProxy();

            copy._profile = new Profile();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the same type.
        /// <para>
        /// This clones any contained data types to return an instance that represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            Debug.Assert(!IsLocked);

            base.CopyInto(into);

            var copy = (PhysicsSystem)into;

            for (var i = 0; i < copy._contacts.Length; ++i)
            {
                copy._contacts[i].Manager = copy.Manager;
            }

            for (var i = 0; i < copy._joints.Length; ++i)
            {
                if (_joints[i].Manager != null)
                {
                    copy._joints[i].Manager = copy.Manager;
                }
            }

            copy._gearJoints.Clear();
            foreach (var gearJoints in _gearJoints)
            {
                var gears = new HashSet<int>();
                gears.UnionWith(gearJoints.Value);
                copy._gearJoints.Add(gearJoints.Key, gears);
            }

            copy._touched.Clear();
            copy._touched.UnionWith(_touched);
        }

        #endregion

        #region ToString

        // TODO

        #endregion
    }

    /// <summary>
    /// Some helper methods to access joints via the <see cref="IManager"/>
    /// to have a common place for everything.
    /// </summary>
    public static class JointManagerExtensions
    {
        /// <summary>Determines whether the specified joint is valid/exists.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="jointId">The ID of the joint.</param>
        /// <returns><c>true</c> if the specified joint exists in the manager's context; otherwise, <c>false</c>.</returns>
        public static bool HasJoint(this IManager manager, int jointId)
        {
            return manager.GetSimulation().HasJoint(jointId);
        }

        /// <summary>Gets a joint by its ID.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="jointId">The joint id.</param>
        /// <returns>A reference to the joint with the specified ID.</returns>
        public static Joint GetJointById(this IManager manager, int jointId)
        {
            return manager.GetSimulation().GetJointById(jointId);
        }

        /// <summary>Gets all joints attached to the body with the specified entity ID.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="bodyId">The ID of the entity the body belongs to.</param>
        /// <returns>A list of all joints attached to that body.</returns>
        public static IEnumerable<Joint> GetJoints(this IManager manager, int bodyId)
        {
            var body = manager.GetComponent(bodyId, Body.TypeId) as Body;
            if (body == null)
            {
                throw new ArgumentException("The specified entity is not a body.", "bodyId");
            }
            return manager.GetSimulation().GetJoints(body);
        }

        /// <summary>Gets all joints attached to the specified body.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="body">The body to check for.</param>
        /// <returns>A list of all joints attached to that body.</returns>
        public static IEnumerable<Joint> GetJoints(this IManager manager, Body body)
        {
            return manager.GetSimulation().GetJoints(body);
        }

        /// <summary>Removes the specified joint from the simulation.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="joint">The joint to remove.</param>
        public static void RemoveJoint(this IManager manager, Joint joint)
        {
            joint.Destroy();
        }

        /// <summary>Removes the joint with the specified id from the simulation.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="jointId">The joint id.</param>
        public static void RemoveJoint(this IManager manager, int jointId)
        {
            manager.GetJointById(jointId).Destroy();
        }

        /// <summary>Gets the simulation for the specified manager.</summary>
        private static PhysicsSystem GetSimulation(this IManager manager)
        {
            return manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
        }
    }
}
