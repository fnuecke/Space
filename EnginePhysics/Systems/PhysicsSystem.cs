using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Physics.Components;
using Engine.Physics.Detail;
using Engine.Physics.Detail.Collision;
using Engine.Physics.Detail.Contacts;
using Engine.Physics.Messages;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using Engine.FarMath;

using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using Engine.Math;

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
    public sealed class PhysicsSystem : AbstractComponentSystem<Body>, IUpdatingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the global gravity vector.
        /// </summary>
        public Vector2 Gravity
        {
            get { return _gravity; }
            set
            {
                System.Diagnostics.Debug.Assert(!IsLocked);

                _gravity = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is locked. The system will lock
        /// itself during an update, disallowing changes to its state from the outside.
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Gets all bodies in the simulation.
        /// </summary>
        public IEnumerable<Body> Bodies
        {
            get { return Components; }
        }

        /// <summary>
        /// Gets all active the contacts.
        /// </summary>
        public IEnumerable<IContact> Contacts
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

        /// <summary>
        /// Gets the fixture bounding boxes for the debug renderer.
        /// </summary>
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

        /// <summary>
        /// The settings for this simulation.
        /// </summary>
        private float _timestep = 1 / 60f;

        /// <summary>
        /// The directed global gravitational vector.
        /// </summary>
        private Vector2 _gravity = -Vector2.UnitY;

        /// <summary>
        /// The number of used contacts. This is used to reserve all the memory
        /// we might need in our islands used for solving we may need in advance.
        /// </summary>
        private int _contactCount;

        /// <summary>
        /// List of active contacts between bodies (i.e. current active collisions).
        /// </summary>
        private Contact[] _contacts = new Contact[0];

        /// <summary>
        /// List of contact edges, two per contact.
        /// </summary>
        private ContactEdge[] _edges = new ContactEdge[0];

        /// <summary>
        /// The start of the linked list of used contacts.
        /// </summary>
        private int _usedContacts = -1;

        /// <summary>
        /// The start of the linked list of available contacts.
        /// It is important to note that the list of free contacts is maintained
        /// using the 'Previous' reference, to allow keeping the 'Next' reference
        /// intact when deleting contacts during interation of the active contact
        /// list.
        /// </summary>
        private int _freeContacts = -1;

        /// <summary>
        /// The index structure we use for our broad phase. We don't use the index
        /// system because we want to track the individual fixtures, not the complete
        /// body (and the index system only tracks complete entities).
        /// </summary>
#if FARMATH
        private IIndex<int, WorldBounds, WorldPoint> _index = new FarCollections.SpatialHashedQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier);
#else
        private IIndex<int, WorldBounds, WorldPoint> _index = new DynamicQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier);
#endif

        /// <summary>
        /// The list of fixtures that have changed since the last update, i.e. for
        /// which we need to scan for new/lost contacts.
        /// </summary>
        private ISet<int> _changed = new HashSet<int>();

        /// <summary>
        /// Tracks whether a new fixture was added since the last update. This will
        /// trigger a search for new contacts at the beginning of the next update.
        /// </summary>
        private bool _newFixtureAdded;

        /// <summary>
        /// Reused every update for solving simulation constraints.
        /// </summary>
        private Island _island;

        /// <summary>
        /// Proxies for fixtures used in time of impact computation. We keep those two
        /// instances alive to avoid producing garbage.
        /// </summary>
        private Algorithms.DistanceProxy _proxyA = new Algorithms.DistanceProxy(),
                                         _proxyB = new Algorithms.DistanceProxy();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicsSystem"/> class.
        /// </summary>
        /// <param name="timestep">The timestep.</param>
        /// <param name="gravity">The global gravity vector.</param>
        public PhysicsSystem(float timestep, Vector2 gravity)
        {
            _timestep = timestep;
            _gravity = gravity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicsSystem"/> class.
        /// </summary>
        /// <param name="timestep">The timestep.</param>
        public PhysicsSystem(float timestep) : this(timestep, Vector2.Zero)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            // Lock system to avoid adding/removal of bodies and fixtures and
            // modifying critical properties during an update (in message handlers).
            IsLocked = true;

            // If new fixtures were added we want to look for contacts with that
            // fixtures before jumping into the actual update.
            if (_newFixtureAdded)
            {
                FindContacts();
                _newFixtureAdded = false;
            }

            // Check all contacts for validity (may delete some).
            UpdateContacts();

            // Build our time step information.
            TimeStep step;
            step.DeltaT = _timestep;
            step.IsWarmStarting = true;

            // Find islands, integrate and solve constraints.
            Solve(step);

            // Find TOI contacts and solve them.
            SolveTOI(step);

            // Clear forces for next iteration.
            foreach (var body in Components)
            {
                body.ClearForces();
            }

            // Done, unlock the system.
            IsLocked = false;
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
                System.Diagnostics.Debug.Assert(fixture != null);
                var body = Manager.GetComponent(fixture.Entity, Body.TypeId) as Body;
                System.Diagnostics.Debug.Assert(body != null);

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

        /// <summary>
        /// Marks the specified entity as changed, forcing a search for new/lost
        /// contacts with this entity in the next update.
        /// </summary>
        /// <param name="entity">The entity to mark.</param>
        internal void QueueForContactSearch(int entity)
        {
            foreach (Fixture fixture in Manager.GetComponents(entity, Fixture.TypeId))
            {
                _changed.Add(fixture.Id);
            }
        }

        /// <summary>
        /// Frees the contacts the specified body is involved in. This is used to
        /// invalidate contacts when a body significantly changes (e.g. its type
        /// or enabled state). It also removes the fixtures of the body from the
        /// list of changed fixtures, to avoid updating them.
        /// </summary>
        /// <param name="entity">The entity for which to free the contacts.</param>
        internal void RemoveContacts(int entity)
        {
            foreach (Fixture fixture in Manager.GetComponents(entity, Fixture.TypeId))
            {
                while (fixture.EdgeList >= 0)
                {
                    FreeContact(_edges[fixture.EdgeList].Parent);
                }

                _changed.Remove(fixture.Id);
            }

            // Behave as though a new fixture was added and check for contacts
            // before the actual update next time.
            _newFixtureAdded = true;
        }

        /// <summary>
        /// Updates the specified fixture's position in the index structure
        /// to the specified new bounds, within the context of the specified
        /// displacement.
        /// </summary>
        /// <param name="bounds">The new bounds of the fixture.</param>
        /// <param name="delta">How much the fixture has moved.</param>
        /// <param name="fixtureId">The id of the fixture.</param>
        internal void UpdateIndex(WorldBounds bounds, Vector2 delta, int fixtureId)
        {
            if (_index.Update(bounds, delta, fixtureId))
            {
                // The index changed, mark the fixture for contact updates.
                _changed.Add(fixtureId);
            }
        }

        /// <summary>
        /// Called by the manager when a new component was added.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            var fixture = component as Fixture;
            var body = component as Body;
            if ((body != null || fixture != null) && IsLocked)
            {
                throw new InvalidOperationException("Must not add or remove bodies and fixtures during update.");
            }

            if (body != null)
            {
                if (Manager.GetComponents(body.Entity, Body.TypeId).Count() > 1)
                {
                    throw new InvalidOperationException("There must only be one body component per entity.");
                }
            }
            else if (fixture != null)
            {
                body = Manager.GetComponent(fixture.Entity, Body.TypeId) as Body;
                if (body == null)
                {
                    throw new InvalidOperationException("Fixtures must be added to entities that already have a body.");
                }

                // Recompute mass for our body, if we have density.
                if (fixture.Density > 0)
                {
                    body.ResetMassData();
                }

                // Add it to our index.
                _index.Add(fixture.ComputeBounds(body.Transform), fixture.Id);

                if (body.Enabled)
                {
                    // Make sure the body is awake.
                    body.IsAwake = true;

                    // Mark it for a first contact check.
                    _changed.Add(component.Id);

                    // Remember to check for new contacts before the next update.
                    _newFixtureAdded = true;
                }
            }

            // All checks passed, continue with parent.
            base.OnComponentAdded(component);
        }

        /// <summary>
        /// Called by the manager when a component was removed.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            var fixture = component as Fixture;
            var body = component as Body;
            if ((body != null || fixture != null) && IsLocked)
            {
                throw new InvalidOperationException("Must not add or remove bodies and fixtures during update.");
            }

            base.OnComponentRemoved(component);

            if (body != null)
            {
                // Remove all fixtures if a body is removed.
                foreach (Fixture f in Manager.GetComponents(component.Entity, Fixture.TypeId).ToList())
                {
                    Manager.RemoveComponent(f);
                }
            }
            else if (fixture != null)
            {
                // Remove all contacts involving the fixture.
                while (fixture.EdgeList >= 0)
                {
                    FreeContact(_edges[fixture.EdgeList].Parent);
                }

                // Remove it from the list of fixtures to update.
                _changed.Remove(fixture.Id);

                // Remove from index.
                _index.Remove(fixture.Id);
            }
        }

        #endregion

        #region Contact allocation

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
                System.Diagnostics.Debug.Assert(_contacts[i] == null);
                _contacts[i] = new Contact
                {
                    Manager = Manager,
                    Previous = i + 1
                };
            }
            for (var i = start * 2; i < _edges.Length; i++)
            {
                System.Diagnostics.Debug.Assert(_edges[i] == null);
                _edges[i] = new ContactEdge();
            }

            // Prepend to existing list.
            _contacts[_contacts.Length - 1].Previous = -1;
            _freeContacts = start;
        }

        /// <summary>
        /// Allocates a new contact between the two specified fixtures and initializes it.
        /// </summary>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        private void AllocateContact(Fixture fixtureA, Fixture fixtureB)
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
                _edges.CopyTo(newEdges, 0);
                _contacts = newContacts;
                _edges = newEdges;

                // Initialize the new segment by making it available in the linked
                // list of available contacts.
                InitializeContacts(startOfNewSegment);
            }

            // Remember index of contact we will return, then remove it from
            // the linked list of available contacts.
            var contact = _freeContacts;
            _freeContacts = _contacts[_freeContacts].Previous;

            // Initialize with the basics. This may swap the fixture order.
            _contacts[contact].Initialize(ref fixtureA, ref fixtureB);

            var edgeA = contact * 2;
            var edgeB = contact * 2 + 1;

            System.Diagnostics.Debug.Assert(fixtureA.EdgeList != edgeA);
            System.Diagnostics.Debug.Assert(fixtureB.EdgeList != edgeB);

            _edges[edgeA].Parent = contact;
            _edges[edgeA].Other = fixtureB.Entity;
            _edges[edgeA].Next = fixtureA.EdgeList;
            _edges[edgeA].Previous = -1;

            _edges[edgeB].Parent = contact;
            _edges[edgeB].Other = fixtureA.Entity;
            _edges[edgeB].Next = fixtureB.EdgeList;
            _edges[edgeB].Previous = -1;

            // Adjust local linked lists.
            if (fixtureA.EdgeList >= 0)
            {
                _edges[fixtureA.EdgeList].Previous = edgeA;
            }
            if (fixtureB.EdgeList >= 0)
            {
                _edges[fixtureB.EdgeList].Previous = edgeB;
            }

            fixtureA.EdgeList = edgeA;
            fixtureB.EdgeList = edgeB;

            // Adjust global linked list.
            if (_usedContacts < 0)
            {
                // First contact.
                _contacts[contact].Next = -1;
            }
            else
            {
                // Prepend to list.
                _contacts[_usedContacts].Previous = contact;
                _contacts[contact].Next = _usedContacts;
            }
            _contacts[contact].Previous = -1;
            _usedContacts = contact;

            // Increment counter used for island allocation.
            ++_contactCount;
        }

        /// <summary>
        /// Frees the specified contact.
        /// </summary>
        /// <param name="contact">The contact.</param>
        private void FreeContact(int contact)
        {
            // Get the actual components.
            var fixtureA = Manager.GetComponentById(_contacts[contact].FixtureIdA) as Fixture;
            var fixtureB = Manager.GetComponentById(_contacts[contact].FixtureIdB) as Fixture;

            System.Diagnostics.Debug.Assert(fixtureA != null);
            System.Diagnostics.Debug.Assert(fixtureB != null);

            // Send message if this contact was active.
            if (_contacts[contact].IsTouching)
            {
                EndContact message;
                message.Contact = _contacts[contact];
                Manager.SendMessage(message);

                var bodyA = Manager.GetComponent(fixtureA.Entity, Body.TypeId) as Body;
                var bodyB = Manager.GetComponent(fixtureB.Entity, Body.TypeId) as Body;

                System.Diagnostics.Debug.Assert(bodyA != null);
                System.Diagnostics.Debug.Assert(bodyB != null);

                bodyA.IsAwake = true;
                bodyB.IsAwake = true;
            }

            // Remove from local edge lists.
            {
                var edgeA = contact * 2;
                var previous = _edges[edgeA].Previous;
                var next = _edges[edgeA].Next;
                if (previous >= 0)
                {
                    _edges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _edges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (fixtureA.EdgeList == edgeA)
                {
                    fixtureA.EdgeList = next;
                }
            }
            {
                var edgeB = contact * 2 + 1;
                var previous = _edges[edgeB].Previous;
                var next = _edges[edgeB].Next;
                if (previous >= 0)
                {
                    _edges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _edges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (fixtureB.EdgeList == edgeB)
                {
                    fixtureB.EdgeList = next;
                }
            }

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

            // Push contact back to list of free contacts.
            _contacts[contact].Previous = _freeContacts;
            _freeContacts = contact;

            // Decrement counter used for island allocation.
            --_contactCount;
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
            for (var contact = _usedContacts; contact >= 0;)
            {
                var fixtureIdA = _contacts[contact].FixtureIdA;
                var fixtureIdB = _contacts[contact].FixtureIdB;

                // Get the actual components.
                var fixtureA = Manager.GetComponentById(fixtureIdA) as Fixture;
                var fixtureB = Manager.GetComponentById(fixtureIdB) as Fixture;

                System.Diagnostics.Debug.Assert(fixtureA != null);
                System.Diagnostics.Debug.Assert(fixtureB != null);

                // Get the actual collidable information for more filtering.
                var bodyA = (Body)Manager.GetComponent(fixtureA.Entity, Body.TypeId);
                var bodyB = (Body)Manager.GetComponent(fixtureB.Entity, Body.TypeId);

                // See if the collision is still valid.
                if (!bodyA.ShouldCollide(bodyB))
                {
                    // No longer valid, free this contact.
                    var oldContact = contact;
                    contact = _contacts[contact].Next;
                    FreeContact(oldContact);
                    continue;
                }

                System.Diagnostics.Debug.Assert(bodyA.Enabled);
                System.Diagnostics.Debug.Assert(bodyB.Enabled);

                // Skip contacts for sleeping or static bodies and keep them
                // alive (until the non-static body wakes up -- there are no
                // contacts between two static bodies).
                var activeA = bodyA.IsAwake && bodyA._type != Body.BodyType.Static;
                var activeB = bodyB.IsAwake && bodyB._type != Body.BodyType.Static;
                if (!activeA && !activeB)
                {
                    // Continue with next contact.
                    contact = _contacts[contact].Next;
                    continue;
                }

                // Test for actual collision. First check via the index.
                if (!_index[fixtureIdA].Intersects(_index[fixtureIdB]))
                {
                    // Contact stopped being valid, free it.
                    var oldContact = contact;
                    contact = _contacts[contact].Next;
                    FreeContact(oldContact);
                    continue;
                }

                // Contact is still valid, continue with narrow phase, which tests
                // for detailed collision based on the shapes of the fixtures, and
                // computes the contact manifold.
                _contacts[contact].Update(Manager, fixtureA, fixtureB, bodyA, bodyB);

                // Continue with next contact.
                contact = _contacts[contact].Next;
            }
        }

        /// <summary>
        /// Checks for new contacts for all entities that moved significantly in the index.
        /// </summary>
        private void FindContacts()
        {
            // Check the list of entities that moved in the index.
            ISet<int> neighbors = new HashSet<int>();
            foreach (var fixture in _changed)
            {
                // Find contacts (possible collisions based on fattened bounds).
                _index.Find(_index[fixture], ref neighbors);
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

                    System.Diagnostics.Debug.Assert(fixtureA != null);
                    System.Diagnostics.Debug.Assert(fixtureB != null);

                    // Get the actual collidable information for more filtering.
                    var bodyA = (Body)Manager.GetComponent(fixtureA.Entity, Body.TypeId);
                    var bodyB = (Body)Manager.GetComponent(fixtureB.Entity, Body.TypeId);

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
                    if (!bodyA.ShouldCollide(bodyB))
                    {
                        continue;
                    }

                    // Check if the contact is already known.
                    if (ContactExists(fixtureA.EdgeList, fixtureIdA, fixtureIdB, bodyB.Entity))
                    {
                        continue;
                    }

                    // Not known, create new contact.
                    AllocateContact(fixtureA, fixtureB);

                    // Make sure the two involved bodies are awake.
                    bodyA.IsAwake = true;
                    bodyB.IsAwake = true;
                }
                neighbors.Clear();
            }
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
            for (var i = edgeList; i >= 0; i = _edges[i].Next)
            {
                // Only consider edges connected to the other body.
                if (_edges[i].Other != entityB)
                {
                    continue;
                }

                // Get the actual contact for that edge.
                var contact = _contacts[_edges[i].Parent];

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
                _island = new Island(Manager);
            }

            // Size the island for the worst case.
            _island.Reset(Components.Count, _contactCount);

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
                    seed._type == Body.BodyType.Static)
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
                    System.Diagnostics.Debug.Assert(body.Enabled);

                    // And add it to the island.
                    _island.Add(body);

                    // Make sure the body is awake.
                    body.IsAwake = true;

                    // To keep islands as small as possible, we don't
                    // propagate islands across static bodies.
                    if (body._type == Body.BodyType.Static)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body. We store the start of the
                    // contact lists in the fixtures that the are related to, so we first
                    // need to iterate the fixtures.
                    foreach (Fixture fixture in Manager.GetComponents(body.Entity, Fixture.TypeId))
                    {
                        // Then we loop the list of contact edges in the fixture.
                        for (var i = fixture.EdgeList; i >= 0; i = _edges[i].Next)
                        {
                            // Get the actual contact for that edge.
                            var contact = _contacts[_edges[i].Parent];

                            // Skip disabled, non-touching and already processed contacts.
                            if (!contact.IsEnabled ||
                                !contact.IsTouching ||
                                _island.IsProcessed(contact))
                            {
                                continue;
                            }

                            // Add the contact to the island.
                            _island.Add(contact);
                            _island.MarkProcessed(contact);

                            // Get the other party involved in this contact.
                            var other = Manager.GetComponent(_edges[i].Other, Body.TypeId) as Body;

                            System.Diagnostics.Debug.Assert(other != null);

                            // Was the other body already added to this island?
                            if (_island.IsProcessed(other))
                            {
                                continue;
                            }

                            // OK, mark it for processing and flag it as being in an island.
                            stack.Push(other);
                            _island.MarkProcessed(other);
                        }
                    }
                }

                // Done building our island, solve it.
                _island.Solve(step, _gravity);

                // Post solve cleanup: allow static bodies to participate in other islands.
                _island.UnmarkStaticBodies();
            }

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

        /// <summary>
        /// Post-processing step after the simple solver, checking for possibly skipped
        /// collosions (tunneling effect).
        /// </summary>
        private void SolveTOI(TimeStep step)
        {
            // Resize island; we limit the number of considered contacts and bodies
            // to avoid excessive substepping.
            _island.Reset(2 * Settings.MaxTOIContacts, Settings.MaxTOIContacts);

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
                        var fA = Manager.GetComponentById(c.FixtureIdA) as Fixture;
                        var fB = Manager.GetComponentById(c.FixtureIdB) as Fixture;

                        System.Diagnostics.Debug.Assert(fA != null);
                        System.Diagnostics.Debug.Assert(fB != null);

                        var bA = Manager.GetComponent(fA.Entity, Body.TypeId) as Body;
                        var bB = Manager.GetComponent(fB.Entity, Body.TypeId) as Body;

                        System.Diagnostics.Debug.Assert(bA != null);
                        System.Diagnostics.Debug.Assert(bB != null);

                        var typeA = bA._type;
                        var typeB = bB._type;

                        System.Diagnostics.Debug.Assert(typeA == Body.BodyType.Dynamic || typeB == Body.BodyType.Dynamic);

                        // Is at least one body active (awake and dynamic or kinematic)?
                        var activeA = bA._isAwake && typeA != Body.BodyType.Static;
                        var activeB = bB._isAwake && typeB != Body.BodyType.Static;
                        if (!activeA && !activeB)
                        {
                            continue;
                        }

                        // Are these two non-bullet dynamic bodies?
                        var collideA = bA._isBullet || typeA != Body.BodyType.Dynamic;
                        var collideB = bB._isBullet || typeB != Body.BodyType.Dynamic;
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

                        System.Diagnostics.Debug.Assert(alpha0 < 1.0f);

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
                    var fA = Manager.GetComponentById(minContact.FixtureIdA) as Fixture;
                    var fB = Manager.GetComponentById(minContact.FixtureIdB) as Fixture;

                    System.Diagnostics.Debug.Assert(fA != null);
                    System.Diagnostics.Debug.Assert(fB != null);

                    var bA = Manager.GetComponent(fA.Entity, Body.TypeId) as Body;
                    var bB = Manager.GetComponent(fB.Entity, Body.TypeId) as Body;

                    System.Diagnostics.Debug.Assert(bA != null);
                    System.Diagnostics.Debug.Assert(bB != null);

                    var backupA = bA.Sweep;
                    var backupB = bB.Sweep;

                    bA.Advance(minAlpha);
                    bB.Advance(minAlpha);

                    // The TOI contact likely has some new contact points.
                    minContact.Update(Manager, fA, fB, bA, bB);
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
                    subStep.IsWarmStarting = false;
                    _island.SolveTOI(subStep, bA.IslandIndex, bB.IslandIndex);

                    // Reset island flags and synchronize broad-phase proxies.
                    _island.UnmarkAllBodies();
                    foreach (var body in _island.Bodies)
                    {
                        // If it's not a dynamic body, we didn't move it.
                        if (body._type != Body.BodyType.Dynamic)
                        {
                            continue;
                        }

                        body.SynchronizeFixtures();

                        // Invalidate all contact TOIs on this displaced body.
                        foreach (Fixture fixture in Manager.GetComponents(body.Entity, Fixture.TypeId))
                        {
                            for (var edge = fixture.EdgeList; edge >= 0; edge = _edges[edge].Next)
                            {
                                var contact = _contacts[_edges[edge].Parent];
                                contact.HasCachedTOI = false;
                                _island.UnmarkContact(contact);
                            }
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
            if (body._type != Body.BodyType.Dynamic)
            {
                return;
            }

            // Get all fixtures for this body.
            foreach (Fixture fixture in Manager.GetComponents(body.Entity, Fixture.TypeId))
            {
                // Then we loop the list of contact edges in the fixtures.
                for (var i = fixture.EdgeList; i >= 0; i = _edges[i].Next)
                {
                    // Stop if we have reached our limits.
                    if (_island.IsFull)
                    {
                        return;
                    }

                    var edge = _edges[i];

                    // Get the actual contact for that edge.
                    var contact = _contacts[edge.Parent];

                    // Has this contact already been added to the island?
                    if (_island.IsProcessed(contact))
                    {
                        continue;
                    }

                    // Get the other party involved in this contact.
                    var other = Manager.GetComponent(edge.Other, Body.TypeId) as Body;

                    System.Diagnostics.Debug.Assert(other != null);

                    // Skip disabled bodies.
                    if (!other.Enabled)
                    {
                        continue;
                    }

                    // Only add static, kinematic, or bullet bodies.
                    if (other._type == Body.BodyType.Dynamic &&
                        !body.IsBullet && !other.IsBullet)
                    {
                        continue;
                    }

                    // Tentatively advance the body to the TOI.
                    var backup = other.Sweep;
                    if (!_island.IsProcessed(other))
                    {
                        other.Advance(minAlpha);
                    }

                    // Update the contact points
                    var fA = Manager.GetComponentById(contact.FixtureIdA) as Fixture;
                    var fB = Manager.GetComponentById(contact.FixtureIdB) as Fixture;

                    System.Diagnostics.Debug.Assert(fA != null);
                    System.Diagnostics.Debug.Assert(fB != null);

                    var bA = Manager.GetComponent(fA.Entity, Body.TypeId) as Body;
                    var bB = Manager.GetComponent(fB.Entity, Body.TypeId) as Body;

                    System.Diagnostics.Debug.Assert(bA != null);
                    System.Diagnostics.Debug.Assert(bB != null);

                    contact.Update(Manager, fA, fB, bA, bB);

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
                    if (other._type != Body.BodyType.Static)
                    {
                        other.IsAwake = true;
                    }

                    // Add the other body to the island.
                    _island.Add(other);
                    _island.MarkProcessed(other);
                }
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
            System.Diagnostics.Debug.Assert(!IsLocked);

            base.Packetize(packet);

            packet.Write(_timestep);

            packet.Write(_gravity);

            packet.Write(_contactCount);

            packet.Write(_contacts.Length);
            for (var i = 0; i < _contacts.Length; i++)
            {
                packet.Write(_contacts[i]);
            }

            packet.Write(_edges.Length);
            for (var i = 0; i < _edges.Length; i++)
            {
                packet.Write(_edges[i]);
            }

            packet.Write(_usedContacts);

            packet.Write(_freeContacts);

            packet.Write(_index.Count);
            foreach (var entry in _index)
            {
                packet.Write(entry.Item1);
                packet.Write(entry.Item2);
            }

            packet.Write(_changed.Count);
            foreach (var entry in _changed)
            {
                packet.Write(entry);
            }

            packet.Write(_newFixtureAdded);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            System.Diagnostics.Debug.Assert(!IsLocked);

            base.Depacketize(packet);

            _timestep = packet.ReadSingle();

            _gravity = packet.ReadVector2();

            _contactCount = packet.ReadInt32();

            _contacts = new Contact[packet.ReadInt32()];
            for (var i = 0; i < _contacts.Length; i++)
            {
                packet.ReadPacketizableInto(ref _contacts[i]);
            }

            _edges = new ContactEdge[packet.ReadInt32()];
            for (var i = 0; i < _edges.Length; i++)
            {
                packet.ReadPacketizableInto(ref _edges[i]);
            }

            _usedContacts = packet.ReadInt32();

            _freeContacts = packet.ReadInt32();

            _index.Clear();
            var indexCount = packet.ReadInt32();
            for (var i = 0; i < indexCount; i++)
            {
#if FARMATH
                var bounds = packet.ReadFarRectangle();
#else
                var bounds = packet.ReadRectangleF();
#endif
                var id = packet.ReadInt32();
                _index.Add(bounds, id);
            }

            _changed.Clear();
            var changedCount = packet.ReadInt32();
            for (var i = 0; i < changedCount; i++)
            {
                _changed.Add(packet.ReadInt32());
            }

            _newFixtureAdded = packet.ReadBoolean();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            System.Diagnostics.Debug.Assert(!IsLocked);

            base.Hash(hasher);

            // TODO
            hasher.Put(_usedContacts);
            hasher.Put(_freeContacts);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>
        /// The copy.
        /// </returns>
        public override AbstractSystem NewInstance()
        {
            System.Diagnostics.Debug.Assert(!IsLocked);

            var copy = (PhysicsSystem)base.NewInstance();

#if FARMATH
            copy._index = new FarCollections.SpatialHashedQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier);
#else
            copy._index = new DynamicQuadTree<int>(16, 64, Settings.AabbExtension, Settings.AabbMultiplier);
#endif
            copy._changed = new HashSet<int>();
            copy._contacts = new Contact[0];
            copy._edges = new ContactEdge[0];
            copy._usedContacts = -1;
            copy._freeContacts = 0;

            copy._island = null;
            copy._proxyA = new Algorithms.DistanceProxy();
            copy._proxyB = new Algorithms.DistanceProxy();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            System.Diagnostics.Debug.Assert(!IsLocked);

            base.CopyInto(into);

            var copy = (PhysicsSystem)into;

            copy._timestep = _timestep;
            copy._gravity = _gravity;

            copy._index.Clear();
            foreach (var entry in _index)
            {
                copy._index.Add(entry.Item1, entry.Item2);
            }

            copy._changed.Clear();
            copy._changed.UnionWith(_changed);
            copy._newFixtureAdded = _newFixtureAdded;

            if (copy._contacts.Length != _contacts.Length)
            {
                copy._contacts = new Contact[_contacts.Length];
            }
            for (var i = 0; i < _contacts.Length; ++i)
            {
                if (copy._contacts[i] == null)
                {
                    copy._contacts[i] = _contacts[i].NewInstance();
                }
                _contacts[i].CopyInto(copy._contacts[i]);
                copy._contacts[i].Manager = copy.Manager;
            }

            if (copy._edges.Length != _edges.Length)
            {
                copy._edges = new ContactEdge[_edges.Length];
            }
            for (var i = 0; i < _edges.Length; ++i)
            {
                if (copy._edges[i] == null)
                {
                    copy._edges[i] = _edges[i].NewInstance();
                }
                _edges[i].CopyInto(copy._edges[i]);
            }

            copy._usedContacts = _usedContacts;
            copy._freeContacts = _freeContacts;
        }

        #endregion

        #region Contact interface

        /// <summary>
        /// This interface is used to pass contact information outside the internal
        /// state, for example for iteration of existing contacts or when firing
        /// contact messages.
        /// </summary>
        public interface IContact
        {
            /// <summary>
            /// Gets the first fixture involved in this contact.
            /// </summary>
            Fixture FixtureA { get; }

            /// <summary>
            /// Gets the second fixture involved in this contact.
            /// </summary>
            Fixture FixtureB { get; }

            /// <summary>
            /// Gets the normal impulse of the specified contact point (separation).
            /// </summary>
            float GetNormalImpulse(int point);

            /// <summary>
            /// Gets the tangent impulse of the specified contact point (friction).
            /// </summary>
            float GetTangentImpulse(int point);

            /// <summary>
            /// Computes the world manifold data for this contact. This is relatively
            /// expensive, so use with care.
            /// The normal applies for all contact points. The number of contact points
            /// may vary. For an active contact it is one or two, for inactive contacts
            /// it is zero.
            /// </summary>
            /// <param name="normal">The world contact normal.</param>
            /// <param name="points">The contact points.</param>
            void ComputeWorldManifold(out Vector2 normal, out IList<WorldPoint> points);
        }

        #endregion

        #region ToString

        // TODO

        #endregion
    }
}
