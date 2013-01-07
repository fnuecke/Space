using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Engine.Collections;
using Engine.ComponentSystem;
using Engine.Physics.Components;
using Engine.Physics.Collision;
using Engine.Physics.Math;
using Engine.Physics.Messages;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Contacts
{
    /// <summary>
    /// Represents a contact between two fixtures.
    /// </summary>
    [DebuggerDisplay("FixtureA = {FixtureIdA}, FixtureB = {FixtureIdB}, IsTouching = {IsTouching}")]
    public sealed class Contact : ICopyable<Contact>, IPacketizable
    {
        #region Linked list data (unused/free)

        /// <summary>
        /// Index of previous entry in the global linked list.
        /// </summary>
        internal int Previous;

        /// <summary>
        /// Index of next entry in the global linked list.
        /// </summary>
        internal int Next;

        #endregion

        #region Context

        /// <summary>
        /// Component id of the first fixture.
        /// </summary>
        internal int FixtureIdA;

        /// <summary>
        /// Component id of the second fixture.
        /// </summary>
        internal int FixtureIdB;

        /// <summary>
        /// The friction between the two fixtures.
        /// </summary>
        internal float Friction;

        /// <summary>
        /// The restitution between the two fixtures.
        /// </summary>
        internal float Restitution;

        /// <summary>
        /// Whether the two involved fixtures are intersecting.
        /// </summary>
        internal bool IsTouching;

        /// <summary>
        /// Whether this contact is currently enabled.
        /// </summary>
        internal bool IsEnabled;

        /// <summary>
        /// Whether the contact is flagged for refiltering (from changes
        /// to the involved bodies, e.g. from adding joints).
        /// </summary>
        internal bool ShouldFilter;

        /// <summary>
        /// The contact manifold for this contact.
        /// </summary>
        internal Manifold Manifold;

        /// <summary>
        /// The type of this contact (used to look-up evaluation method).
        /// </summary>
        private ContactType _type;

        #endregion

        #region TOI Cache

        /// <summary>
        /// The number of iterations this contact was involved in.
        /// </summary>
        internal int ToiCount;

        /// <summary>
        /// Whether the contact has a valid, cached TOI value.
        /// </summary>
        internal bool HasCachedTOI;

        /// <summary>
        /// The cached TOI value.
        /// </summary>
        internal float TOI;

        #endregion

        #region Interface

        /// <summary>
        /// The manager of the simulation this contact lives in. Used to look up
        /// involved members.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        internal IManager Manager;

        /// <summary>
        /// Gets the first fixture involved in this contact.
        /// </summary>
        public Fixture FixtureA
        {
            get { return Manager.GetComponentById(FixtureIdA) as Fixture; }
        }

        /// <summary>
        /// Gets the second fixture involved in this contact.
        /// </summary>
        public Fixture FixtureB
        {
            get { return Manager.GetComponentById(FixtureIdB) as Fixture; }
        }

        /// <summary>
        /// Gets the normal impulse of the specified contact point (separation).
        /// </summary>
        public float GetNormalImpulse(int point)
        {
            return Manifold.Points[point].NormalImpulse;
        }

        /// <summary>
        /// Gets the tangent impulse of the specified contact point (friction).
        /// </summary>
        public float GetTangentImpulse(int point)
        {
            return Manifold.Points[point].TangentImpulse;
        }

        /// <summary>
        /// Computes the world manifold data for this contact. This is relatively
        /// expensive, so use with care.
        /// </summary>
        /// <param name="normal">The world contact normal.</param>
        /// <param name="points">The contact points.</param>
        public void ComputeWorldManifold(out Vector2 normal, out IList<WorldPoint> points)
        {
            if (Manifold.PointCount < 1)
            {
                normal = Vector2.Zero;
                points = new FixedArray2<WorldPoint>();
                return;
            }

            var bodyA = FixtureA.Body;
            var bodyB = FixtureB.Body;
            var transformA = bodyA.Transform;
            var transformB = bodyB.Transform;
            var radiusA = FixtureA.Radius;
            var radiusB = FixtureB.Radius;

            FixedArray2<WorldPoint> worldPoints;
            Manifold.ComputeWorldManifold(transformA, radiusA, transformB, radiusB, out normal, out worldPoints);
            worldPoints.Count = Manifold.PointCount;
            points = worldPoints;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Initializes the contact to represent a contact between the two specified fixtures.
        /// </summary>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        internal void Initialize(ref Fixture fixtureA, ref Fixture fixtureB)
        {
            if (SwapFixtures[(int)fixtureA.Type, (int)fixtureB.Type])
            {
                var tmp = fixtureA;
                fixtureA = fixtureB;
                fixtureB = tmp;
            }
            FixtureIdA = fixtureA.Id;
            FixtureIdB = fixtureB.Id;
            _type = ContactTypes[(int)fixtureA.Type, (int)fixtureB.Type];
            Friction = MixFriction(fixtureA.Friction, fixtureB.Friction);
            Restitution = MixRestitution(fixtureA.Restitution, fixtureB.Restitution);
            IsTouching = false;
        }

        /// <summary>
        /// Updates the contact manifold and touching status.
        /// </summary>
        /// <param name="fixtureA">The first involved fixture.</param>
        /// <param name="fixtureB">The second involved fixture.</param>
        /// <param name="bodyA">The first involved body.</param>
        /// <param name="bodyB">The second involved body.</param>
        /// <param name="proxyA">The distance proxy ro use for the first fixture.</param>
        /// <param name="proxyB">The distance proxy ro use for the first fixture.</param>
        /// <remarks>
        /// We pass the proxy singletons along to avoid having to recreate them
        /// or store static versions in the collision module. This is not exactly
        /// nice, but at least it avoid running into issues when running multiple
        /// simulations at a time (in different threads).
        /// </remarks>
        internal void Update(Fixture fixtureA, Fixture fixtureB,
                             Body bodyA, Body bodyB,
                             Algorithms.DistanceProxy proxyA, Algorithms.DistanceProxy proxyB)
        {
            // Note: do not assume the fixture AABBs are overlapping or are valid.
            var oldManifold = Manifold;

            // Re-enable this contact.
            IsEnabled = true;

            // Check if a sensor is involved.
            var sensor = fixtureA.IsSensorInternal || fixtureB.IsSensorInternal;

            // See how we need to update this contact.
            bool nowTouching, wasTouching = IsTouching;
            if (sensor)
            {
                // Sensors don't generate manifolds.
                Manifold.PointCount = 0;

                // Just check if the fixtures overlap.
                proxyA.Set(fixtureA);
                proxyB.Set(fixtureB);
                nowTouching = Algorithms.TestOverlap(proxyA, proxyB,
                                                     bodyA.Transform, bodyB.Transform);
            }
            else
            {
                // Update the contact manifold and touching status.
                nowTouching = ContactEvaluators[(int)_type](fixtureA, bodyA.Transform,
                                                            fixtureB, bodyB.Transform,
                                                            out Manifold);

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                for (var i = 0; i < Manifold.PointCount; ++i)
                {
                    var p = Manifold.Points[i];
                    p.NormalImpulse = 0;
                    p.TangentImpulse = 0;

                    for (var j = 0; j < oldManifold.PointCount; ++j)
                    {
                        var p0 = oldManifold.Points[j];
                        if (p.Id.Key == p0.Id.Key)
                        {
                            p.NormalImpulse = p0.NormalImpulse;
                            p.TangentImpulse = p0.TangentImpulse;
                            break;
                        }
                    }

                    Manifold.Points[i] = p;
                }

                // Make sure the two involved bodies are awake if something changed.
                if (nowTouching != wasTouching)
                {
                    bodyA.IsAwake = true;
                    bodyB.IsAwake = true;
                }
            }

            // Update our flag.
            IsTouching = nowTouching;

            // See if anything changed.
            if (!nowTouching && wasTouching)
            {
                // Yes, and the contact was active, but is no longer.
                EndContact message;
                message.Contact = this;
                Manager.SendMessage(message);
            }
            else if (nowTouching && !wasTouching)
            {
                // Got a new intersection.
                BeginContact message;
                message.Contact = this;
                Manager.SendMessage(message);
            }

            // Send presolve message if we're not a sensor.
            if (!sensor && IsTouching)
            {
                PreSolve message;
                message.Contact = this;
                message.OldManifold = oldManifold;
                Manager.SendMessage(message);
            }
        }

        /// <summary>
        /// Possible contact types (i.e. possible fixture type permutations).
        /// </summary>
        private enum ContactType
        {
            /// <summary>
            /// Circle collides with circle.
            /// </summary>
            Circle,

            /// <summary>
            /// Edge collides with edge. This is unused.
            /// </summary>
            Edge,

            /// <summary>
            /// Polygon collides with polygon.
            /// </summary>
            Polygon,

            /// <summary>
            /// Edge collides with circle.
            /// </summary>
            EdgeCircle,

            /// <summary>
            /// Edge collides with polygon.
            /// </summary>
            EdgePolygon,

            /// <summary>
            /// Polygon collides with circle.
            /// </summary>
            PolygonCircle
        }

        /// <summary>
        /// Lookup table for contact types by fixture types. This table must be
        /// symmetric, i.e. for any x,y it must hold that table[x,y] == table[y,x].
        /// </summary>
        private static readonly ContactType[,] ContactTypes = new[,]
        {
            // A = Circle
            {
                // B = Circle
                ContactType.Circle,
                // B = Edge
                ContactType.EdgeCircle,
                // B = Polygon
                ContactType.PolygonCircle
            },
            // A = Edge
            {
                // B = Circle
                ContactType.EdgeCircle,
                // B = Edge
                ContactType.Edge,
                // B = Polygon
                ContactType.EdgePolygon
            },
            // A = Polygon
            {
                // B = Circle
                ContactType.PolygonCircle,
                // B = Edge
                ContactType.EdgePolygon,
                // B = Polygon
                ContactType.Polygon
            }
        };

        /// <summary>
        /// Lookup table marking whether fixtures should be stored in reverse
        /// order. This is necessary for the collision detection routines that
        /// depend on the order of shapes.
        /// </summary>
        private static readonly bool[,] SwapFixtures = new[,]
        {
            // A = Circle
            {
                // B = Circle
                false,
                // B = Edge
                true,
                // B = Polygon
                true
            },
            // A = Edge
            {
                // B = Circle
                false,
                // B = Edge
                false,
                // B = Polygon
                false
            },
            // A = Polygon
            {
                // B = Circle
                false,
                // B = Edge
                true,
                // B = Polygon
                false
            }
        };

        /// <summary>
        /// Signature for contact evaluation methods for contacts.
        /// </summary>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="xfA">The world transform of the body of the first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        /// <param name="xfB">The world transform of the body of the second fixture.</param>
        /// <param name="manifold">The resulting contact manifold.</param>
        /// <returns></returns>
        private delegate bool ContactEvaluator(Fixture fixtureA, WorldTransform xfA,
                                               Fixture fixtureB, WorldTransform xfB,
                                               out Manifold manifold);

        /// <summary>
        /// Lookup table for contact evaluators, taking values of the <see cref="ContactType"/>
        /// enum as keys.
        /// </summary>
        private static readonly ContactEvaluator[] ContactEvaluators = new ContactEvaluator[]
        {
            Algorithms.CollideCircles,
            null, // Edge with edge is not supported (because edges have no volume).
            Algorithms.CollidePolygons,
            Algorithms.CollideEdgeAndCircle,
            Algorithms.CollideEdgeAndPolygon,
            Algorithms.CollidePolygonAndCircle
        };

        /// <summary>
        /// Friction mixing law. The idea is to allow either fixture to drive the restitution to zero.
        /// For example, anything slides on ice.
        /// </summary>
        private static float MixFriction(float friction1, float friction2)
        {
            return (float)System.Math.Sqrt(friction1 * friction2);
        }

        /// <summary>
        /// Restitution mixing law. The idea is allow for anything to bounce off an inelastic surface.
        /// For example, a superball bounces on anything.
        /// </summary>
        private static float MixRestitution(float restitution1, float restitution2)
        {
            return restitution1 > restitution2 ? restitution1 : restitution2;
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public Contact NewInstance()
        {
            return new Contact();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(Contact into)
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
            return "Contact: Type=" + _type +
                   ", Previous=" + Previous +
                   ", Next=" + Next +
                   ", FixtureA=" + FixtureIdA +
                   ", FixtureB=" + FixtureIdB +
                   ", Friction=" + Friction.ToString(CultureInfo.InvariantCulture) +
                   ", Restitution=" + Restitution.ToString(CultureInfo.InvariantCulture) +
                   ", IsTouching=" + IsTouching +
                   ", IsEnabled=" + IsEnabled +
                   ", Manifold=" + Manifold;
        }

        #endregion
    }

    /// <summary>
    /// Represents a connection between two (potentially) colliding
    /// objects.
    /// </summary>
    internal sealed class ContactEdge : ICopyable<ContactEdge>, IPacketizable
    {
        #region Fields

        /// <summary>
        /// The index of the actual contact.
        /// </summary>
        public int Contact;

        /// <summary>
        /// The id of the other entity involved in this contact.
        /// </summary>
        public int Other;

        /// <summary>
        /// The index of the previous contact edge, for the entity this
        /// edge belongs to.
        /// </summary>
        public int Previous;

        /// <summary>
        /// The index of the next contact edge, for the entity this
        /// edge belongs to.
        /// </summary>
        public int Next;

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public ContactEdge NewInstance()
        {
            return new ContactEdge();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(ContactEdge into)
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
            return "ContactEdge: Contact=" + Contact +
                ", Other=" + Other +
                ", Previous=" + Previous +
                ", Next=" + Next;
        }

        #endregion
    }
}
