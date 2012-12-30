using System.Collections.Generic;
using Engine.ComponentSystem;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Components
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
        /// <param name="collisionGroups">The collision groups.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddBody(this IManager manager,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true)
        {
            return manager.AddComponent<Body>(manager.AddEntity())
                .Initialize(type)
                .Initialize(collisionGroups)
                .Initialize(isBullet, allowSleep)
                .Initialize(worldPosition, worldAngle);
        }

        /// <summary>
        /// Creates the a new body with the specified properties.
        /// </summary>
        /// <param name="manager">The manager to create the body in.</param>
        /// <param name="angle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddBody(this IManager manager,
                                   float angle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true)
        {
            return manager.AddBody(WorldPoint.Zero, angle,
                                   type, collisionGroups,
                                   isBullet, allowSleep);
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager,
                                     LocalPoint localPosition, float radius,
                                     WorldPoint worldPosition, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static,
                                     uint collisionGroups = 0,
                                     bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachCircle(body,
                                 localPosition, radius,
                                 density, friction, restitution);

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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager,
                                     float radius,
                                     WorldPoint worldPosition, float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static,
                                     uint collisionGroups = 0,
                                     bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius,
                                     worldPosition, worldAngle,
                                     type, collisionGroups,
                                     isBullet, allowSleep,
                                     density, friction, restitution);
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager,
                                     LocalPoint localPosition, float radius,
                                     float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static,
                                     uint collisionGroups = 0,
                                     bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(localPosition, radius,
                                     WorldPoint.Zero, worldAngle,
                                     type, collisionGroups,
                                     isBullet, allowSleep,
                                     density, friction, restitution);
        }

        /// <summary>
        /// Creates a circle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddCircle(this IManager manager,
                                     float radius,
                                     float worldAngle = 0,
                                     Body.BodyType type = Body.BodyType.Static,
                                     uint collisionGroups = 0,
                                     bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius,
                                     WorldPoint.Zero, worldAngle,
                                     type, collisionGroups,
                                     isBullet, allowSleep,
                                     density, friction, restitution);
        }

        /// <summary>
        /// Attaches a circle fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="localPosition">The center of the circle relative to the body's local origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static CircleFixture AttachCircle(this IManager manager, Body body,
                                                 LocalPoint localPosition, float radius,
                                                 float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var fixture = manager.AddComponent<CircleFixture>(body.Entity);
            fixture.Initialize(density, friction, restitution);
            fixture.InitializeShape(radius, localPosition);

            // Recompute mass data if necessary.
            if (density > 0)
            {
                body.ResetMassData();
            }

            return fixture;
        }

        /// <summary>
        /// Attaches a circle fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static CircleFixture AttachCircle(this IManager manager, Body body,
                                                 float radius,
                                                 float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AttachCircle(body,
                                        LocalPoint.Zero, radius,
                                        density, friction, restitution);
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
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint localStart, LocalPoint localEnd,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true,
                                   float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachEdge(body,
                               localStart, localEnd,
                               friction, restitution);

            return body;
        }

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint localStart, LocalPoint localEnd,
                                   float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true,
                                   float friction = 0.2f, float restitution = 0)
        {
            return manager.AddEdge(localStart, localEnd,
                                   WorldPoint.Zero, worldAngle,
                                   type, collisionGroups,
                                   isBullet, allowSleep,
                                   friction, restitution);
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
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint ghostStart, LocalPoint localStart, LocalPoint localEnd,
                                   LocalPoint ghostEnd,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true,
                                   float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachEdge(body,
                               ghostStart, localStart, localEnd, ghostEnd,
                               friction, restitution);

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
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint ghostStart, LocalPoint localStart, LocalPoint localEnd,
                                   LocalPoint ghostEnd,
                                   float worldAngle = 0,
                                   Body.BodyType type = Body.BodyType.Static,
                                   uint collisionGroups = 0,
                                   bool isBullet = false, bool allowSleep = true,
                                   float friction = 0.2f, float restitution = 0)
        {
            return manager.AddEdge(ghostStart, localStart, localEnd, ghostEnd,
                                   WorldPoint.Zero, worldAngle,
                                   type, collisionGroups,
                                   isBullet, allowSleep,
                                   friction, restitution);
        }

        /// <summary>
        /// Attaches an edge fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static EdgeFixture AttachEdge(this IManager manager, Body body,
                                             LocalPoint localStart, LocalPoint localEnd,
                                             float friction = 0.2f, float restitution = 0)
        {
            var fixture = manager.AddComponent<EdgeFixture>(body.Entity);
            fixture.Initialize(friction: friction, restitution: restitution);
            fixture.InitializeShape(localStart, localEnd);
            return fixture;
        }

        /// <summary>
        /// Attaches an edge fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="ghostStart">The ghost point for the start point, for chained edges.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="ghostEnd">The ghost point for end point, for chained edges.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static EdgeFixture AttachEdge(this IManager manager, Body body,
                                             LocalPoint ghostStart, LocalPoint localStart, LocalPoint localEnd,
                                             LocalPoint ghostEnd,
                                             float friction = 0.2f, float restitution = 0)
        {
            var fixture = manager.AddComponent<EdgeFixture>(body.Entity);
            fixture.Initialize(friction: friction, restitution: restitution);
            fixture.InitializeShape(ghostStart, localStart, localEnd, ghostEnd);
            return fixture;
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddPolygon(this IManager manager,
                                      IList<LocalPoint> vertices,
                                      WorldPoint worldPosition, float worldAngle = 0,
                                      Body.BodyType type = Body.BodyType.Static,
                                      uint collisionGroups = 0,
                                      bool isBullet = false, bool allowSleep = true,
                                      float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachPolygon(body,
                                  vertices,
                                  density, friction, restitution);

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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddRectangle(this IManager manager,
                                        float width, float height,
                                        WorldPoint worldPosition, float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static,
                                        uint collisionGroups = 0,
                                        bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachRectangle(body,
                                    width, height,
                                    density, friction, restitution);

            return body;
        }

        /// <summary>
        /// Creates a rectangle with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="localOffset">The offset of the rectangle relative to the body's local origin.</param>
        /// <param name="localAngle">The rotation of the rectangle relative to the body.</param>
        /// <param name="worldPosition">The initial world position of the body.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="isBullet">if set to <c>true</c> enables continuous
        /// collision with other dynamic bodies.</param>
        /// <param name="allowSleep">if set to <c>true</c> allows the object
        /// to sleep when it is not moving (for improved performance).</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddRectangle(this IManager manager,
                                        float width, float height,
                                        LocalPoint localOffset, float localAngle,
                                        WorldPoint worldPosition, float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static,
                                        uint collisionGroups = 0,
                                        bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       isBullet, allowSleep);
            manager.AttachRectangle(body,
                                    width, height, localOffset, localAngle,
                                    density, friction, restitution);

            return body;
        }

        /// <summary>
        /// Attaches a polygon fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static PolygonFixture AttachPolygon(this IManager manager, Body body,
                                                   IList<LocalPoint> vertices,
                                                   float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var fixture = manager.AddComponent<PolygonFixture>(body.Entity);
            fixture.Initialize(density, friction, restitution);
            fixture.InitializeShape(vertices);

            // Recompute mass data if necessary.
            if (density > 0)
            {
                body.ResetMassData();
            }

            return fixture;
        }

        /// <summary>
        /// Attaches a rectangle fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static PolygonFixture AttachRectangle(this IManager manager, Body body,
                                                     float width, float height,
                                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            // Create the four corner vertices.
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var vertices = new[]
            {
                new LocalPoint(-halfWidth, -halfHeight),
                new LocalPoint(halfWidth, -halfHeight),
                new LocalPoint(halfWidth, halfHeight),
                new LocalPoint(-halfWidth, halfHeight)
            };

            // Then create a normal polygon.
            return manager.AttachPolygon(body, vertices, density, friction, restitution);
        }

        /// <summary>
        /// Attaches a rectangle fixture to the specified entity.
        /// </summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="localPosition">The offset of the rectangle relative to the body's local origin.</param>
        /// <param name="localAngle">The rotation of the rectangle relative to the body.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <returns>
        /// The created fixture.
        /// </returns>
        public static PolygonFixture AttachRectangle(this IManager manager, Body body,
                                                     float width, float height,
                                                     LocalPoint localPosition, float localAngle = 0,
                                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            // Create the four corner vertices.
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var vertices = new[]
            {
                new LocalPoint(-halfWidth, -halfHeight),
                new LocalPoint(halfWidth, - halfHeight),
                new LocalPoint(halfWidth, halfHeight),
                new LocalPoint(-halfWidth, halfHeight)
            };

            // Transform vertices.
            var sin = (float)System.Math.Sin(localAngle);
            var cos = (float)System.Math.Cos(localAngle);
            for (var i = 0; i < vertices.Length; ++i)
            {
                LocalPoint result;
                result.X = (cos * vertices[i].X - sin * vertices[i].Y) + localPosition.X;
                result.Y = (sin * vertices[i].X + cos * vertices[i].Y) + localPosition.Y;
                vertices[i] = result;
            }

            // Then create a normal polygon.
            return manager.AttachPolygon(body, vertices, density, friction, restitution);
        }

        #endregion
    }
}
