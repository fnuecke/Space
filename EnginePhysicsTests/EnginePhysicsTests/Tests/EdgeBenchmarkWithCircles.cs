using System;
using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class EdgeBenchmarkWithCircles : AbstractTest
    {
        private int _count;

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
                var x = (float)Random.NextDouble(-1, 1);
                const float y = 15;

                Manager.AddCircle(radius: 0.5f,
                                  type: Body.BodyType.Dynamic,
                                  worldPosition: new WorldPoint(x, y),
                                  density: 20, friction: 0.3f);
            }
        }
    }
}
