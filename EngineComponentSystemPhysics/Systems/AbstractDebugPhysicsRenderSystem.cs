using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Physics.Contacts;
using Engine.ComponentSystem.Physics.Joints;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
using WorldTransform = Engine.FarMath.FarTransform;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldTransform = Microsoft.Xna.Framework.Matrix;
#endif

namespace Engine.ComponentSystem.Physics.Systems
{
    public abstract class AbstractDebugPhysicsRenderSystem : AbstractSystem, IDrawingSystem
    {
        #region Constants

        /// <summary>Scale factor for impulse vectors.</summary>
        private const float ImpulseScale = 0.1f;

        /// <summary>Scale factor for normals.</summary>
        private const float NormalScale = 0.3f;

        /// <summary>Scale factor for transform axes.</summary>
        private const float AxisScale = 0.3f;

        /// <summary>The color to render enabled and awake, dynamic bodies in.</summary>
        private static readonly Color DefaultShapeColor = new Color(0.9f, 0.7f, 0.7f);

        /// <summary>The color to render disabled bodies in.</summary>
        private static readonly Color DisabledShapeColor = new Color(0.5f, 0.5f, 0.3f);

        /// <summary>The color to render enabled, kinematic bodies in.</summary>
        private static readonly Color KinematicShapeColor = new Color(0.5f, 0.5f, 0.9f);

        /// <summary>The color to render sleeping, dynamic bodies in.</summary>
        private static readonly Color SleepingShapeColor = new Color(0.6f, 0.6f, 0.6f);

        /// <summary>The color to render enabled, static bodies in.</summary>
        private static readonly Color StaticShapeColor = new Color(0.5f, 0.9f, 0.5f);

        /// <summary>The color to render contact points in.</summary>
        private static readonly Color ContactColor = Color.Yellow;

        /// <summary>The color to render contact normals in.</summary>
        private static readonly Color ContactNormalColor = new Color(0.4f, 0.9f, 0.4f);

        /// <summary>The color to render contact point impulse vectors in.</summary>
        private static readonly Color ContactNormalImpulseColor = Color.Yellow;

        /// <summary>The color to render bounding boxes in.</summary>
        private static readonly Color FixtureBoundsColor = new Color(0.9f, 0.3f, 0.9f);

        /// <summary>The color to render joint anchor points in.</summary>
        private static readonly Color JointAnchorColor = new Color(0.0f, 1.0f, 0.0f);

        /// <summary>The color to render joint edges/shapes in.</summary>
        private static readonly Color JointEdgeColor = new Color(0.8f, 0.8f, 0.8f);

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets whether to render fixture shapes.</summary>
        public bool RenderFixtures { get; set; }

        /// <summary>Gets or sets whether to render fixture bounding boxes.</summary>
        public bool RenderFixtureBounds { get; set; }

        /// <summary>Gets or sets whether to render the center of mass of bodies.</summary>
        public bool RenderCenterOfMass { get; set; }

        /// <summary>Gets or sets whether to render contact points.</summary>
        public bool RenderContactPoints { get; set; }

        /// <summary>Gets or sets whether to render contact normals.</summary>
        public bool RenderContactNormals { get; set; }

        /// <summary>Gets or sets whether to render contact points' normal impulse.</summary>
        public bool RenderContactPointNormalImpulse { get; set; }

        /// <summary>Gets or sets whether to render joint edges.</summary>
        public bool RenderJoints { get; set; }

        #endregion

        #region Fields

        /// <summary>Keep a reference to the graphics device for projecting/unprojecting.</summary>
        private GraphicsDevice _graphicsDevice;

        /// <summary>The primitive batch we use to render shapes.</summary>
        private PrimitiveBatch _primitiveBatch;

        #endregion

        #region Implementation

        /// <summary>Gets the display scaling factor (camera zoom).</summary>
        protected abstract float GetScale();

        /// <summary>Gets the display transform (camera position/rotation).</summary>
        protected abstract WorldTransform GetTransform();

        /// <summary>Gets the visible bodies.</summary>
        protected virtual IEnumerable<Tuple<Body, WorldPoint, float>> GetVisibleBodies()
        {
            return ((PhysicsSystem) Manager.GetSystem(PhysicsSystem.TypeId)).Bodies
                .Select(body => Tuple.Create(body, body.Position, body.Angle));
        }

        /// <summary>Gets the visible contacts.</summary>
        protected virtual IEnumerable<Contact> GetVisibleContacts()
        {
            return ((PhysicsSystem) Manager.GetSystem(PhysicsSystem.TypeId)).Contacts;
        }

        /// <summary>Gets the visible joints.</summary>
        protected virtual IEnumerable<Joint> GetVisibleJoints()
        {
            return ((PhysicsSystem) Manager.GetSystem(PhysicsSystem.TypeId)).Joints;
        }

        #endregion

        #region Accessors

        /// <summary>Converts a coordinate in simulation space to screen space.</summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The unprojected point.</returns>
        public WorldPoint ScreenToSimulation(Vector2 point)
        {
            var viewport = _graphicsDevice.Viewport;
            var projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, 0, viewport.Height, 0, 1);
            var view = ComputeViewTransform();
#if FARMATH
            var unprojected = _graphicsDevice.Viewport.Unproject(new Vector3(point, 0), projection, view.Matrix, Matrix.Identity);
            return XnaUnitConversion.ToSimulationUnits(new Vector2(unprojected.X, unprojected.Y)) - view.Translation;
#else
            var unprojected = _graphicsDevice.Viewport.Unproject(
                new Vector3(point, 0), projection, view, Matrix.Identity);
            return XnaUnitConversion.ToSimulationUnits(new Vector2(unprojected.X, unprojected.Y));
#endif
        }

        /// <summary>Converts a coordinate in screen space to simulation space.</summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The projected point.</returns>
        public Vector2 SimulationToScreen(WorldPoint point)
        {
            var viewport = _graphicsDevice.Viewport;
            var projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, 0, viewport.Height, 0, 1);
            var view = ComputeViewTransform();
#if FARMATH
            var projected = _graphicsDevice.Viewport.Project(new Vector3(XnaUnitConversion.ToScreenUnits((Vector2)(point + view.Translation)), 0), projection, view.Matrix, Matrix.Identity);
#else
            var projected = _graphicsDevice.Viewport.Project(
                new Vector3(XnaUnitConversion.ToScreenUnits(point), 0), projection, view, Matrix.Identity);
#endif
            return new Vector2(projected.X, projected.Y);
        }

        /// <summary>Computes the view matrix.</summary>
        /// <returns></returns>
        private WorldTransform ComputeViewTransform()
        {
            var scale = GetScale();
            var view = GetTransform();
            var world = Matrix.CreateTranslation(
                _graphicsDevice.Viewport.Width / 2f,
                _graphicsDevice.Viewport.Height / 2f,
                0);
#if FARMATH
            view.Translation /= 100f;
            view.Matrix = view.Matrix * Matrix.CreateScale(scale) * world;
            return view;
#else
            return view * Matrix.CreateScale(scale) * world;
#endif
        }

        #endregion

        #region Rendering

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Skip if we have nothing to draw.
            if (
                !(RenderFixtureBounds || RenderFixtures || RenderCenterOfMass || RenderContactPoints ||
                  RenderContactNormals || RenderContactPointNormalImpulse))
            {
                return;
            }

            // Get physics system from which to get bodies and contacts.
            var physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
            if (physics == null)
            {
                return;
            }

            // Compute view matrix.
            var view = ComputeViewTransform();

            // Render fixture bounds.
            if (RenderFixtureBounds)
            {
#if FARMATH
                _primitiveBatch.Begin(view.Matrix);
#else
                _primitiveBatch.Begin(view);
#endif
                foreach (var aabb in physics.FixtureBounds)
                {
#if FARMATH
                    var center = (Vector2)(aabb.Center + view.Translation);
#else
                    var center = aabb.Center;
#endif
                    _primitiveBatch.DrawRectangle(
                        XnaUnitConversion.ToScreenUnits(center),
                        UnitConversion.ToScreenUnits(aabb.Width),
                        UnitConversion.ToScreenUnits(aabb.Height),
                        FixtureBoundsColor);
                }
                _primitiveBatch.End();
            }

            // Render fixtures.
            if (RenderFixtures || RenderCenterOfMass)
            {
                foreach (var bodyInfo in GetVisibleBodies())
                {
                    // Get model transform based on body transform.
                    var body = bodyInfo.Item1;
                    var bodyPosition = bodyInfo.Item2;
                    var bodyAngle = bodyInfo.Item3;
#if FARMATH
                    var bodyTranslation = (Vector2)(bodyPosition + view.Translation);
                    var model = Matrix.CreateRotationZ(bodyAngle) *
                                Matrix.CreateTranslation(UnitConversion.ToScreenUnits(bodyTranslation.X),
                                                         UnitConversion.ToScreenUnits(bodyTranslation.Y), 0);
                    _primitiveBatch.Begin(model * view.Matrix);
                    DrawBody(body, XnaUnitConversion.ToScreenUnits);
#else
                    var model = Matrix.CreateRotationZ(body.Sweep.Angle) *
                                Matrix.CreateTranslation(
                                    UnitConversion.ToScreenUnits(bodyPosition.X),
                                    UnitConversion.ToScreenUnits(bodyPosition.Y),
                                    0);
                    _primitiveBatch.Begin(model * view);

                    DrawBody(body, XnaUnitConversion.ToScreenUnits);
#endif

                    _primitiveBatch.End();
                }
            }

            // Render contacts.
            if (RenderContactPoints || RenderContactNormals || RenderContactPointNormalImpulse)
            {
#if FARMATH
                _primitiveBatch.Begin(view.Matrix);
#else
                _primitiveBatch.Begin(view);
#endif
                foreach (var contact in GetVisibleContacts())
                {
#if FARMATH
                    DrawContact(contact, v => XnaUnitConversion.ToScreenUnits((Vector2)(v + view.Translation)));
#else
                    DrawContact(contact, XnaUnitConversion.ToScreenUnits);
#endif
                }
                _primitiveBatch.End();
            }

            // Render joints.
            if (RenderJoints)
            {
#if FARMATH
                _primitiveBatch.Begin(view.Matrix);
#else
                _primitiveBatch.Begin(view);
#endif

                foreach (var joint in GetVisibleJoints())
                {
#if FARMATH
                    DrawJoint(joint, v => XnaUnitConversion.ToScreenUnits((Vector2)(v + view.Translation)));
#else
                    DrawJoint(joint, XnaUnitConversion.ToScreenUnits);
#endif
                }

                _primitiveBatch.End();
            }
        }

        private void DrawBody(Body body, Func<Vector2, Vector2> toScreen)
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
                foreach (Fixture fixture in body.Fixtures)
                {
                    switch (fixture.Type)
                    {
                        case Fixture.FixtureType.Circle:
                        {
                            var circle = fixture as CircleFixture;
                            System.Diagnostics.Debug.Assert(circle != null);
                            _primitiveBatch.DrawSolidCircle(
                                toScreen(circle.Center),
                                UnitConversion.ToScreenUnits(circle.Radius),
                                color);
                        }
                            break;
                        case Fixture.FixtureType.Edge:
                        {
                            var edge = fixture as EdgeFixture;
                            System.Diagnostics.Debug.Assert(edge != null);
                            _primitiveBatch.DrawLine(
                                toScreen(edge.Vertex1),
                                toScreen(edge.Vertex2),
                                color);
                        }
                            break;
                        case Fixture.FixtureType.Polygon:
                        {
                            var polygon = fixture as PolygonFixture;
                            System.Diagnostics.Debug.Assert(polygon != null);
                            _primitiveBatch.DrawFilledPolygon(
                                polygon.Vertices
                                       .Take(polygon.Count)
                                       .Select(toScreen).ToList(),
                                color);
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
                var p1 = toScreen(body.Sweep.LocalCenter);
                // We want to use the normal (untransformed in FarValue case) method
                // for mapping to screen space when computing our axis length.
                _primitiveBatch.DrawLine(p1, p1 + XnaUnitConversion.ToScreenUnits(Vector2.UnitX * AxisScale), Color.Red);
                _primitiveBatch.DrawLine(p1, p1 + XnaUnitConversion.ToScreenUnits(Vector2.UnitY * AxisScale), Color.Blue);
            }
        }

        private void DrawContact(Contact contact, Func<WorldPoint, Vector2> toScreen)
        {
            Vector2 normal;
            IList<WorldPoint> points;
            contact.ComputeWorldManifold(out normal, out points);

            for (var i = 0; i < points.Count; ++i)
            {
                var point = toScreen(points[i]);

                if (RenderContactPoints)
                {
                    _primitiveBatch.DrawFilledRectangle(
                        point, UnitConversion.ToScreenUnits(0.1f), UnitConversion.ToScreenUnits(0.1f), ContactColor);
                }

                if (RenderContactNormals)
                {
                    // We want to use the normal (untransformed in FarValue case) method
                    // for mapping to screen space when computing our axis length.
                    _primitiveBatch.DrawLine(
                        point, point + XnaUnitConversion.ToScreenUnits(normal * NormalScale), ContactNormalColor);
                }

                if (RenderContactPointNormalImpulse)
                {
                    // We want to use the normal (untransformed in FarValue case) method
                    // for mapping to screen space when computing our axis length.
                    _primitiveBatch.DrawLine(
                        point,
                        point + XnaUnitConversion.ToScreenUnits(normal * contact.GetNormalImpulse(i) * ImpulseScale),
                        ContactNormalImpulseColor);
                }
            }
        }

        private void DrawJoint(Joint joint, Func<WorldPoint, Vector2> toScreen)
        {
            var anchorA = toScreen(joint.AnchorA);
            var anchorB = toScreen(joint.AnchorB);

            _primitiveBatch.DrawFilledRectangle(
                anchorA, UnitConversion.ToScreenUnits(0.1f), UnitConversion.ToScreenUnits(0.1f), JointAnchorColor);
            _primitiveBatch.DrawFilledRectangle(
                anchorB, UnitConversion.ToScreenUnits(0.1f), UnitConversion.ToScreenUnits(0.1f), JointAnchorColor);

            switch (joint.Type)
            {
                case Joint.JointType.Mouse:
                case Joint.JointType.Distance:
                    // For mouse and distance joints we just want the line between the two anchors.
                    _primitiveBatch.DrawLine(anchorA, anchorB, JointEdgeColor);
                    break;
                case Joint.JointType.Pulley:
                {
                    // For pulleys we draw the two lines connecting the bodies to their world points.
                    var pulleyJoint = (PulleyJoint) joint;
                    var anchorA0 = toScreen(pulleyJoint.GroundAnchorA);
                    var anchorB0 = toScreen(pulleyJoint.GroundAnchorB);
                    _primitiveBatch.DrawFilledRectangle(
                        anchorA0, UnitConversion.ToScreenUnits(0.1f), UnitConversion.ToScreenUnits(0.1f), JointAnchorColor);
                    _primitiveBatch.DrawFilledRectangle(
                        anchorB0, UnitConversion.ToScreenUnits(0.1f), UnitConversion.ToScreenUnits(0.1f), JointAnchorColor);

                    _primitiveBatch.DrawLine(anchorA0, anchorA, JointEdgeColor);
                    _primitiveBatch.DrawLine(anchorB0, anchorB, JointEdgeColor);
                    break;
                }
                case Joint.JointType.Prismatic:
                {
                    // Don't draw attachment of prismatic joints if they attach to the fix point.
                    var physicsSystem = (PhysicsSystem) Manager.GetSystem(PhysicsSystem.TypeId);
                    if (joint.BodyA != physicsSystem.FixPoint)
                    {
                        _primitiveBatch.DrawLine(toScreen(joint.BodyA.Position), anchorA, JointEdgeColor);
                    }
                    _primitiveBatch.DrawLine(anchorA, anchorB, JointEdgeColor);
                    if (joint.BodyB != physicsSystem.FixPoint)
                    {
                        _primitiveBatch.DrawLine(anchorB, toScreen(joint.BodyB.Position), JointEdgeColor);
                    }
                    break;
                }
                case Joint.JointType.Revolute:
                {
                    // Don't draw attachment of revolute joints if they attach to the fix point.
                    var physicsSystem = (PhysicsSystem) Manager.GetSystem(PhysicsSystem.TypeId);
                    if (joint.BodyA != physicsSystem.FixPoint)
                    {
                        _primitiveBatch.DrawLine(toScreen(joint.BodyA.Position), anchorA, JointEdgeColor);
                    }
                    _primitiveBatch.DrawLine(anchorA, anchorB, JointEdgeColor);
                    if (joint.BodyB != physicsSystem.FixPoint)
                    {
                        _primitiveBatch.DrawLine(anchorB, toScreen(joint.BodyB.Position), JointEdgeColor);
                    }
                    break;
                }
                default:
                {
                    // Per default just draw three lines from body to joint anchor to
                    // joint anchor to body.
                    if (joint.BodyA != null)
                    {
                        _primitiveBatch.DrawLine(toScreen(joint.BodyA.Position), anchorA, JointEdgeColor);
                    }
                    _primitiveBatch.DrawLine(anchorA, anchorB, JointEdgeColor);
                    if (joint.BodyB != null)
                    {
                        _primitiveBatch.DrawLine(anchorB, toScreen(joint.BodyB.Position), JointEdgeColor);
                    }
                    break;
                }
            }
        }

        #endregion

        #region Logic

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<GraphicsDeviceCreated>(OnGraphicsDeviceCreated);
            Manager.AddMessageListener<GraphicsDeviceDisposing>(OnGraphicsDeviceDisposing);
        }

        private void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _graphicsDevice = message.Graphics.GraphicsDevice;
            _primitiveBatch = new PrimitiveBatch(_graphicsDevice);
        }

        private void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            _graphicsDevice = null;
            if (_primitiveBatch != null)
            {
                _primitiveBatch.Dispose();
                _primitiveBatch = null;
            }
        }

        #endregion
    }
}