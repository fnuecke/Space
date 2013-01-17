using Engine.ComponentSystem.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Tests.Tests
{
    internal sealed class Pyramid : AbstractTest
    {
        protected override void Create()
        {
            Manager.AddEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));

            var columOrigin = new WorldPoint(-7.0f, 0.75f);
            var deltaX = new Vector2(0.5625f, 1.25f);
            var deltaY = new Vector2(1.125f, 0.0f);

            const int count = 35;

            for (var i = 0; i < count; ++i)
            {
                var columnPosition = columOrigin;

                for (var j = i; j < count; ++j)
                {
                    Manager.AddRectangle(width: 1, height: 1,
                                         worldPosition: columnPosition,
                                         type: Body.BodyType.Dynamic,
                                         density: 5);

                    columnPosition += deltaY;
                }

                columOrigin += deltaX;
            }
        }
    }
}
