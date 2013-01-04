using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.Physics.Components;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics
{
    public static class FactoryManagerExtensions
    {
        #region Body

        /// <summary>
        /// Creates the a new body with the specified properties.
        /// </summary>
        /// <param name="manager">The manager to create the body in.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddBody(this IManager manager, WorldPoint worldPosition, float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                   bool isBullet = false, bool allowSleep = true)
        {
            return manager.AddComponent<Body>(manager.AddEntity())
                .Initialize(worldPosition, worldAngle, type, fixedRotation, isBullet, allowSleep);

        }

        /// <summary>
        /// Creates the a new body with the specified properties.
        /// </summary>
        /// <param name="manager">The manager to create the body in.</param>
        /// <param name="angle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddBody(this IManager manager, float angle = 0, Body.BodyType type = Body.BodyType.Static,
                                   bool fixedRotation = false, bool isBullet = false, bool allowSleep = true)
        {
            return manager.AddBody(WorldPoint.Zero, angle, type, fixedRotation, isBullet, allowSleep);
        }

        #endregion

        #region Circle

        /// <summary>
        /// Creates a circle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localPosition">The position of the circle relative
        /// to the body's local origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager, LocalPoint localPosition, float radius,
                                     WorldPoint worldPosition, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                     bool isBullet = false, bool allowSleep = true, float density = 0,
                                     float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                     uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, type, fixedRotation, isBullet, allowSleep);
            manager.AttachCircle(body, localPosition, radius, density, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates a circle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager, float radius, WorldPoint worldPosition, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                     bool isBullet = false, bool allowSleep = true, float density = 0,
                                     float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                     uint collisionGroups = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius, worldPosition, worldAngle, type, fixedRotation, isBullet,
                                     allowSleep, density, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>
        /// Creates a circle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localPosition">The position of the circle relative
        /// to the body's local origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager, LocalPoint localPosition, float radius, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                     bool isBullet = false, bool allowSleep = true, float density = 0,
                                     float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                     uint collisionGroups = 0)
        {
            return manager.AddCircle(localPosition, radius, WorldPoint.Zero, worldAngle, type, fixedRotation, isBullet,
                                     allowSleep, density, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>
        /// Creates a circle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager, float radius, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                     bool isBullet = false, bool allowSleep = true, float density = 0,
                                     float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                     uint collisionGroups = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius, WorldPoint.Zero, worldAngle, type, fixedRotation, isBullet,
                                     allowSleep, density, friction, restitution, isSensor, collisionGroups);
        }

        #endregion

        #region Edge

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager, LocalPoint localStart, LocalPoint localEnd,
                                   WorldPoint worldPosition, float worldAngle = 0, float friction = 0.2f,
                                   float restitution = 0, bool isSensor = false, uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle);
            manager.AttachEdge(body, localStart, localEnd, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager, LocalPoint localStart, LocalPoint localEnd,
                                   float worldAngle = 0, float friction = 0.2f, float restitution = 0,
                                   bool isSensor = false, uint collisionGroups = 0)
        {
            return manager.AddEdge(localStart, localEnd, WorldPoint.Zero, worldAngle, friction, restitution, isSensor,
                                   collisionGroups);
        }

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="ghostStart">The ghost point for the start point, for chained edges.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="ghostEnd">The ghost point for end point, for chained edges.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager, LocalPoint ghostStart, LocalPoint localStart,
                                   LocalPoint localEnd, LocalPoint ghostEnd, WorldPoint worldPosition,
                                   float worldAngle = 0, float friction = 0.2f, float restitution = 0,
                                   bool isSensor = false, uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle);
            manager.AttachEdge(body, ghostStart, localStart, localEnd, ghostEnd, friction, restitution, isSensor,
                               collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="ghostStart">The ghost point for the start point, for chained edges.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="ghostEnd">The ghost point for end point, for chained edges.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager, LocalPoint ghostStart, LocalPoint localStart,
                                   LocalPoint localEnd, LocalPoint ghostEnd, float worldAngle = 0, float friction = 0.2f,
                                   float restitution = 0, bool isSensor = false, uint collisionGroups = 0)
        {
            return manager.AddEdge(ghostStart, localStart, localEnd, ghostEnd, WorldPoint.Zero, worldAngle, friction,
                                   restitution, isSensor, collisionGroups);
        }

        #endregion

        #region Chain / Loop

        /// <summary>
        /// Creates an edge loop with the specified parameters. A loop consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddLoop(this IManager manager, IList<LocalPoint> vertices, WorldPoint worldPosition,
                                   float worldAngle = 0, float friction = 0.2f, float restitution = 0,
                                   bool isSensor = false, uint collisionGroups = 0)
        {
            FixtureFactory.ValidateVertices(vertices, 3);

            var body = manager.AddBody(worldPosition, worldAngle);
            manager.AttachLoopUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates an edge loop with the specified parameters. A loop consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddLoop(this IManager manager, IList<LocalPoint> vertices, float worldAngle = 0,
                                   float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                   uint collisionGroups = 0)
        {
            FixtureFactory.ValidateVertices(vertices, 3);

            var body = manager.AddBody(WorldPoint.Zero, worldAngle);
            manager.AttachLoopUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates an edge chain with the specified parameters. A chain consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// A proxy object that may be used to interact with the created chain.
        /// </returns>
        public static Chain AddChain(this IManager manager, IList<LocalPoint> vertices, WorldPoint worldPosition,
                                     float worldAngle = 0, float friction = 0.2f, float restitution = 0,
                                     bool isSensor = false, uint collisionGroups = 0)
        {
            FixtureFactory.ValidateVertices(vertices, 2);

            var body = manager.AddBody(worldPosition, worldAngle);
            return manager.AttachChainUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>
        /// Creates an edge chain with the specified parameters. A chain consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// A proxy object that may be used to interact with the created chain.
        /// </returns>
        public static Chain AddChain(this IManager manager, IList<LocalPoint> vertices, float worldAngle = 0,
                                     float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                     uint collisionGroups = 0)
        {
            FixtureFactory.ValidateVertices(vertices, 2);

            var body = manager.AddBody(WorldPoint.Zero, worldAngle);
            return manager.AttachChainUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);
        }

        #endregion

        #region Polygon

        /// <summary>
        /// Creates a polygon with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddPolygon(this IManager manager, IList<LocalPoint> vertices, WorldPoint worldPosition,
                                      float worldAngle = 0, Body.BodyType type = Body.BodyType.Static,
                                      bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                      float density = 0, float friction = 0.2f, float restitution = 0,
                                      bool isSensor = false, uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, type, fixedRotation, isBullet, allowSleep);
            manager.AttachPolygon(body, vertices, density, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates a rectangle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="localPosition">The offset of the rectangle relative to the body's local origin.</param>
        /// <param name="localAngle">The rotation of the rectangle relative to the body.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddRectangle(this IManager manager, float width, float height, LocalPoint localPosition,
                                        float localAngle, WorldPoint worldPosition, float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                        bool isBullet = false, bool allowSleep = true, float density = 0,
                                        float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                        uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, type, fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height, localPosition, localAngle, density, friction, restitution,
                                    isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates a rectangle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddRectangle(this IManager manager, float width, float height, WorldPoint worldPosition,
                                        float worldAngle = 0, Body.BodyType type = Body.BodyType.Static,
                                        bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0,
                                        bool isSensor = false, uint collisionGroups = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, type, fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height, density, friction, restitution, isSensor, collisionGroups);

            return body;
        }

        /// <summary>
        /// Creates a rectangle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="localPosition">The offset of the rectangle relative to the body's local origin.</param>
        /// <param name="localAngle">The rotation of the rectangle relative to the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">if set to <c>true</c> the created fixture is marked
        /// as a sensor (i.e. it will fire collision events but the collision
        /// will not be handled by the solver).</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddRectangle(this IManager manager, float width, float height, LocalPoint localPosition,
                                        float localAngle = 0, float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static, bool fixedRotation = false,
                                        bool isBullet = false, bool allowSleep = true, float density = 0,
                                        float friction = 0.2f, float restitution = 0, bool isSensor = false,
                                        uint collisionGroups = 0)
        {
            var body = manager.AddBody(WorldPoint.Zero, worldAngle, type, fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height, localPosition, localAngle, density, friction, restitution,
                                    isSensor, collisionGroups);

            return body;
        }

        #endregion
    }
}
