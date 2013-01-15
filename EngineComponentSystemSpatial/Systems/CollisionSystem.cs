using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Messages;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>
    ///     This system takes care of components that support collision (anything that extends <see cref="Collidable"/>). It
    ///     fetches the components neighbors and checks their collision groups, keeping the number of actual collision checks
    ///     that have to be performed low.
    ///     <para>
    ///         Collisions are handled in a way very much inspired by box 2D: * In the index, entities are stored with
    ///         'inflated' bounds, i.e. the rectangle in the index representing them is not their minimal bounding box, but one
    ///         that has been expanded slightly. Once by a fixed small delta, which works against frequent updates due to tiny,
    ///         wobbly motion, as well as by a dynamic amount into the direction the entity is moving in. Together with the
    ///         fact that entities rarely stop instantly, this works against frequent updates due to movement, but keeps the
    ///         update frequency rather constant (due to the dependency on the velocity). * In each update we get the list of
    ///         entities from the index that have changed so much so that their expanded bounds have changed. This means they
    ///         now potentially collide with entities they did not before (bounds may intersect with other bounds, now). For
    ///         each such changed entity we get the list of intersections with other bounds, and for each intersection we
    ///         declare a 'contact'. If there already is a contact for the two intersecting entities we skip them. * Each
    ///         contact has two states: colliding and not colliding. This state is updated each update, by querying the
    ///         specific collision detection methods of the two entities involved in the contact. * When a contact state
    ///         changed, we send a message to let other systems know that a new collision took place, or that an active one
    ///         stopped.
    ///     </para>
    ///     <para>
    ///         The lists for contacts and contact edges (an edge is a directed part of a contact when it is in its colliding
    ///         state, so there's one for each of the two involved entities) are managed in a hybrid of a linked list and an
    ///         array list. This allows for the best of both worlds: very fast removal and adding as well as indexed lookup of
    ///         members. Essentially this is a linked list with 'pointers' to the single nodes, but using structs, which gives
    ///         us nice memory locality.
    ///     </para>
    /// </summary>
    public sealed class CollisionSystem : AbstractSystem, IMessagingSystem, IUpdatingSystem
    {
        #region Constants

        /// <summary>Start using indexes after the collision index.</summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        /// <summary>Default capacity of the contacts list.</summary>
        private const int DefaultContactCapacity = 64;

        #endregion

        #region Fields

        /// <summary>List of active contacts between collidables (i.e. current active collisions).</summary>
        [CopyIgnore, PacketizerIgnore]
        private Contact[] _contacts = new Contact[DefaultContactCapacity];

        /// <summary>List of contact edges, two per contact.</summary>
        [CopyIgnore, PacketizerIgnore]
        private ContactEdge[] _edges = new ContactEdge[DefaultContactCapacity * 2];

        /// <summary>The start of the linked list of used contacts.</summary>
        private int _usedContacts = -1;

        /// <summary>
        ///     The start of the linked list of available contacts. It is important to note that the list of free contacts is
        ///     maintained using the 'Previous' reference, to allow keeping the 'Next' reference intact when deleting contacts
        ///     during iteration of the active contact list.
        /// </summary>
        private int _freeContacts;

        #endregion

        #region Constructor

        public CollisionSystem()
        {
            InitializeContacts(0);
        }

        private void InitializeContacts(int start)
        {
            // Initialize all new entries.
            for (var i = start; i < _contacts.Length - 1; i++)
            {
                _contacts[i].Previous = i + 1;
            }

            // Prepend to existing list.
            _contacts[_contacts.Length - 1].Previous = -1;
            _freeContacts = start;
        }

        private void AllocateContact(int componentA, int componentB, int entityA, int entityB)
        {
            // The collidable info for the two entities, holding in particular
            // the head of the linked list of contacts involving the entities.
            var collidableA = (Collidable) Manager.GetComponent(entityA, Collidable.TypeId);
            var collidableB = (Collidable) Manager.GetComponent(entityB, Collidable.TypeId);

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

            // Initialize with the basics.
            _contacts[contact].ComponentA = componentA;
            _contacts[contact].ComponentB = componentB;
            _contacts[contact].Intersecting = false;

            var edgeA = contact * 2;
            var edgeB = contact * 2 + 1;

            _edges[edgeA].Parent = contact;
            _edges[edgeA].Next = collidableA.ContactList;
            _edges[edgeA].Previous = -1;

            _edges[edgeB].Parent = contact;
            _edges[edgeB].Next = collidableB.ContactList;
            _edges[edgeB].Previous = -1;

            // Adjust local linked lists.
            if (collidableA.ContactList >= 0)
            {
                _edges[collidableA.ContactList].Previous = edgeA;
            }
            if (collidableB.ContactList >= 0)
            {
                _edges[collidableB.ContactList].Previous = edgeB;
            }

            collidableA.ContactList = edgeA;
            collidableB.ContactList = edgeB;

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

            // For debug view.
            SetCollisionState(componentA, Collidable.CollisionState.Contact);
            SetCollisionState(componentB, Collidable.CollisionState.Contact);
        }

        private void FreeContact(int contact, Collidable collidableA, Collidable collidableB)
        {
            // Remove from local edge lists.
            {
                var edge = contact * 2;
                var previous = _edges[edge].Previous;
                var next = _edges[edge].Next;
                if (previous >= 0)
                {
                    _edges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _edges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (collidableA.ContactList == edge)
                {
                    collidableA.ContactList = next;
                }
            }
            {
                var edge = contact * 2 + 1;
                var previous = _edges[edge].Previous;
                var next = _edges[edge].Next;
                if (previous >= 0)
                {
                    _edges[previous].Next = next;
                }
                if (next >= 0)
                {
                    _edges[next].Previous = previous;
                }
                // Adjust list pointer as necessary.
                if (collidableB.ContactList == edge)
                {
                    collidableB.ContactList = next;
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

            // Debug view for collisions.
            SetCollisionState(_contacts[contact].ComponentA, Collidable.CollisionState.None);
            SetCollisionState(_contacts[contact].ComponentB, Collidable.CollisionState.None);
        }

        #endregion

        #region Logic

        /// <summary>Updates the system.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            return;
            // Checks for new contacts for changed entities.
            FindContacts();

            // Check all contacts for validity (may delete some) and check for
            // actual intersections between entities.
            UpdateContacts();
        }

        /// <summary>Checks for new contacts for all entities that moved significantly in the index.</summary>
        private void FindContacts()
        {
            // Get reference to index system.
            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);

            // Check the list of entities that moved in the index.
            ISet<int> neighbors = new HashSet<int>();
            foreach (IIndexable component in index.GetChanged(IndexGroupMask).Select(id => Manager.GetComponentById(id)))
            {
                // See if it's a collidable.
                var collidable = (Collidable) Manager.GetComponent(component.Entity, Collidable.TypeId);
                if (collidable == null)
                {
                    continue;
                }

                // Find contacts (possible collisions based on fattened bounds).
                var bounds = index[IndexGroupMask][component.Id];
                index[IndexGroupMask].Find(bounds, neighbors);
                foreach (IIndexable neighbor in neighbors.Select(id => Manager.GetComponentById(id)))
                {
                    // Skip self collision.
                    if (neighbor == component)
                    {
                        continue;
                    }

                    // Get sorted ids of involved entities.
                    int componentA, componentB, entityA, entityB;
                    if (component.Id < neighbor.Id)
                    {
                        componentA = component.Id;
                        componentB = neighbor.Id;
                        entityA = component.Entity;
                        entityB = neighbor.Entity;
                    }
                    else
                    {
                        componentA = neighbor.Id;
                        componentB = component.Id;
                        entityA = neighbor.Entity;
                        entityB = component.Entity;
                    }

                    // Check if the contact is already known.
                    if (ContactExists(collidable.ContactList, componentA, componentB))
                    {
                        continue;
                    }

                    // Get the actual collidable information for more filtering.
                    var collidableA = (Collidable) Manager.GetComponent(entityA, Collidable.TypeId);
                    var collidableB = (Collidable) Manager.GetComponent(entityB, Collidable.TypeId);

                    // Skip if not enabled. If a component gets enabled at a place where it would
                    // intersect this may lead to to missed intersections, but this probably won't
                    // ever really happen -- and if it does one of the two entities just has to
                    // move a little bit...
                    if (!collidableA.Enabled || !collidableB.Enabled)
                    {
                        continue;
                    }

                    // Only test if its from a different collision group. If the groups change
                    // the entity will be marked as changed by the index system, so we don't have
                    // to track that ourselves.
                    if ((collidableA.CollisionGroups & collidableB.CollisionGroups) != 0)
                    {
                        continue;
                    }

                    // Not known, create new contact.
                    //AllocateContact(componentA, componentB);
                }
                neighbors.Clear();
            }
        }

        /// <summary>
        ///     Checks if a contact exists in the specified contact edge list for the two specified entities. The entity ids
        ///     are expected to be sorted (i.e. entityA &lt; entityB).
        /// </summary>
        /// <param name="contactList">The contact list.</param>
        /// <param name="componentA">The entity A.</param>
        /// <param name="componentB">The entity B.</param>
        /// <returns></returns>
        private bool ContactExists(int contactList, int componentA, int componentB)
        {
            for (var i = contactList; i >= 0; i = _edges[i].Next)
            {
                // Get the actual contact for that edge.
                var contact = _edges[i].Parent;

                // Compare (sorted) ids of involved entities to check if the contact
                // represents the one we're create now.
                if (_contacts[contact].ComponentA == componentA && _contacts[contact].ComponentB == componentB)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Updates all contacts, checking if they're still valid and if they result in actual intersections.</summary>
        private void UpdateContacts()
        {
            // Get reference to index system.
            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);

            // Check the list of contacts for actual collisions.
            for (var i = _usedContacts; i >= 0; i = _contacts[i].Next)
            {
                var entityA = _contacts[i].ComponentA;
                var entityB = _contacts[i].ComponentB;

                var collidableA = (Collidable) Manager.GetComponent(entityA, Collidable.TypeId);
                var collidableB = (Collidable) Manager.GetComponent(entityB, Collidable.TypeId);

                // Test for actual collision.
                Vector2 normal;
                if (collidableA.Intersects(collidableB, out normal))
                {
                    if (!_contacts[i].Intersecting)
                    {
                        // Got a new intersection.
                        _contacts[i].Intersecting = true;

                        // HACK: because tracing back to check where the collision actually
                        // began is a pain in the ass we simply guesstimate the original impact
                        // normal by weighing in the relative velocity of the to two parties.
                        // This is far from perfect, but should do a relatively good (and cheap) job.
                        var velocityA = (ILinearVelocity) Manager.GetComponent(entityA, ComponentSystem.Manager.GetComponentTypeId<ILinearVelocity>());
                        var velocityB = (ILinearVelocity) Manager.GetComponent(entityB, ComponentSystem.Manager.GetComponentTypeId<ILinearVelocity>());
                        Vector2 relativeVelocity;
                        if (velocityB != null)
                        {
                            if (velocityA != null)
                            {
                                // Both parties are moving, compute A's relative velocity.
                                relativeVelocity = velocityA.Value - velocityB.Value;
                            }
                            else
                            {
                                // A isn't moving, so we can just take B's inverse velocity.
                                relativeVelocity = -velocityB.Value;
                            }
                        }
                        else if (velocityA != null)
                        {
                            // B wasn't moving, so we can take A's speed as is.
                            relativeVelocity = velocityA.Value;
                        }
                        else
                        {
                            // Well this is interesting... neither of the parties moved :P
                            // Use the normal to avoid doing anything at all...
                            relativeVelocity = normal;
                        }

                        // Normalize, then interpolate with the normal.
                        relativeVelocity.Normalize();
                        normal = Vector2.Lerp(normal, relativeVelocity, 0.5f);

                        // Send message.
                        BeginCollision message;
                        message.ContactId = i;
                        message.EntityA = entityA;
                        message.EntityB = entityB;
                        message.Normal = normal;
                        Manager.SendMessage(message);

                        // For debug view.
                        SetCollisionState(entityA, Collidable.CollisionState.Contact);
                        SetCollisionState(entityB, Collidable.CollisionState.Contact);
                    }
                }
                else
                {
                    // Test if the contact is still valid (fattened bounds overlap).
                    var boundsA = index.GetBounds(entityA, IndexGroupMask);
                    var boundsB = index.GetBounds(entityB, IndexGroupMask);
                    if (!boundsA.Intersects(boundsB))
                    {
                        // No, we can free this contact.
                        FreeContact(i, collidableA, collidableB);
                    }
                    else if (_contacts[i].Intersecting)
                    {
                        // Yes, and the contact was active, but is no longer.
                        _contacts[i].Intersecting = false;

                        // Send message.
                        EndCollision message;
                        message.ContactId = i;
                        message.EntityA = entityA;
                        message.EntityB = entityB;
                        Manager.SendMessage(message);

                        // For debug view.
                        SetCollisionState(entityA, Collidable.CollisionState.Collides);
                        SetCollisionState(entityB, Collidable.CollisionState.Collides);
                    }
                }
            }
        }

        /// <summary>Update the previous position to the current one when adding a component.</summary>
        /// <param name="component">The added component.</param>
        public override void OnComponentAdded(Component component)
        {
            var collidable = component as Collidable;
            if (collidable != null)
            {
                // Initialize previous position to current position, to avoid false positives
                // due to sweep intersection tests (with a sweep from (0,0) to position in the
                // first update).
                var transform = ((Transform) Manager.GetComponent(collidable.Entity, Transform.TypeId));
                if (transform != null)
                {
                    collidable.PreviousPosition = transform.Translation;
                }
            }
        }

        /// <summary>Called by the manager when a component was removed.</summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            if (component is Collidable)
            {
                // Remove all contacts involving the specified component.
                for (var i = _usedContacts; i >= 0; i = _contacts[i].Next)
                {
                    if (_contacts[i].ComponentA == component.Entity || _contacts[i].ComponentB == component.Entity)
                    {
                        if (_contacts[i].Intersecting)
                        {
                            // Contact was active, so send a message that the intersection ended.
                            EndCollision message;
                            message.ContactId = i;
                            message.EntityA = _contacts[i].ComponentA;
                            message.EntityB = _contacts[i].ComponentB;
                            Manager.SendMessage(message);
                        }

                        var collidableA = (Collidable) Manager.GetComponent(_contacts[i].ComponentA, Collidable.TypeId);
                        var collidableB = (Collidable) Manager.GetComponent(_contacts[i].ComponentB, Collidable.TypeId);

                        FreeContact(i, collidableA, collidableB);
                    }
                }
            }
        }

        /// <summary>Update the previous position when a collidable component changes its position.</summary>
        /// <param name="message">The sent message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as TranslationChanged?;
            if (cm == null)
            {
                return;
            }

            var m = cm.Value;

            var collidable = ((Collidable) Manager.GetComponent(m.Component.Entity, Collidable.TypeId));
            if (collidable != null)
            {
                collidable.PreviousPosition = m.PreviousPosition;
            }
        }

        /// <summary>Only make the effort to set this when in debug mode.</summary>
        /// <param name="component">The entity.</param>
        /// <param name="state">The state.</param>
        [Conditional("DEBUG")]
        private void SetCollisionState(int component, Collidable.CollisionState state)
        {
            if (!Manager.HasComponent(component))
            {
                return;
            }
            //((Collidable) Manager.GetComponent(component, Collidable.TypeId)).State = state;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_contacts.Length);
            for (var i = 0; i < _contacts.Length; i++)
            {
                packet.Write(_contacts[i].ComponentA);
                packet.Write(_contacts[i].ComponentB);
                packet.Write(_contacts[i].Previous);
                packet.Write(_contacts[i].Next);
                packet.Write(_contacts[i].Intersecting);
            }

            packet.Write(_edges.Length);
            for (var i = 0; i < _edges.Length; i++)
            {
                packet.Write(_edges[i].Parent);
                packet.Write(_edges[i].Previous);
                packet.Write(_edges[i].Next);
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _contacts = new Contact[packet.ReadInt32()];
            for (var i = 0; i < _contacts.Length; i++)
            {
                _contacts[i].ComponentA = packet.ReadInt32();
                _contacts[i].ComponentB = packet.ReadInt32();
                _contacts[i].Previous = packet.ReadInt32();
                _contacts[i].Next = packet.ReadInt32();
                _contacts[i].Intersecting = packet.ReadBoolean();
            }

            _edges = new ContactEdge[packet.ReadInt32()];
            for (var i = 0; i < _edges.Length; i++)
            {
                _edges[i].Parent = packet.ReadInt32();
                _edges[i].Previous = packet.ReadInt32();
                _edges[i].Next = packet.ReadInt32();
            }
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (CollisionSystem) base.NewInstance();

            copy._contacts = new Contact[_contacts.Length];
            copy._edges = new ContactEdge[_edges.Length];

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CollisionSystem) into;

            if (copy._contacts.Length != _contacts.Length)
            {
                copy._contacts = new Contact[_contacts.Length];
            }
            _contacts.CopyTo(copy._contacts, 0);

            if (copy._edges.Length != _edges.Length)
            {
                copy._edges = new ContactEdge[_edges.Length];
            }
            _edges.CopyTo(copy._edges, 0);
        }

        #endregion

        #region Utility types

        /// <summary>Represents a contact between two collidables.</summary>
        private struct Contact
        {
            /// <summary>Entity id of the first collidables entity.</summary>
            public int ComponentA;

            /// <summary>Entity id of the second collidables entity.</summary>
            public int ComponentB;

            /// <summary>Index of previous entry in the global linked list.</summary>
            public int Previous;

            /// <summary>Index of next entry in the global linked list.</summary>
            public int Next;

            /// <summary>Whether the two entities were intersecting in the previous update.</summary>
            public bool Intersecting;
        }

        /// <summary>Represents a connection between two (potentially) colliding objects.</summary>
        private struct ContactEdge
        {
            /// <summary>The index of the actual contact.</summary>
            public int Parent;

            /// <summary>The index of the previous contact edge, for the entity this edge belongs to.</summary>
            public int Previous;

            /// <summary>The index of the next contact edge, for the entity this edge belongs to.</summary>
            public int Next;
        }

        #endregion
    }
}