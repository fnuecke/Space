using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Physics.Components;
using FarseerPhysics.DebugViews;
using Microsoft.Xna.Framework;

namespace Engine.Physics.Systems
{
    public sealed class DebugPhysicsRenderSystem : AbstractComponentSystem<Body>, IDrawingSystem, IMessagingSystem
    {
        private static readonly Color DefaultShapeColor = new Color(0.9f, 0.7f, 0.7f);
        private static readonly Color InactiveShapeColor = new Color(0.5f, 0.5f, 0.3f);
        private static readonly Color KinematicShapeColor = new Color(0.5f, 0.5f, 0.9f);
        private static readonly Color SleepingShapeColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color StaticShapeColor = new Color(0.5f, 0.9f, 0.5f);

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        public float Scale { get; set; }

        public Vector2 Offset { get; set; }

        private PrimitiveBatch _primitiveBatch;

        public DebugPhysicsRenderSystem()
        {
            Scale = 8;
            Offset = new Vector2(0, 10);
        }

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            foreach (var body in Components)
            {
                // Get view transform based on body transform.
                var view = Matrix.CreateRotationZ(body.Sweep.Angle) *
                    Matrix.CreateTranslation(body.Transform.Translation.X, body.Transform.Translation.Y, 0) *
                    Matrix.CreateTranslation(Offset.X, -Offset.Y, 0) *
                    Matrix.CreateScale(Scale);
                _primitiveBatch.Begin(ref view);

                // Get color to draw primitives in based on body state.
                Color color;
                if (!body.Enabled)
                {
                    color = InactiveShapeColor;
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
                } else
                {
                    color = DefaultShapeColor;
                }

                // Draw the fixtures attached to this body.
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

                // Draw the transform at the center of mass.

                const float axisScale = 0.4f;
                var p1 = body.Sweep.LocalCenter;

                //Vector2 p2 = p1 + axisScale * transform.R.Col1;
                var p2 = p1 + Vector2.UnitX * axisScale;
                _primitiveBatch.DrawSegment(p1, p2, Color.Red);

                //p2 = p1 + axisScale * transform.R.Col2;
                p2 = p1 + Vector2.UnitY * axisScale;
                _primitiveBatch.DrawSegment(p1, p2, Color.Green);

                _primitiveBatch.End();
            }
        }

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
                    _primitiveBatch = new PrimitiveBatch(cm.Value.Graphics.GraphicsDevice);
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    if (_primitiveBatch != null)
                    {
                        _primitiveBatch.Dispose();
                        _primitiveBatch = null;
                    }
                }
            }
        }
    }
}
