using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Physics.Components;
using FarseerPhysics.DebugViews;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Physics.Systems
{
    public abstract class AbstractDebugPhysicsRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Constants

        /// <summary>
        /// Scale factor for impulse vectors.
        /// </summary>
        private const float ImpulseScale = 0.1f;

        /// <summary>
        /// Scale factor for normals.
        /// </summary>
        private const float NormalScale = 0.3f;

        /// <summary>
        /// Scale factor for transform axes.
        /// </summary>
        private const float AxisScale = 0.3f;

        /// <summary>
        /// The color to render enabled and awake, dynamic bodies in.
        /// </summary>
        private static readonly Color DefaultShapeColor = new Color(0.9f, 0.7f, 0.7f);

        /// <summary>
        /// The color to render disabled bodies in.
        /// </summary>
        private static readonly Color DisabledShapeColor = new Color(0.5f, 0.5f, 0.3f);

        /// <summary>
        /// The color to render enabled, kinematic bodies in.
        /// </summary>
        private static readonly Color KinematicShapeColor = new Color(0.5f, 0.5f, 0.9f);

        /// <summary>
        /// The color to render sleeping, dynamic bodies in.
        /// </summary>
        private static readonly Color SleepingShapeColor = new Color(0.6f, 0.6f, 0.6f);

        /// <summary>
        /// The color to render enabled, static bodies in.
        /// </summary>
        private static readonly Color StaticShapeColor = new Color(0.5f, 0.9f, 0.5f);

        /// <summary>
        /// The color to render contact points in.
        /// </summary>
        private static readonly Color ContactColor = Color.Yellow;

        /// <summary>
        /// The color to render contact normals in.
        /// </summary>
        private static readonly Color ContactNormalColor = new Color(0.4f, 0.9f, 0.4f);

        /// <summary>
        /// The color to render contact point imulse vectors in.
        /// </summary>
        private static readonly Color ContactNormalImpulseColor = Color.Yellow;

        /// <summary>
        /// The color to render bounding boxes in.
        /// </summary>
        private static readonly Color FixtureBoundsColor = new Color(0.9f, 0.3f, 0.9f);

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether to render fixture shapes.
        /// </summary>
        public bool RenderFixtures { get; set; }

        /// <summary>
        /// Gets or sets whether to render fixture bounding boxes.
        /// </summary>
        public bool RenderFixtureBounds { get; set; }

        /// <summary>
        /// Gets or sets whether to render the center of mass of bodies.
        /// </summary>
        public bool RenderCenterOfMass { get; set; }

        /// <summary>
        /// Gets or sets whether to render contact points.
        /// </summary>
        public bool RenderContactPoints { get; set; }

        /// <summary>
        /// Gets or sets whether to render contact normals.
        /// </summary>
        public bool RenderContactNormals { get; set; }

        /// <summary>
        /// Gets or sets whether to render contact points' normal impulse.
        /// </summary>
        public bool RenderContactPointNormalImpulse { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Keep a reference to the graphics device for projecting/unprojecting.
        /// </summary>
        private GraphicsDevice _graphics;

        /// <summary>
        /// The primitive batch we use to render shapes.
        /// </summary>
        private PrimitiveBatch _primitiveBatch;

        #endregion

        #region Implementation

        /// <summary>
        /// Gets the display scaling factor (camera zoom).
        /// </summary>
        protected abstract float GetScale();

        /// <summary>
        /// Gets the display transform (camera position/rotation).
        /// </summary>
        protected abstract Matrix GetTransform();

        #endregion

        #region Accessors

        /// <summary>
        /// Converts a coordinate in simulation space to screen space.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The unprojected point.</returns>
        public Vector2 ScreenToSimulation(Vector2 point)
        {
            var unprojected = _graphics.Viewport.Unproject(new Vector3(point, 0), _primitiveBatch.Projection, ComputeViewMatrix(), Matrix.Identity);
            return new Vector2(unprojected.X, unprojected.Y);
        }

        /// <summary>
        /// Converts a coordinate in screen space to simulation space.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The projected point.</returns>
        public Vector2 SimulationToScreen(Vector2 point)
        {
            var projected = _graphics.Viewport.Project(new Vector3(point, 0), _primitiveBatch.Projection, ComputeViewMatrix(), Matrix.Identity);
            return new Vector2(projected.X, projected.Y);
        }

        /// <summary>
        /// Computes the view matrix.
        /// </summary>
        /// <returns></returns>
        private Matrix ComputeViewMatrix()
        {
            var scale = GetScale();
            var viewport = new Vector2(_graphics.Viewport.Width, _graphics.Viewport.Height);
            viewport = PhysicsSystem.ToSimulationUnits(viewport);
            return GetTransform() *
                Matrix.CreateTranslation(viewport.X / (scale * 4f), viewport.Y / (scale * 4f), 0) *
                Matrix.CreateScale(scale);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Skip if we have nothing to draw.
            if (!(RenderFixtureBounds || RenderFixtures || RenderCenterOfMass || RenderContactPoints || RenderContactNormals || RenderContactPointNormalImpulse))
            {
                return;
            }

            // Get phyisics system from which to get bodies and contacts.
            var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
            if (physics == null)
            {
                return;
            }

            // Compute view matrix.
            var view = ComputeViewMatrix();

            // Render fixture bounds.
            if (RenderFixtureBounds)
            {
                var vertices = new Vector2[4];
                _primitiveBatch.Begin(ref view);
                foreach (var aabb in physics.FixtureBounds)
                {
                    vertices[0].X = aabb.Left;
                    vertices[0].Y = aabb.Top;
                    vertices[1].X = aabb.Right;
                    vertices[1].Y = aabb.Top;
                    vertices[2].X = aabb.Right;
                    vertices[2].Y = aabb.Bottom;
                    vertices[3].X = aabb.Left;
                    vertices[3].Y = aabb.Bottom;
                    _primitiveBatch.DrawPolygon(vertices, 4, FixtureBoundsColor);
                }
                _primitiveBatch.End();
            }

            // Render fixtures.
            if (RenderFixtures || RenderCenterOfMass)
            {
                foreach (var body in physics.Bodies)
                {
                    // Get model transform based on body transform.
                    var model = Matrix.CreateRotationZ(body.Sweep.Angle) *
                                Matrix.CreateTranslation(body.Transform.Translation.X, body.Transform.Translation.Y, 0);

                    var modelView = model * view;
                    _primitiveBatch.Begin(ref modelView);

                    DrawBody(body);

                    _primitiveBatch.End();
                }
            }

            // Render contacts.
            if (RenderContactPoints || RenderContactNormals || RenderContactPointNormalImpulse)
            {
                _primitiveBatch.Begin(ref view);
                foreach (var contact in physics.Contacts)
                {
                    DrawContact(contact);
                }
                _primitiveBatch.End();
            }
        }

        private void DrawBody(Body body)
        {
            // Draw the fixtures attached to this body.
            if (RenderFixtures)
            {
                // Get color to draw primitives in based on body state.
                Color color;
                if (!body.Enabled)
                {
                    color = DisabledShapeColor;
                }
                else if (body.Type == Body.BodyType.Static)
                {
                    color = StaticShapeColor;
                }
                else if (body.Type == Body.BodyType.Kinematic)
                {
                    color = KinematicShapeColor;
                }
                else if (!body.IsAwake)
                {
                    color = SleepingShapeColor;
                }
                else
                {
                    color = DefaultShapeColor;
                }

                // Get all fixtures attached to this body.
                foreach (Fixture component in Manager.GetComponents(body.Entity, Fixture.TypeId))
                {
                    switch (component.Type)
                    {
                        case Fixture.FixtureType.Circle:
                        {
                            var circle = component as CircleFixture;
                            System.Diagnostics.Debug.Assert(circle != null);
                            _primitiveBatch.DrawSolidCircle(circle.Center, circle.Radius, Vector2.UnitX, color);
                        }
                            break;
                        case Fixture.FixtureType.Edge:
                        {
                            var edge = component as EdgeFixture;
                            System.Diagnostics.Debug.Assert(edge != null);
                            _primitiveBatch.DrawSegment(edge.Vertex1, edge.Vertex2, color);
                        }
                            break;
                        case Fixture.FixtureType.Polygon:
                        {
                            var polygon = component as PolygonFixture;
                            System.Diagnostics.Debug.Assert(polygon != null);
                            _primitiveBatch.DrawSolidPolygon(polygon.Vertices, polygon.Count, color);
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            // Draw the transform at the center of mass.
            if (RenderCenterOfMass)
            {
                var p1 = body.Sweep.LocalCenter;
                _primitiveBatch.DrawSegment(p1, p1 + Vector2.UnitX * AxisScale, Color.Red);
                _primitiveBatch.DrawSegment(p1, p1 + Vector2.UnitY * AxisScale, Color.Green);
            }
        }

        private void DrawContact(PhysicsSystem.IContact contact)
        {
            Vector2 normal;
            IList<Vector2> points;
            contact.ComputeWorldManifold(out normal, out points);

            for (var i = 0; i < points.Count; ++i)
            {
                var point = points[i];

                if (RenderContactPoints)
                {
                    _primitiveBatch.DrawPoint(point, 0.1f, ContactColor);
                }

                if (RenderContactNormals)
                {
                    _primitiveBatch.DrawSegment(point, point + NormalScale * normal, ContactNormalColor);
                }

                if (RenderContactPointNormalImpulse)
                {
                    _primitiveBatch.DrawSegment(point, point + ImpulseScale * normal * contact.GetNormalImpulse(i),
                                                ContactNormalImpulseColor);
                }
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    _graphics = cm.Value.Graphics.GraphicsDevice;
                    _primitiveBatch = new PrimitiveBatch(_graphics);
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    _graphics = null;
                    if (_primitiveBatch != null)
                    {
                        _primitiveBatch.Dispose();
                        _primitiveBatch = null;
                    }
                }
            }
        }

        #endregion
    }
}
