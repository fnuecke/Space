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

using System;
using System.Diagnostics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Dynamics.Contacts
{
    /// <summary>
    /// A contact edge is used to connect bodies and contacts together
    /// in a contact graph where each body is a node and each contact
    /// is an edge. A contact edge belongs to a doubly linked list
    /// maintained in each attached body. Each contact has two contact
    /// nodes, one for each attached body.
    /// </summary>
    public sealed class ContactEdge
    {
        /// <summary>
        /// The contact
        /// </summary>
        internal Contact Contact;

        /// <summary>
        /// The next contact edge in the body's contact list
        /// </summary>
        internal ContactEdge Next;

        /// <summary>
        /// Provides quick access to the other body attached.
        /// </summary>
        internal Body Other;

        /// <summary>
        /// The previous contact edge in the body's contact list
        /// </summary>
        internal ContactEdge Prev;
    }

    [Flags]
    internal enum ContactFlags
    {
        None = 0,

        /// <summary>
        /// Used when crawling contact graph when forming islands.
        /// </summary>
        Island = 0x0001,

        /// <summary>
        /// Set when the shapes are touching.
        /// </summary>
        Touching = 0x0002,

        /// <summary>
        /// This contact can be disabled (by user)
        /// </summary>
        Enabled = 0x0004,

        /// <summary>
        /// This contact needs filtering because a fixture filter was changed.
        /// </summary>
        Filter = 0x0008,

        /// <summary>
        /// This bullet contact had a TOI event
        /// </summary>
        BulletHit = 0x0010,

        /// <summary>
        /// This contact has a valid TOI i the field TOI
        /// </summary>
        TOI = 0x0020
    }

    /// <summary>
    /// The class manages contact between two shapes. A contact exists for each overlapping
    /// AABB in the broad-phase (except if filtered). Therefore a contact object may exist
    /// that has no contact points.
    /// </summary>
    public sealed class Contact
    {
        private enum ContactType
        {
            NotSupported,

            Polygon,

            PolygonAndCircle,

            Circle,

            EdgeAndPolygon,

            EdgeAndCircle,

            LoopAndPolygon,

            LoopAndCircle,
        }

        private static readonly ContactType[,] Registers = new[,]
        {
            {
                ContactType.Circle,
                ContactType.EdgeAndCircle,
                ContactType.PolygonAndCircle,
                ContactType.LoopAndCircle
            },
            {
                ContactType.EdgeAndCircle,
                ContactType.NotSupported,
                // 1,1 is invalid (no ContactType.Edge)
                ContactType.EdgeAndPolygon,
                ContactType.NotSupported
                // 1,3 is invalid (no ContactType.EdgeAndLoop)
            },
            {
                ContactType.PolygonAndCircle,
                ContactType.EdgeAndPolygon,
                ContactType.Polygon,
                ContactType.LoopAndPolygon
            },
            {
                ContactType.LoopAndCircle,
                ContactType.NotSupported,
                // 3,1 is invalid (no ContactType.EdgeAndLoop)
                ContactType.LoopAndPolygon,
                ContactType.NotSupported
                // 3,3 is invalid (no ContactType.Loop)
            }
        };

        /// <summary>
        /// Creates the specified fixture A.
        /// </summary>
        /// <param name="fixtureA">The fixture A.</param>
        /// <param name="indexA">The index A.</param>
        /// <param name="fixtureB">The fixture B.</param>
        /// <param name="indexB">The index B.</param>
        /// <returns></returns>
        internal static Contact Create(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            var typeA = fixtureA.ShapeType;
            var typeB = fixtureB.ShapeType;

            Debug.Assert(ShapeType.Unknown < typeA && typeA < ShapeType.TypeCount);
            Debug.Assert(ShapeType.Unknown < typeB && typeB < ShapeType.TypeCount);

            Contact c;
            var pool = fixtureA.Body.World.ContactManager.ContactPool;
            if (pool.Count > 0)
            {
                c = pool.Dequeue();
                if ((typeA >= typeB || (typeA == ShapeType.Edge && typeB == ShapeType.Polygon)) && !(typeB == ShapeType.Edge && typeA == ShapeType.Polygon))
                {
                    c.Reset(fixtureA, indexA, fixtureB, indexB);
                }
                else
                {
                    c.Reset(fixtureB, indexB, fixtureA, indexA);
                }
            }
            else
            {
                // Edge+Polygon is non-symetrical due to the way Erin handles collision type registration.
                if ((typeA >= typeB || (typeA == ShapeType.Edge && typeB == ShapeType.Polygon)) && !(typeB == ShapeType.Edge && typeA == ShapeType.Polygon))
                {
                    c = new Contact(fixtureA, indexA, fixtureB, indexB);
                }
                else
                {
                    c = new Contact(fixtureB, indexB, fixtureA, indexA);
                }
            }

            c._type = Registers[(int)typeA, (int)typeB];

            return c;
        }

        /// Enable/disable this contact. This can be used inside the pre-solve
        /// contact listener. The contact is only disabled for the current
        /// time step (or sub-step in continuous collisions).
        /// NOTE: If you are setting Enabled to a constant true or false,
        /// use the explicit Enable() or Disable() functions instead to 
        /// save the CPU from doing a branch operation.
        public bool Enabled
        {
            set
            {
                if (value)
                {
                    Flags |= ContactFlags.Enabled;
                }
                else
                {
                    Flags &= ~ContactFlags.Enabled;
                }
            }

            get { return (Flags & ContactFlags.Enabled) == ContactFlags.Enabled; }
        }

        /// <summary>
        /// Enable this contact.
        /// </summary>
        public void Enable()
        {
            Flags |= ContactFlags.Enabled;
        }

        /// <summary>
        /// Disable this contact.
        /// </summary>
        public void Disable()
        {
            Flags &= ~ContactFlags.Enabled;
        }

        /// <summary>
        /// The first involved fixture.
        /// </summary>
        public Fixture FixtureA;

        /// <summary>
        /// The second involved fixture.
        /// </summary>
        public Fixture FixtureB;

        /// <summary>
        /// The contact manifold.
        /// </summary>
        public Manifold Manifold;

        internal ContactFlags Flags;

        /// <summary>
        /// Get the child primitive index for fixture A.
        /// </summary>
        internal int ChildIndexA;

        /// <summary>
        /// Get the child primitive index for fixture B.
        /// </summary>
        internal int ChildIndexB;

        // Nodes for connecting bodies.
        internal readonly ContactEdge NodeA = new ContactEdge();

        internal readonly ContactEdge NodeB = new ContactEdge();

        internal float TOI = 1f;

        internal int TOICount;

        private ContactType _type;

        // Temporary variable for Evaluate(), as a field to avoid repetitive
        // allocation, non-static to avoid issues in multi-threaded environments.
        private EdgeShape _edge = new EdgeShape();

        private Contact(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        internal void Reset(Fixture fA, int indexA, Fixture fB, int indexB)
        {
            Flags = ContactFlags.Enabled;

            FixtureA = fA;
            FixtureB = fB;

            ChildIndexA = indexA;
            ChildIndexB = indexB;

            Manifold.PointCount = 0;

            NodeA.Contact = null;
            NodeA.Prev = null;
            NodeA.Next = null;
            NodeA.Other = null;

            NodeB.Contact = null;
            NodeB.Prev = null;
            NodeB.Next = null;
            NodeB.Other = null;

            TOI = 1f;
            TOICount = 0;

            _type = ContactType.NotSupported;
        }

        /// <summary>
        /// Gets the world manifold.
        /// </summary>
        public void GetWorldManifold(out Vector2 normal, out FixedArray2<WorldVector2> points)
        {
            Collision.Collision.GetWorldManifold(ref Manifold, ref FixtureA.Body.Xf, FixtureA.Shape.Radius,
                                                 ref FixtureB.Body.Xf, FixtureB.Shape.Radius, out normal, out points);
        }

        /// <summary>
        /// Determines whether this contact is touching.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is touching; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsTouching()
        {
            return (Flags & ContactFlags.Touching) == ContactFlags.Touching;
        }

        /// <summary>
        /// Flag this contact for filtering. Filtering will occur the next time step.
        /// </summary>
        internal void FlagForFiltering()
        {
            Flags |= ContactFlags.Filter;
        }

        /// <summary>
        /// Update the contact manifold and touching status.
        /// Note: do not assume the fixture AABBs are overlapping or are valid.
        /// </summary>
        /// <param name="contactManager">The contact manager.</param>
        internal void Update(ContactManager contactManager)
        {
            var oldManifold = Manifold;

            // Re-enable this contact.
            Flags |= ContactFlags.Enabled;

            bool touching;
            var wasTouching = (Flags & ContactFlags.Touching) == ContactFlags.Touching;

            var sensor = FixtureA.IsSensor || FixtureB.IsSensor;

            var bodyA = FixtureA.Body;
            var bodyB = FixtureB.Body;

            // Is this contact a sensor?
            if (sensor)
            {
                var shapeA = FixtureA.Shape;
                var shapeB = FixtureB.Shape;
                touching = AABB.TestOverlap(shapeA, ChildIndexA, shapeB, ChildIndexB, ref bodyA.Xf, ref bodyB.Xf);

                // Sensors don't generate manifolds.
                Manifold.PointCount = 0;
            }
            else
            {
                Evaluate(ref Manifold, ref bodyA.Xf, ref bodyB.Xf);
                touching = Manifold.PointCount > 0;

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                for (var i = 0; i < Manifold.PointCount; ++i)
                {
                    var mp2 = Manifold.Points[i];
                    mp2.NormalImpulse = 0.0f;
                    mp2.TangentImpulse = 0.0f;
                    var id2 = mp2.Id;
                    var found = false;

                    for (var j = 0; j < oldManifold.PointCount; ++j)
                    {
                        var mp1 = oldManifold.Points[j];

                        if (mp1.Id.Key == id2.Key)
                        {
                            mp2.NormalImpulse = mp1.NormalImpulse;
                            mp2.TangentImpulse = mp1.TangentImpulse;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        mp2.NormalImpulse = 0.0f;
                        mp2.TangentImpulse = 0.0f;
                    }

                    Manifold.Points[i] = mp2;
                }

                if (touching != wasTouching)
                {
                    bodyA.Awake = true;
                    bodyB.Awake = true;
                }
            }

            if (touching)
            {
                Flags |= ContactFlags.Touching;
            }
            else
            {
                Flags &= ~ContactFlags.Touching;
            }

            if (!wasTouching)
            {
                if (touching)
                {
                    // BeginContact can also return false and disable the contact.
                    if (contactManager.BeginContact != null)
                    {
                        Enabled = contactManager.BeginContact(this);
                    }

                    // If the user disabled the contact (needed to exclude it in TOI solver) at any point by
                    // any of the callbacks, we need to mark it as not touching and call any separation
                    // callbacks for fixtures that didn't explicitly disable the collision.
                    if (!Enabled)
                    {
                        Flags &= ~ContactFlags.Touching;
                    }
                }
            }
            else
            {
                if (touching == false)
                {
                    if (contactManager.EndContact != null)
                        contactManager.EndContact(this);
                }
            }
        }

        /// <summary>
        /// Evaluate this contact with your own manifold and transforms.   
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="transformA">The first transform.</param>
        /// <param name="transformB">The second transform.</param>
        private void Evaluate(ref Manifold manifold, ref Transform transformA, ref Transform transformB)
        {
            switch (_type)
            {
                case ContactType.Polygon:
                    Collision.Collision.CollidePolygons(ref manifold,
                                                        (PolygonShape)FixtureA.Shape, ref transformA,
                                                        (PolygonShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.PolygonAndCircle:
                    Collision.Collision.CollidePolygonAndCircle(ref manifold,
                                                                (PolygonShape)FixtureA.Shape, ref transformA,
                                                                (CircleShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.EdgeAndCircle:
                    Collision.Collision.CollideEdgeAndCircle(ref manifold,
                                                             (EdgeShape)FixtureA.Shape, ref transformA,
                                                             (CircleShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.EdgeAndPolygon:
                    Collision.Collision.CollideEdgeAndPolygon(ref manifold,
                                                              (EdgeShape)FixtureA.Shape, ref transformA,
                                                              (PolygonShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.LoopAndCircle:
                    ((LoopShape)FixtureA.Shape).GetChildEdge(ref _edge, ChildIndexA);
                    Collision.Collision.CollideEdgeAndCircle(ref manifold, _edge, ref transformA,
                                                             (CircleShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.LoopAndPolygon:
                    ((LoopShape)FixtureA.Shape).GetChildEdge(ref _edge, ChildIndexA);
                    Collision.Collision.CollideEdgeAndPolygon(ref manifold, _edge, ref transformA,
                                                              (PolygonShape)FixtureB.Shape, ref transformB);
                    break;
                case ContactType.Circle:
                    Collision.Collision.CollideCircles(ref manifold,
                                                       (CircleShape)FixtureA.Shape, ref transformA,
                                                       (CircleShape)FixtureB.Shape, ref transformB);
                    break;
            }
        }
    }
}