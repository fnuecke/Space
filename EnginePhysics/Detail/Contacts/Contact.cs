using Engine.ComponentSystem;
using Engine.Physics.Components;
using Engine.Physics.Detail.Collision;
using Engine.Physics.Detail.Math;
using Engine.Physics.Messages;
using Engine.Serialization;
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

namespace Engine.Physics.Detail.Contacts
{
    /// <summary>
    /// Possible contact types (i.e. possible fixture type permutations).
    /// </summary>
    internal enum ContactType
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
        CircleEdge,
        EdgeCircle,

        /// <summary>
        /// Edge collides with polygon.
        /// </summary>
        EdgePolygon,
        PolygonEdge,

        /// <summary>
        /// Polygon collides with circle.
        /// </summary>
        CirclePolygon,
        PolygonCircle
    }

    /// <summary>
    /// Represents a contact between two fixtures.
    /// </summary>
    internal sealed class Contact : IPacketizable
    {
        #region Linked list data (unused/free)

        /// <summary>
        /// Index of previous entry in the global linked list.
        /// </summary>
        public int Previous;

        /// <summary>
        /// Index of next entry in the global linked list.
        /// </summary>
        public int Next;

        #endregion

        #region Context

        /// <summary>
        /// Component id of the first fixture.
        /// </summary>
        public int FixtureA;

        /// <summary>
        /// Component id of the second fixture.
        /// </summary>
        public int FixtureB;

        /// <summary>
        /// The friction between the two fixtures.
        /// </summary>
        public float Friction;

        /// <summary>
        /// The restitution between the two fixtures.
        /// </summary>
        public float Restitution;

        /// <summary>
        /// Whether the two involved fixtures are intersecting.
        /// </summary>
        public bool IsTouching;

        /// <summary>
        /// Whether this contact is currently enabled.
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// The contact manifold for this contact.
        /// </summary>
        public Manifold Manifold;

        /// <summary>
        /// The type of this contact (used to look-up evaluation method).
        /// </summary>
        private ContactType _type;

        #endregion

        #region TOI Cache

        /// <summary>
        /// The number of iterations this contact was involved in.
        /// </summary>
        public int ToiCount;

        /// <summary>
        /// Whether the contact has a valid, cached TOI value.
        /// </summary>
        public bool HasCachedTOI;

        /// <summary>
        /// The cached TOI value.
        /// </summary>
        public float TOI;

        #endregion

        #region Logic

        /// <summary>
        /// Initializes the contact to represent a contact between the two specified fixtures.
        /// </summary>
        /// <param name="fixtureA">The first fixture.</param>
        /// <param name="fixtureB">The second fixture.</param>
        public void Initialize(Fixture fixtureA, Fixture fixtureB)
        {
            FixtureA = fixtureA.Id;
            FixtureB = fixtureB.Id;
            IsTouching = false;
            _type = ContactTypes[(int)fixtureA.Type, (int)fixtureB.Type];
            Friction = MixFriction(fixtureA.Friction, fixtureB.Friction);
            Restitution = MixRestitution(fixtureA.Restitution, fixtureB.Restitution);
        }

        /// <summary>
        /// Updates the contact manifold and touching status.
        /// </summary>
        /// <param name="manager">The manager to send messages to (contact begin/end).</param>
        /// <param name="fixtureA">The first involved fixture.</param>
        /// <param name="fixtureB">The second involved fixture.</param>
        /// <param name="bodyA">The first involved body.</param>
        /// <param name="bodyB">The second involved body.</param>
        public void Update(IManager manager, Fixture fixtureA, Fixture fixtureB, Body bodyA, Body bodyB)
        {
            // Note: do not assume the fixture AABBs are overlapping or are valid.
            var oldManifold = Manifold;

            // Re-enable this contact.
            IsEnabled = true;

            // Update the contact manifold and touching status.
            if (ContactEvaluators[(int)_type](fixtureA, bodyA.Transform, fixtureB, bodyB.Transform, out Manifold))
            {
                // The two are intersecting!

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                System.Diagnostics.Debug.Assert(Manifold.PointCount > 0);
                var i = 0;
                do
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
                } while (++i < Manifold.PointCount);

                // See if anything changed.
                if (!IsTouching)
                {
                    // Got a new intersection.
                    IsTouching = true;

                    // Make sure the two involved bodies are awake.
                    bodyA.IsAwake = true;
                    bodyB.IsAwake = true;

                    // Send message.
                    BeginContact message;
                    message.ContactId = FixtureA ^ FixtureB;
                    message.EntityA = fixtureA.Id;
                    message.EntityB = fixtureB.Id;
                    // TODO pass back some proxy that can be used to compute world space info as necessary?
                    message.Normal = Vector2.Zero;
                    manager.SendMessage(message);
                }
            }
            else
            {
                // Not intersecting. See if anything changed.
                if (IsTouching)
                {
                    // Yes, and the contact was active, but is no longer.
                    IsTouching = false;

                    // Make sure the two involved bodies are awake.
                    bodyA.IsAwake = true;
                    bodyB.IsAwake = true;

                    // Send message.
                    EndContact message;
                    message.ContactId = FixtureA ^ FixtureB;
                    message.EntityA = fixtureA.Id;
                    message.EntityB = fixtureB.Id;
                    manager.SendMessage(message);
                }
            }
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
                ContactType.CircleEdge,
                // B = Polygon
                ContactType.CirclePolygon
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
                ContactType.PolygonEdge,
                // B = Polygon
                ContactType.Polygon
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
            Algorithms.CollideCircleAndEdge,
            Algorithms.CollideEdgeAndCircle,
            Algorithms.CollideEdgeAndPolygon,
            Algorithms.CollidePolygonAndEdge,
            Algorithms.CollideCircleAndPolygon,
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

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Previous)
                .Write(Next)
                .Write(FixtureA)
                .Write(FixtureB)
                .Write(Friction)
                .Write(Restitution)
                .Write(IsTouching)
                .Write(Manifold)
                .Write((byte)_type);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Previous = packet.ReadInt32();
            Next = packet.ReadInt32();
            FixtureA = packet.ReadInt32();
            FixtureB = packet.ReadInt32();
            Friction = packet.ReadSingle();
            Restitution = packet.ReadSingle();
            IsTouching = packet.ReadBoolean();
            Manifold = packet.ReadManifold();
            _type = (ContactType)packet.ReadByte();
        }

        #endregion
    }

    /// <summary>
    /// Represents a connection between two (potentially) colliding
    /// objects.
    /// </summary>
    internal sealed class ContactEdge : IPacketizable
    {
        #region Fields

        /// <summary>
        /// The index of the actual contact.
        /// </summary>
        public int Parent;

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

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Parent)
                .Write(Other)
                .Write(Previous)
                .Write(Next);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Parent = packet.ReadInt32();
            Other = packet.ReadInt32();
            Previous = packet.ReadInt32();
            Next = packet.ReadInt32();
        }

        #endregion
    }

}
