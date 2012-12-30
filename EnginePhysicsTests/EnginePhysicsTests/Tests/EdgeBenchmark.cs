using System;
using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace EnginePhysicsTests.Tests
{
    internal sealed class EdgeBenchmark : AbstractTest
    {
        private int _count;

        private const float W = 1.0f;

        private const float T = 2.0f;

        private static readonly float B = W / (2.0f + (float)Math.Sqrt(T));

        private static readonly float S = (float)Math.Sqrt(T) * B;

        private static readonly Vector2[] PolyShape = new[]
        {
            new Vector2(0.5f * S, 0.0f),
            new Vector2(0.5f * W, B),
            new Vector2(0.5f * W, B + S),
            new Vector2(0.5f * S, W),
            new Vector2(-0.5f * S, W),
            new Vector2(-0.5f * W, B + S),
            new Vector2(-0.5f * W, B),
            new Vector2(-0.5f * S, 0.0f)
        };

        protected override void Create()
        {
            // Ground body
            {
                var ground = Manager.AddBody();

                var x1 = -20.0f;
                var y1 = 2.0f * (float)Math.Cos(x1 / 10.0f * (float)Math.PI);
                for (var i = 0; i < 80; ++i)
                {
                    var x2 = x1 + 0.5f;
                    var y2 = 2.0f * (float)Math.Cos(x2 / 10.0f * (float)Math.PI);

                    Manager.AttachEdge(ground,
                                       new Vector2(x1, y1), new Vector2(x2, y2));

                    x1 = x2;
                    y1 = y2;
                }
            }

            _count = 0;
        }

        protected override void Step()
        {
            _count++;

            if (_count < 50)
            {
                const float x = 0;
                const float y = 15;

                Manager.AddPolygon(vertices: PolyShape,
                                   type: Body.BodyType.Dynamic,
                                   worldPosition: new WorldPoint(x, y),
                                   density: 20, friction: 0.3f);
            }
        }
    }
}
