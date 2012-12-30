﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.Physics.Detail;

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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                   bool fixedRotation = false, bool isBullet = false,
                                   bool allowSleep = true)
        {
            return manager.AddComponent<Body>(manager.AddEntity())
                .Initialize(type)
                .Initialize(collisionGroups)
                .Initialize(fixedRotation, isBullet, allowSleep)
                .Initialize(worldPosition, worldAngle);
        }

        /// <summary>
        /// Creates the a new body with the specified properties.
        /// </summary>
        /// <param name="manager">The manager to create the body in.</param>
        /// <param name="angle">The initial world angle of the body.</param>
        /// <param name="type">The type of the body.</param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                   bool fixedRotation = false, bool isBullet = false, bool allowSleep = true)
        {
            return manager.AddBody(WorldPoint.Zero, angle,
                                   type, collisionGroups,
                                   fixedRotation, isBullet, allowSleep);
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                     bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       fixedRotation, isBullet, allowSleep);
            manager.AttachCircle(body, localPosition, radius,
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                     bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius,
                                     worldPosition, worldAngle,
                                     type, collisionGroups,
                                     fixedRotation, isBullet, allowSleep,
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                     bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(localPosition, radius,
                                     WorldPoint.Zero, worldAngle,
                                     type, collisionGroups,
                                     fixedRotation, isBullet, allowSleep,
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                     bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                     float density = 0, float friction = 0.2f, float restitution = 0)
        {
            return manager.AddCircle(LocalPoint.Zero, radius,
                                     WorldPoint.Zero, worldAngle,
                                     type, collisionGroups,
                                     fixedRotation, isBullet, allowSleep,
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
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

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
            return manager.AttachCircle(body, LocalPoint.Zero, radius, density, friction, restitution);
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint localStart, LocalPoint localEnd,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, collisionGroups: collisionGroups);
            manager.AttachEdge(body, localStart, localEnd, friction, restitution);

            return body;
        }

        /// <summary>
        /// Creates an edge with the specified parameters.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="worldAngle">The initial world angle of the body.</param>
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint localStart, LocalPoint localEnd,
                                   float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            return manager.AddEdge(localStart, localEnd,
                                   WorldPoint.Zero, worldAngle,
                                   collisionGroups,
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint ghostStart, LocalPoint localStart, LocalPoint localEnd,
                                   LocalPoint ghostEnd,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle, collisionGroups: collisionGroups);
            manager.AttachEdge(body, ghostStart, localStart, localEnd, ghostEnd,
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddEdge(this IManager manager,
                                   LocalPoint ghostStart, LocalPoint localStart,
                                   LocalPoint localEnd, LocalPoint ghostEnd,
                                   float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            return manager.AddEdge(ghostStart, localStart, localEnd, ghostEnd,
                                   WorldPoint.Zero, worldAngle,
                                   collisionGroups,
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
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

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
                                             LocalPoint ghostStart, LocalPoint localStart,
                                             LocalPoint localEnd, LocalPoint ghostEnd,
                                             float friction = 0.2f, float restitution = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var fixture = manager.AddComponent<EdgeFixture>(body.Entity);
            fixture.Initialize(friction: friction, restitution: restitution);
            fixture.InitializeShape(ghostStart, localStart, localEnd, ghostEnd);

            return fixture;
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddLoop(this IManager manager,
                                   IList<LocalPoint> vertices,
                                   WorldPoint worldPosition, float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            ValidateVertices(vertices, 3);

            var body = manager.AddBody(worldPosition, worldAngle, collisionGroups: collisionGroups);
            manager.AttachLoopUnchecked(body, vertices, friction, restitution);

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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// The created body.
        /// </returns>
        public static Body AddLoop(this IManager manager,
                                   IList<LocalPoint> vertices,
                                   float worldAngle = 0,
                                   uint collisionGroups = 0,
                                   float friction = 0.2f, float restitution = 0)
        {
            ValidateVertices(vertices, 3);

            var body = manager.AddBody(WorldPoint.Zero, worldAngle, collisionGroups: collisionGroups);
            manager.AttachLoopUnchecked(body, vertices, friction, restitution);

            return body;
        }

        /// <summary>
        /// Creates an edge loop with the specified parameters. A loop consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="body">The body to attach the fixtures to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        public static void AttachLoop(this IManager manager, Body body,
                                      IList<LocalPoint> vertices,
                                      float friction = 0.2f, float restitution = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            ValidateVertices(vertices, 3);

            manager.AttachLoopUnchecked(body, vertices, friction, restitution);
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// A proxy object that may be used to interact with the created chain.
        /// </returns>
        public static Chain AddChain(this IManager manager,
                                     IList<LocalPoint> vertices,
                                     WorldPoint worldPosition, float worldAngle = 0,
                                     uint collisionGroups = 0,
                                     float friction = 0.2f, float restitution = 0)
        {
            ValidateVertices(vertices, 2);

            var body = manager.AddBody(worldPosition, worldAngle, collisionGroups: collisionGroups);
            return manager.AttachChainUnchecked(body, vertices, friction, restitution);
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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// A proxy object that may be used to interact with the created chain.
        /// </returns>
        public static Chain AddChain(this IManager manager,
                                     IList<LocalPoint> vertices,
                                     float worldAngle = 0,
                                     uint collisionGroups = 0,
                                     float friction = 0.2f, float restitution = 0)
        {
            ValidateVertices(vertices, 2);

            var body = manager.AddBody(WorldPoint.Zero, worldAngle, collisionGroups: collisionGroups);
            return manager.AttachChainUnchecked(body, vertices, friction, restitution);
        }

        /// <summary>
        /// Creates an edge chain with the specified parameters. A chain consists in fact
        /// of multiple edge fixtures with neighborhood information, which avoid collisions
        /// with inner vertices. Note that is not actually Box2D's implementation of a chain,
        /// but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="body">The body to attach the fixtures to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <returns>
        /// A proxy object that may be used to interact with the created chain.
        /// </returns>
        public static Chain AttachChain(this IManager manager,
                                        Body body,
                                        IList<LocalPoint> vertices,
                                        float friction = 0.2f, float restitution = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            ValidateVertices(vertices, 2);

            return manager.AttachChainUnchecked(body, vertices, friction, restitution);
        }

        /// <summary>
        /// Validates vertex input.
        /// </summary>
        private static void ValidateVertices(IList<LocalPoint> vertices, int minCount)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException("vertices");
            }
            if (vertices.Count < minCount)
            {
                throw new ArgumentException("Too few vertices to create a chain.", "vertices");
            }
            for (var i = 1; i < vertices.Count; ++i)
            {
                var v1 = vertices[i - 1];
                var v2 = vertices[i];
                if (LocalPoint.DistanceSquared(v1, v2) <= Settings.LinearSlop * Settings.LinearSlop)
                {
                    throw new ArgumentException("Some vertices are too close together.", "vertices");
                }
            }
        }

        /// <summary>
        /// Unchecked attach logic to avoid duplicate input validation.
        /// </summary>
        public static void AttachLoopUnchecked(this IManager manager, Body body,
                                               IList<LocalPoint> vertices,
                                               float friction = 0.2f, float restitution = 0)
        {
            for (var i = 0; i < vertices.Count; ++i)
            {
                var ghostStart = vertices[(i - 1 + vertices.Count) % vertices.Count];
                var start = vertices[i];
                var end = vertices[(i + 1) % vertices.Count];
                var ghostEnd = vertices[(i + 2) % vertices.Count];
                manager.AttachEdge(body, ghostStart, start, end, ghostEnd, friction, restitution);
            }
        }

        /// <summary>
        /// Unchecked attach logic to avoid duplicate input validation.
        /// </summary>
        private static Chain AttachChainUnchecked(this IManager manager,
                                                  Body body,
                                                  IList<LocalPoint> vertices,
                                                  float friction = 0.2f, float restitution = 0)
        {
            // See if it's just a single edge.
            if (vertices.Count == 2)
            {
                var edge = manager.AttachEdge(body, vertices[0], vertices[1],
                                              friction, restitution);
                return new Chain(body, edge, edge);
            }

            var head = manager.AttachEdge(body, vertices[0], vertices[1],
                                          friction, restitution);
            head.Vertex3 = vertices[2];
            head.HasVertex3 = true;

            for (var i = 1; i < vertices.Count - 2; ++i)
            {
                manager.AttachEdge(body, vertices[i - 1], vertices[i],
                                   vertices[i + 1], vertices[i + 2],
                                   friction, restitution);
            }

            var tail = manager.AttachEdge(body, vertices[vertices.Count - 2], vertices[vertices.Count - 1],
                                          friction, restitution);
            tail.Vertex0 = vertices[vertices.Count - 3];
            tail.HasVertex0 = true;

            return new Chain(body, head, tail);
        }

        /// <summary>
        /// This a helper construct to allow dynamic modification of pseudo-chains
        /// at some point in time after their creation. This is necessary because
        /// we do not actually implement Box2D's chain shape but generate actual
        /// edges for each chain link, instead.
        /// </summary>
        public class Chain
        {
            /// <summary>
            /// Gets the body that this chain is attached to.
            /// </summary>
            public Body Body
            {
                get { return _body; }
            }

            /// <summary>
            /// Establish connectivity to a vertex that precedes the first vertex.
            /// This may be used to connect an edge or other chain shape to this one.
            /// </summary>
            public LocalPoint? PreviousVertex
            {
                get { return _head.HasVertex0 ? (LocalPoint?)_head.Vertex0 : null; }
                set
                {
                    if (value != null)
                    {
                        _head.Vertex0 = value.Value;
                        _head.HasVertex0 = true;
                    }
                    else
                    {
                        _head.HasVertex0 = false;
                    }
                }
            }

            /// <summary>
            /// Establish connectivity to a vertex that follows the last vertex.
            /// This may be used to connect an edge or other chain shape to this one.
            /// </summary>
            public LocalPoint? NextVertex
            {
                get { return _tail.HasVertex3 ? (LocalPoint?)_tail.Vertex3 : null; }
                set
                {
                    if (value != null)
                    {
                        _tail.Vertex3 = value.Value;
                        _tail.HasVertex3 = true;
                    }
                    else
                    {
                        _tail.HasVertex3 = false;
                    }
                }
            }

            /// <summary>
            /// The body this chain is attached to.
            /// </summary>
            private readonly Body _body;
            
            /// <summary>
            /// The head and tail edge fixtures of this chain.
            /// </summary>
            private readonly EdgeFixture _head, _tail;

            /// <summary>
            /// Initializes a new instance of the <see cref="Chain"/> class.
            /// </summary>
            /// <param name="body">The body the chain is attached to.</param>
            /// <param name="head">The head link of the chain.</param>
            /// <param name="tail">The tail link of the chain.</param>
            internal Chain(Body body, EdgeFixture head, EdgeFixture tail)
            {
                System.Diagnostics.Debug.Assert(body != null && head != null && tail != null);
                System.Diagnostics.Debug.Assert(!head.HasVertex0);
                System.Diagnostics.Debug.Assert(!tail.HasVertex3);

                _body = body;
                _head = head;
                _tail = tail;
            }
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                      bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                      float density = 0, float friction = 0.2f, float restitution = 0)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException("vertices");
            }

            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       fixedRotation, isBullet, allowSleep);
            manager.AttachPolygon(body, vertices, density, friction, restitution);

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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                        LocalPoint localPosition, float localAngle,
                                        WorldPoint worldPosition, float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static,
                                        uint collisionGroups = 0,
                                        bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height, localPosition, localAngle,
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
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                        bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(worldPosition, worldAngle,
                                       type, collisionGroups,
                                       fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height,
                                    density, friction, restitution);

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
        /// <param name="collisionGroups">The collision groups of the fixture.</param>
        /// <param name="fixedRotation">if set to <c>true</c> the rotation of
        /// the body is fixed to its initial value.</param>
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
                                        LocalPoint localPosition, float localAngle = 0,
                                        float worldAngle = 0,
                                        Body.BodyType type = Body.BodyType.Static,
                                        uint collisionGroups = 0,
                                        bool fixedRotation = false, bool isBullet = false, bool allowSleep = true,
                                        float density = 0, float friction = 0.2f, float restitution = 0)
        {
            var body = manager.AddBody(WorldPoint.Zero, worldAngle,
                                       type, collisionGroups,
                                       fixedRotation, isBullet, allowSleep);
            manager.AttachRectangle(body, width, height, localPosition, localAngle,
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
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            if (vertices == null)
            {
                throw new ArgumentNullException("vertices");
            }

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
