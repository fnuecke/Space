using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace EnginePhysicsTests.Tests
{
    internal class SphereStack : AbstractTest
    {
        protected override void Create()
        {
            Manager.AddEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));

            const int count = 10;
            for (var i = 0; i < count; ++i)
            {
                var sphere = Manager.AddCircle(radius: 1,
                                               type: Body.BodyType.Dynamic,
                                               worldPosition: new WorldPoint(0, 4.0f + 3.0f * i),
                                               density: 1);
                sphere.LinearVelocity = new Vector2(0.0f, -50.0f);
            }
        }
    }
}
