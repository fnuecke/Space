using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.Math;
using Engine.Physics.Components;
using Engine.Physics.Detail;
using Engine.Physics.Detail.Collision;
using Engine.Physics.Detail.Contacts;
using Engine.Physics.Detail.Math;
using Engine.Physics.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.DebugViews
{
    /// <summary>
    /// A debug view that works in XNA.
    /// A debug view shows you what happens inside the physics engine. You can view
    /// bodies, joints, fixtures and more.
    /// </summary>
    internal class DebugViewXNA : DebugView, IDisposable
    {
        //Drawing
        private PrimitiveBatch _primitiveBatch;
        private SpriteBatch _batch;
        private SpriteFont _font;
        private GraphicsDevice _device;
        private Vector2[] _tempVertices = new Vector2[Settings.MaxPolygonVertices];
        private List<StringData> _stringData;

        private Matrix _localProjection;
        private Matrix _localView;

        //Shapes
        public Color DefaultShapeColor = new Color(0.9f, 0.7f, 0.7f);
        public Color InactiveShapeColor = new Color(0.5f, 0.5f, 0.3f);
        public Color KinematicShapeColor = new Color(0.5f, 0.5f, 0.9f);
        public Color SleepingShapeColor = new Color(0.6f, 0.6f, 0.6f);
        public Color StaticShapeColor = new Color(0.5f, 0.9f, 0.5f);
        public Color TextColor = Color.White;

        //Contacts
        private int _pointCount;
        private const int MaxContactPoints = 2048;
        private ContactPoint[] _points = new ContactPoint[MaxContactPoints];

        //Debug panel
#if XBOX
        public Vector2 DebugPanelPosition = new Vector2(55, 100);
#else
        public Vector2 DebugPanelPosition = new Vector2(40, 100);
#endif
        private int _max;
        private int _avg;
        private int _min;

        //Performance graph
        public bool AdaptiveLimits = true;
        public int ValuesToGraph = 500;
        public int MinimumValue;
        public int MaximumValue = 1000;
        private List<float> _graphValues = new List<float>();

#if XBOX
        public Rectangle PerformancePanelBounds = new Rectangle(265, 100, 200, 100);
#else
        public Rectangle PerformancePanelBounds = new Rectangle(250, 100, 200, 100);
#endif
        private Vector2[] _background = new Vector2[4];
        public bool Enabled = true;

#if XBOX || WINDOWS_PHONE
        public const int CircleSegments = 16;
#else
        public const int CircleSegments = 32;
#endif

        public DebugViewXNA(PhysicsSystem world)
            : base(world)
        {
            //world.ContactManager.PreSolve += PreSolve;

            //Default flags
            AppendFlags(DebugViewFlags.Shape);
            AppendFlags(DebugViewFlags.Controllers);
            AppendFlags(DebugViewFlags.Joint);
        }

        public void BeginCustomDraw(ref Matrix view)
        {
            _primitiveBatch.Begin(ref view);
        }

        public void EndCustomDraw()
        {
            _primitiveBatch.End();
        }

        #region IDisposable Members

        public void Dispose()
        {
            //World.ContactManager.PreSolve -= PreSolve;
        }

        #endregion

        //private void PreSolve(Contact contact, ref Manifold oldManifold)
        //{
        //    if ((Flags & DebugViewFlags.ContactPoints) == DebugViewFlags.ContactPoints)
        //    {
        //        Manifold manifold = contact.Manifold;

        //        if (manifold.pointCount == 0)
        //        {
        //            return;
        //        }

        //        Fixture fixtureA = World.Manager.GetComponentById(contact.FixtureA) as Fixture;

        //        FixedArray2<PointState> state1, state2;
        //        Collision.Collision.GetPointStates(out state1, out state2, ref oldManifold, ref manifold);

        //        FixedArray2<Vector2> points;
        //        Vector2 normal;
        //        contact.GetWorldManifold(out normal, out points);

        //        for (int i = 0; i < manifold.PointCount && _pointCount < MaxContactPoints; ++i)
        //        {
        //            if (fixtureA == null)
        //            {
        //                _points[i] = new ContactPoint();
        //            }
        //            ContactPoint cp = _points[_pointCount];
        //            cp.Position = points[i];
        //            cp.Normal = normal;
        //            cp.State = state2[i];
        //            _points[_pointCount] = cp;
        //            ++_pointCount;
        //        }
        //    }
        //}

        /// <summary>
        /// Call this to draw shapes and other debug draw data.
        /// </summary>
        private void DrawDebugData()
        {
            //if ((Flags & DebugViewFlags.Shape) == DebugViewFlags.Shape)
            //{
            //    foreach (Body b in World.Bodies)
            //    {
            //        WorldTransform xf = b._transform;
            //        foreach (Fixture f in World.Manager.GetComponents(b.Entity, Fixture.TypeId))
            //        {
            //            if (b.Enabled == false)
            //            {
            //                DrawShape(f, xf, InactiveShapeColor);
            //            }
            //            else if (b.Type == Body.BodyType.Static)
            //            {
            //                DrawShape(f, xf, StaticShapeColor);
            //            }
            //            else if (b.Type == Body.BodyType.Kinematic)
            //            {
            //                DrawShape(f, xf, KinematicShapeColor);
            //            }
            //            else if (b.IsAwake == false)
            //            {
            //                DrawShape(f, xf, SleepingShapeColor);
            //            }
            //            else
            //            {
            //                DrawShape(f, xf, DefaultShapeColor);
            //            }
            //        }
            //    }
            //}
            //if ((Flags & DebugViewFlags.ContactPoints) == DebugViewFlags.ContactPoints)
            //{
            //    const float axisScale = 0.3f;

            //    for (int i = 0; i < _pointCount; ++i)
            //    {
            //        ContactPoint point = _points[i];

            //        if (point.State == PointState.Add)
            //        {
            //            // Add
            //            DrawPoint(point.Position, 0.1f, new Color(0.3f, 0.95f, 0.3f));
            //        }
            //        else if (point.State == PointState.Persist)
            //        {
            //            // Persist
            //            DrawPoint(point.Position, 0.1f, new Color(0.3f, 0.3f, 0.95f));
            //        }

            //        if ((Flags & DebugViewFlags.ContactNormals) == DebugViewFlags.ContactNormals)
            //        {
            //            Vector2 p1 = point.Position;
            //            Vector2 p2 = p1 + axisScale * point.Normal;
            //            DrawSegment(p1, p2, new Color(0.4f, 0.9f, 0.4f));
            //        }
            //    }
            //    _pointCount = 0;
            //}
            //if ((Flags & DebugViewFlags.PolygonPoints) == DebugViewFlags.PolygonPoints)
            //{
            //    foreach (Body body in World.BodyList)
            //    {
            //        foreach (Fixture f in body.FixtureList)
            //        {
            //            PolygonShape polygon = f.Shape as PolygonShape;
            //            if (polygon != null)
            //            {
            //                WorldTransform xf = body._xf;

            //                for (int i = 0; i < polygon.Vertices.Count; i++)
            //                {
            //                    Vector2 tmp = MathUtils.Multiply(ref xf, polygon.Vertices[i]);
            //                    DrawPoint(tmp, 0.1f, Color.Red);
            //                }
            //            }
            //        }
            //    }
            //}
            //if ((Flags & DebugViewFlags.Pair) == DebugViewFlags.Pair)
            //{
            //    Color color = new Color(0.3f, 0.9f, 0.9f);
            //    for (int i = 0; i < World.ContactManager.ContactList.Count; i++)
            //    {
            //        Contact c = World.ContactManager.ContactList[i];
            //        Fixture fixtureA = c.FixtureA;
            //        Fixture fixtureB = c.FixtureB;

            //        AABB aabbA;
            //        fixtureA.GetAABB(out aabbA, 0);
            //        AABB aabbB;
            //        fixtureB.GetAABB(out aabbB, 0);

            //        Vector2 cA = aabbA.Center;
            //        Vector2 cB = aabbB.Center;

            //        DrawSegment(cA, cB, color);
            //    }
            //}
            //if ((Flags & DebugViewFlags.AABB) == DebugViewFlags.AABB)
            //{
            //    Color color = new Color(0.9f, 0.3f, 0.9f);
            //    IBroadPhase bp = World.ContactManager.BroadPhase;

            //    foreach (Body b in World.BodyList)
            //    {
            //        if (b.Enabled == false)
            //        {
            //            continue;
            //        }

            //        foreach (Fixture f in b.FixtureList)
            //        {
            //            for (int t = 0; t < f.ProxyCount; ++t)
            //            {
            //                FixtureProxy proxy = f.Proxies[t];
            //                AABB aabb;
            //                bp.GetFatAABB(proxy.ProxyId, out aabb);

            //                DrawAABB(ref aabb, color);
            //            }
            //        }
            //    }
            //}
            //if ((Flags & DebugViewFlags.CenterOfMass) == DebugViewFlags.CenterOfMass)
            //{
            //    foreach (Body b in World.BodyList)
            //    {
            //        WorldTransform xf = b._xf;
            //        xf.p = b.WorldCenter;
            //        DrawTransform(ref xf);
            //    }
            //}
            //if ((Flags & DebugViewFlags.DebugPanel) == DebugViewFlags.DebugPanel)
            //{
            //    DrawDebugPanel();
            //}
        }

        //private void DrawPerformanceGraph()
        //{
        //    _graphValues.Add(World.UpdateTime);

        //    if (_graphValues.Count > ValuesToGraph + 1)
        //        _graphValues.RemoveAt(0);

        //    float x = PerformancePanelBounds.X;
        //    float deltaX = PerformancePanelBounds.Width / (float)ValuesToGraph;
        //    float yScale = PerformancePanelBounds.Bottom - (float)PerformancePanelBounds.Top;

        //    // we must have at least 2 values to start rendering
        //    if (_graphValues.Count > 2)
        //    {
        //        _max = (int)_graphValues.Max();
        //        _avg = (int)_graphValues.Average();
        //        _min = (int)_graphValues.Min();

        //        if (AdaptiveLimits)
        //        {
        //            MaximumValue = _max;
        //            MinimumValue = 0;
        //        }

        //        // start at last value (newest value added)
        //        // continue until no values are left
        //        for (int i = _graphValues.Count - 1; i > 0; i--)
        //        {
        //            float y1 = PerformancePanelBounds.Bottom -
        //                       ((_graphValues[i] / (MaximumValue - MinimumValue)) * yScale);
        //            float y2 = PerformancePanelBounds.Bottom -
        //                       ((_graphValues[i - 1] / (MaximumValue - MinimumValue)) * yScale);

        //            Vector2 x1 =
        //                new Vector2(MathHelper.Clamp(x, PerformancePanelBounds.Left, PerformancePanelBounds.Right),
        //                            MathHelper.Clamp(y1, PerformancePanelBounds.Top, PerformancePanelBounds.Bottom));

        //            Vector2 x2 =
        //                new Vector2(
        //                    MathHelper.Clamp(x + deltaX, PerformancePanelBounds.Left, PerformancePanelBounds.Right),
        //                    MathHelper.Clamp(y2, PerformancePanelBounds.Top, PerformancePanelBounds.Bottom));

        //            DrawSegment(x1, x2, Color.LightGreen);

        //            x += deltaX;
        //        }
        //    }

        //    DrawString(PerformancePanelBounds.Right + 10, PerformancePanelBounds.Top, "Max: " + _max);
        //    DrawString(PerformancePanelBounds.Right + 10, PerformancePanelBounds.Center.Y - 7, "Avg: " + _avg);
        //    DrawString(PerformancePanelBounds.Right + 10, PerformancePanelBounds.Bottom - 15, "Min: " + _min);

        //    //Draw background.
        //    _background[0] = new Vector2(PerformancePanelBounds.X, PerformancePanelBounds.Y);
        //    _background[1] = new Vector2(PerformancePanelBounds.X,
        //                                 PerformancePanelBounds.Y + PerformancePanelBounds.Height);
        //    _background[2] = new Vector2(PerformancePanelBounds.X + PerformancePanelBounds.Width,
        //                                 PerformancePanelBounds.Y + PerformancePanelBounds.Height);
        //    _background[3] = new Vector2(PerformancePanelBounds.X + PerformancePanelBounds.Width,
        //                                 PerformancePanelBounds.Y);

        //    DrawSolidPolygon(_background, 4, Color.DarkGray, true);
        //}

        //private void DrawDebugPanel()
        //{
        //    int fixtures = 0;
        //    for (int i = 0; i < World.BodyList.Count; i++)
        //    {
        //        fixtures += World.BodyList[i].FixtureList.Count;
        //    }

        //    int x = (int)DebugPanelPosition.X;
        //    int y = (int)DebugPanelPosition.Y;

        //    DrawString(x, y, "Objects:" +
        //                     "\n- Bodies: " + World.BodyList.Count +
        //                     "\n- Fixtures: " + fixtures +
        //                     "\n- Contacts: " + World.ContactList.Count +
        //                     "\n- Joints: " + World.JointList.Count +
        //                     "\n- Controllers: " + World.ControllerList.Count +
        //                     "\n- Proxies: " + World.ProxyCount);

        //    DrawString(x + 110, y, "Update time:" +
        //                           "\n- Body: " + World.SolveUpdateTime +
        //                           "\n- Contact: " + World.ContactsUpdateTime +
        //                           "\n- CCD: " + World.ContinuousPhysicsTime +
        //                           "\n- Joint: " + World.Island.JointUpdateTime +
        //                           "\n- Controller: " + World.ControllersUpdateTime +
        //                           "\n- Total: " + World.UpdateTime);
        //}

        public void DrawAABB(ref RectangleF aabb, Color color)
        {
            Vector2[] verts = new Vector2[4];
            verts[0] = new Vector2(aabb.Left, aabb.Top);
            verts[1] = new Vector2(aabb.Right, aabb.Top);
            verts[2] = new Vector2(aabb.Right, aabb.Bottom);
            verts[3] = new Vector2(aabb.Left, aabb.Bottom);

            DrawPolygon(verts, 4, color);
        }

        public void DrawShape(Fixture fixture, WorldTransform xf, Color color)
        {
            switch (fixture.Type)
            {
                case Fixture.FixtureType.Circle:
                    {
                        //CircleFixture circle = (CircleFixture)fixture;

                        //Vector2 center = xf.ToGlobal(circle._center);
                        //float radius = circle.Radius;
                        //Vector2 axis = xf.R.Col1;

                        //DrawSolidCircle(center, radius, axis, color);
                    }
                    break;

                case Fixture.FixtureType.Polygon:
                    {
                        PolygonFixture poly = (PolygonFixture)fixture;
                        int vertexCount = poly.Count;
                        Debug.Assert(vertexCount <= Settings.MaxPolygonVertices);

                        for (int i = 0; i < vertexCount; ++i)
                        {
                            _tempVertices[i] = xf.ToGlobal(poly.Vertices[i]);
                        }

                        DrawSolidPolygon(_tempVertices, vertexCount, color);
                    }
                    break;


                case Fixture.FixtureType.Edge:
                    {
                        EdgeFixture edge = (EdgeFixture)fixture;
                        Vector2 v1 = xf.ToGlobal(edge.Vertex1);
                        Vector2 v2 = xf.ToGlobal(edge.Vertex2);
                        DrawSegment(v1, v2, color);
                    }
                    break;
            }
        }

        public override void DrawPolygon(Vector2[] vertices, int count, float red, float green, float blue)
        {
            DrawPolygon(vertices, count, new Color(red, green, blue));
        }

        public void DrawPolygon(Vector2[] vertices, int count, Color color)
        {
            if (!_primitiveBatch.IsReady())
            {
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
            }
            for (int i = 0; i < count - 1; i++)
            {
                _primitiveBatch.AddVertex(vertices[i], color, PrimitiveType.LineList);
                _primitiveBatch.AddVertex(vertices[i + 1], color, PrimitiveType.LineList);
            }

            _primitiveBatch.AddVertex(vertices[count - 1], color, PrimitiveType.LineList);
            _primitiveBatch.AddVertex(vertices[0], color, PrimitiveType.LineList);
        }

        public override void DrawSolidPolygon(Vector2[] vertices, int count, float red, float green, float blue)
        {
            DrawSolidPolygon(vertices, count, new Color(red, green, blue), true);
        }

        public void DrawSolidPolygon(Vector2[] vertices, int count, Color color)
        {
            DrawSolidPolygon(vertices, count, color, true);
        }

        public void DrawSolidPolygon(Vector2[] vertices, int count, Color color, bool outline)
        {
            if (!_primitiveBatch.IsReady())
            {
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
            }
            if (count == 2)
            {
                DrawPolygon(vertices, count, color);
                return;
            }

            Color colorFill = color * (outline ? 0.5f : 1.0f);

            for (int i = 1; i < count - 1; i++)
            {
                _primitiveBatch.AddVertex(vertices[0], colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(vertices[i], colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(vertices[i + 1], colorFill, PrimitiveType.TriangleList);
            }

            if (outline)
            {
                DrawPolygon(vertices, count, color);
            }
        }

        public override void DrawCircle(Vector2 center, float radius, float red, float green, float blue)
        {
            DrawCircle(center, radius, new Color(red, green, blue));
        }

        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            if (!_primitiveBatch.IsReady())
            {
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
            }
            const double increment = Math.PI * 2.0 / CircleSegments;
            double theta = 0.0;

            for (int i = 0; i < CircleSegments; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center +
                             radius *
                             new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _primitiveBatch.AddVertex(v1, color, PrimitiveType.LineList);
                _primitiveBatch.AddVertex(v2, color, PrimitiveType.LineList);

                theta += increment;
            }
        }

        public override void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, float red, float green,
                                             float blue)
        {
            DrawSolidCircle(center, radius, axis, new Color(red, green, blue));
        }

        public void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, Color color)
        {
            if (!_primitiveBatch.IsReady())
            {
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
            }
            const double increment = Math.PI * 2.0 / CircleSegments;
            double theta = 0.0;

            Color colorFill = color * 0.5f;

            Vector2 v0 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
            theta += increment;

            for (int i = 1; i < CircleSegments - 1; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center +
                             radius *
                             new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _primitiveBatch.AddVertex(v0, colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(v1, colorFill, PrimitiveType.TriangleList);
                _primitiveBatch.AddVertex(v2, colorFill, PrimitiveType.TriangleList);

                theta += increment;
            }
            DrawCircle(center, radius, color);

            DrawSegment(center, center + axis * radius, color);
        }

        public override void DrawSegment(Vector2 start, Vector2 end, float red, float green, float blue)
        {
            DrawSegment(start, end, new Color(red, green, blue));
        }

        public void DrawSegment(Vector2 start, Vector2 end, Color color)
        {
            if (!_primitiveBatch.IsReady())
            {
                throw new InvalidOperationException("BeginCustomDraw must be called before drawing anything.");
            }
            _primitiveBatch.AddVertex(start, color, PrimitiveType.LineList);
            _primitiveBatch.AddVertex(end, color, PrimitiveType.LineList);
        }

        public override void DrawTransform(ref WorldTransform transform)
        {
            const float axisScale = 0.4f;
            Vector2 p1 = transform.Translation;

            //Vector2 p2 = p1 + axisScale * transform.R.Col1;
            Vector2 p2 = p1 + axisScale * (transform.Rotation * Vector2.UnitX);
            DrawSegment(p1, p2, Color.Red);

            //p2 = p1 + axisScale * transform.R.Col2;
            p2 = p1 + axisScale * (transform.Rotation * Vector2.UnitY);
            DrawSegment(p1, p2, Color.Green);
        }

        public void DrawPoint(Vector2 p, float size, Color color)
        {
            Vector2[] verts = new Vector2[4];
            float hs = size / 2.0f;
            verts[0] = p + new Vector2(-hs, -hs);
            verts[1] = p + new Vector2(hs, -hs);
            verts[2] = p + new Vector2(hs, hs);
            verts[3] = p + new Vector2(-hs, hs);

            DrawSolidPolygon(verts, 4, color, true);
        }

        public void DrawString(int x, int y, string s, params object[] args)
        {
            _stringData.Add(new StringData(x, y, s, args, TextColor));
        }

        public void DrawArrow(Vector2 start, Vector2 end, float length, float width, bool drawStartIndicator,
                              Color color)
        {
            // Draw connection segment between start- and end-point
            DrawSegment(start, end, color);

            // Precalculate halfwidth
            float halfWidth = width / 2;

            // Create directional reference
            Vector2 rotation = (start - end);
            rotation.Normalize();

            // Calculate angle of directional vector
            float angle = (float)Math.Atan2(rotation.X, -rotation.Y);
            // Create matrix for rotation
            Matrix rotMatrix = Matrix.CreateRotationZ(angle);
            // Create translation matrix for end-point
            Matrix endMatrix = Matrix.CreateTranslation(end.X, end.Y, 0);

            // Setup arrow end shape
            Vector2[] verts = new Vector2[3];
            verts[0] = new Vector2(0, 0);
            verts[1] = new Vector2(-halfWidth, -length);
            verts[2] = new Vector2(halfWidth, -length);

            // Rotate end shape
            Vector2.Transform(verts, ref rotMatrix, verts);
            // Translate end shape
            Vector2.Transform(verts, ref endMatrix, verts);

            // Draw arrow end shape
            DrawSolidPolygon(verts, 3, color, false);

            if (drawStartIndicator)
            {
                // Create translation matrix for start
                Matrix startMatrix = Matrix.CreateTranslation(start.X, start.Y, 0);
                // Setup arrow start shape
                Vector2[] baseVerts = new Vector2[4];
                baseVerts[0] = new Vector2(-halfWidth, length / 4);
                baseVerts[1] = new Vector2(halfWidth, length / 4);
                baseVerts[2] = new Vector2(halfWidth, 0);
                baseVerts[3] = new Vector2(-halfWidth, 0);

                // Rotate start shape
                Vector2.Transform(baseVerts, ref rotMatrix, baseVerts);
                // Translate start shape
                Vector2.Transform(baseVerts, ref startMatrix, baseVerts);
                // Draw start shape
                DrawSolidPolygon(baseVerts, 4, color, false);
            }
        }

        public void RenderDebugData(ref Matrix projection, ref Matrix view)
        {
            if (!Enabled)
            {
                return;
            }

            //Nothing is enabled - don't draw the debug view.
            if (Flags == 0)
                return;

            _device.RasterizerState = RasterizerState.CullNone;
            _device.DepthStencilState = DepthStencilState.Default;

            _primitiveBatch.Begin( ref view);
            DrawDebugData();
            _primitiveBatch.End();

            //if ((Flags & DebugViewFlags.PerformanceGraph) == DebugViewFlags.PerformanceGraph)
            //{
            //    _primitiveBatch.Begin(ref _localProjection, ref _localView);
            //    DrawPerformanceGraph();
            //    _primitiveBatch.End();
            //}

            // begin the sprite batch effect
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // draw any strings we have
            for (int i = 0; i < _stringData.Count; i++)
            {
                _batch.DrawString(_font, string.Format(_stringData[i].S, _stringData[i].Args),
                                  new Vector2(_stringData[i].X + 1f, _stringData[i].Y + 1f), Color.Black);
                _batch.DrawString(_font, string.Format(_stringData[i].S, _stringData[i].Args),
                                  new Vector2(_stringData[i].X, _stringData[i].Y), _stringData[i].Color);
            }
            // end the sprite batch effect
            _batch.End();

            _stringData.Clear();
        }

        public void RenderDebugData(ref Matrix projection)
        {
            if (!Enabled)
            {
                return;
            }
            Matrix view = Matrix.Identity;
            RenderDebugData(ref projection, ref view);
        }

        public void LoadContent(GraphicsDevice device, ContentManager content)
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _device = device;
            _batch = new SpriteBatch(_device);
            _primitiveBatch = new PrimitiveBatch(_device, 1000);
            _font = content.Load<SpriteFont>("font");
            _stringData = new List<StringData>();

            _localProjection = Matrix.CreateOrthographicOffCenter(0f, _device.Viewport.Width, _device.Viewport.Height,
                                                                  0f, 0f, 1f);
            _localView = Matrix.Identity;
        }

        #region Nested type: ContactPoint

        private struct ContactPoint
        {
            public Vector2 Normal;
            public Vector2 Position;
            //public PointState State;
        }

        #endregion

        #region Nested type: StringData

        private struct StringData
        {
            public object[] Args;
            public Color Color;
            public string S;
            public int X, Y;

            public StringData(int x, int y, string s, object[] args, Color color)
            {
                X = x;
                Y = y;
                S = s;
                Args = args;
                Color = color;
            }
        }

        #endregion
    }
}