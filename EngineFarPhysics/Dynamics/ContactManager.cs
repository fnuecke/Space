/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/
#define USE_ACTIVE_CONTACT_SET

using System.Collections.Generic;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics.Contacts;

namespace FarseerPhysics.Dynamics
{
    public class ContactManager
    {
        /// <summary>
        /// Fires when a contact is created.
        /// </summary>
        public BeginContactDelegate BeginContact;

        /// <summary>
        /// Fires when a contact is deleted.
        /// </summary>
        public EndContactDelegate EndContact;

        /// <summary>
        /// The filter used by the contact manager.
        /// </summary>
        public CollisionFilterDelegate ContactFilter;

        public IBroadPhase BroadPhase { get { return _broadPhase; } }

        private readonly IInternalBroadPhase _broadPhase;

        public List<Contact> ContactList = new List<Contact>(128);

        /// <summary>
        /// Object pooling for contacts.
        /// TODO move to contact class
        /// </summary>
        internal readonly Queue<Contact> ContactPool = new Queue<Contact>(256);

#if USE_ACTIVE_CONTACT_SET
        /// <summary>
        /// The set of active contacts.
        /// </summary>
        public HashSet<Contact> ActiveContacts = new HashSet<Contact>();

        /// <summary>
        /// A temporary copy of active contacts that is used during updates so
        /// the hash set can have members added/removed during the update.
        /// This list is cleared after every update.
        /// </summary>
        List<Contact> ActiveList = new List<Contact>();
#endif

        internal ContactManager(IInternalBroadPhase broadPhase)
        {
            this._broadPhase = broadPhase;
        }

        // Broad-phase callback.
        private void AddPair(ref FixtureProxy proxyA, ref FixtureProxy proxyB)
        {
            var fixtureA = proxyA.Fixture;
            var fixtureB = proxyB.Fixture;

            var indexA = proxyA.ChildIndex;
            var indexB = proxyB.ChildIndex;

            var bodyA = fixtureA.Body;
            var bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA == bodyB)
            {
                return;
            }

            // Does a contact already exist?
            ContactEdge edge = bodyB.ContactList;
            while (edge != null)
            {
                if (edge.Other == bodyA)
                {
                    var fA = edge.Contact.FixtureA;
                    var fB = edge.Contact.FixtureB;
                    var iA = edge.Contact.ChildIndexA;
                    var iB = edge.Contact.ChildIndexB;

                    if (fA == fixtureA && fB == fixtureB && iA == indexA && iB == indexB)
                    {
                        // A contact already exists.
                        return;
                    }

                    if (fA == fixtureB && fB == fixtureA && iA == indexB && iB == indexA)
                    {
                        // A contact already exists.
                        return;
                    }
                }

                edge = edge.Next;
            }

            // Does a joint override collision? Is at least one body dynamic?
            if (bodyB.ShouldCollide(bodyA) == false)
                return;

            //Check default filter
            if (ShouldCollide(fixtureA, fixtureB) == false)
                return;

            // Check user filtering.
            if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                return;

            // Call the factory.
            var contact = Contact.Create(fixtureA, indexA, fixtureB, indexB);

            // Contact creation may swap fixtures.
            fixtureA = contact.FixtureA;
            fixtureB = contact.FixtureB;
            bodyA = fixtureA.Body;
            bodyB = fixtureB.Body;

            // Insert into the world.
            ContactList.Add(contact);

#if USE_ACTIVE_CONTACT_SET
            ActiveContacts.Add(contact);
#endif
            // Connect to island graph.

            // Connect to body A
            contact.NodeA.Contact = contact;
            contact.NodeA.Other = bodyB;

            contact.NodeA.Prev = null;
            contact.NodeA.Next = bodyA.ContactList;
            if (bodyA.ContactList != null)
            {
                bodyA.ContactList.Prev = contact.NodeA;
            }
            bodyA.ContactList = contact.NodeA;

            // Connect to body B
            contact.NodeB.Contact = contact;
            contact.NodeB.Other = bodyA;

            contact.NodeB.Prev = null;
            contact.NodeB.Next = bodyB.ContactList;
            if (bodyB.ContactList != null)
            {
                bodyB.ContactList.Prev = contact.NodeB;
            }
            bodyB.ContactList = contact.NodeB;
        }

        internal void FindNewContacts()
        {
            _broadPhase.UpdatePairs(AddPair);
        }

        internal void Destroy(Contact contact)
        {
            var fixtureA = contact.FixtureA;
            var fixtureB = contact.FixtureB;
            var bodyA = fixtureA.Body;
            var bodyB = fixtureB.Body;

            if (EndContact != null && contact.IsTouching())
            {
                EndContact(contact);
            }

            // Remove from the world.
            ContactList.Remove(contact);

            // Remove from body 1
            if (contact.NodeA.Prev != null)
            {
                contact.NodeA.Prev.Next = contact.NodeA.Next;
            }

            if (contact.NodeA.Next != null)
            {
                contact.NodeA.Next.Prev = contact.NodeA.Prev;
            }

            if (contact.NodeA == bodyA.ContactList)
            {
                bodyA.ContactList = contact.NodeA.Next;
            }

            // Remove from body 2
            if (contact.NodeB.Prev != null)
            {
                contact.NodeB.Prev.Next = contact.NodeB.Next;
            }

            if (contact.NodeB.Next != null)
            {
                contact.NodeB.Next.Prev = contact.NodeB.Prev;
            }

            if (contact.NodeB == bodyB.ContactList)
            {
                bodyB.ContactList = contact.NodeB.Next;
            }

#if USE_ACTIVE_CONTACT_SET
            if (ActiveContacts.Contains(contact))
            {
                ActiveContacts.Remove(contact);
            }
            if (ActiveList.Contains(contact))
            {
                ActiveList.Remove(contact);
            }
#endif
            ActiveContacts.Remove(contact);
            ContactPool.Enqueue(contact);
            contact.Reset(null, 0, null, 0);
        }

        internal void Collide()
        {
            // Update awake contacts.
#if USE_ACTIVE_CONTACT_SET
            ActiveList.AddRange(ActiveContacts);

            // Iterate backwards so that removed contacts don't disturb the
            // iteration, but also bound the value to the list value because
            // it might be cleared / more than one contact may be removed in
            // the meantime.
            for (var i = ActiveList.Count - 1; i >= 0; i = System.Math.Min(i - 1, ActiveList.Count - 1))
            {
                var contact = ActiveList[i];
#else
            for (int i = 0; i < ContactList.Count; i++)
            {
                Contact c = ContactList[i];
#endif
                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;
                var indexA = contact.ChildIndexA;
                var indexB = contact.ChildIndexB;

                //if (fixtureA == null || fixtureB == null)
                //{
                //    ActiveContacts.Remove(c);
                //    continue;
                //}

                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;

                if (bodyA.Awake == false && bodyB.Awake == false)
                {
#if USE_ACTIVE_CONTACT_SET
                    ActiveContacts.Remove(contact);
#endif
                    continue;
                }

                // Is this contact flagged for filtering?
                if ((contact.Flags & ContactFlags.Filter) == ContactFlags.Filter)
                {
                    // Should these bodies collide?
                    if (bodyB.ShouldCollide(bodyA) == false)
                    {
                        Destroy(contact);
                        continue;
                    }

                    // Check default filtering
                    if (ShouldCollide(fixtureA, fixtureB) == false)
                    {
                        Destroy(contact);
                        continue;
                    }

                    // Check user filtering.
                    if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                    {
                        Destroy(contact);
                        continue;
                    }

                    // Clear the filtering flag.
                    contact.Flags &= ~ContactFlags.Filter;
                }

                // Here we destroy contacts that cease to overlap in the broad-phase.
                if (_broadPhase.TestOverlap(fixtureA.Proxies[indexA].ProxyId, fixtureB.Proxies[indexB].ProxyId) == false)
                {
                    Destroy(contact);
                    continue;
                }

                // The contact persists.
                contact.Update(this);
            }

#if USE_ACTIVE_CONTACT_SET
            ActiveList.Clear();
#endif
        }

        private static bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
        {
            if (Settings.UseFPECollisionCategories)
            {
                if (fixtureA.CollisionGroup == fixtureB.CollisionGroup &&
                    fixtureA.CollisionGroup != 0 && fixtureB.CollisionGroup != 0)
                {
                    return false;
                }

                if ((fixtureA.CollisionCategories & fixtureB.CollidesWith) == Category.None &&
                    (fixtureB.CollisionCategories & fixtureA.CollidesWith) == Category.None)
                {
                    return false;
                }

                return true;
            }

            if (fixtureA.CollisionGroup == fixtureB.CollisionGroup &&
                fixtureA.CollisionGroup != 0)
            {
                return fixtureA.CollisionGroup > 0;
            }

            return (fixtureA.CollidesWith & fixtureB.CollisionCategories) != 0 &&
                   (fixtureA.CollisionCategories & fixtureB.CollidesWith) != 0;
        }

        internal void EnableContacts(ContactEdge contactEdge)
        {
            while (contactEdge != null)
            {
                ActiveContacts.Add(contactEdge.Contact);
                contactEdge = contactEdge.Next;
            }
        }

        internal void DisableContacts(ContactEdge contactEdge)
        {
            while (contactEdge != null)
            {
                if (!contactEdge.Other.Awake)
                {
                    ActiveContacts.Remove(contactEdge.Contact);
                }
                contactEdge = contactEdge.Next;
            }
        }
    }
}