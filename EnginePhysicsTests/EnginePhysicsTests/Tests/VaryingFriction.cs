using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class VaryingFriction : AbstractTest
    {
        protected override void Create()
        {
            var ground = Manager.AddEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));

            Manager.AttachRectangle(ground,
                                    width: 26.0f, height: 0.5f,
                                    localPosition: new Vector2(-4.0f, 22.0f),
                                    localAngle: -0.25f);

            Manager.AttachRectangle(ground,
                                    width: 0.5f, height: 2.0f,
                                    localPosition: new Vector2(10.5f, 19.0f));

            Manager.AttachRectangle(ground,
                                    width: 26.0f, height: 0.5f,
                                    localPosition: new Vector2(4.0f, 14.0f),
                                    localAngle: 0.25f);

            Manager.AttachRectangle(ground,
                                    width: 0.5f, height: 2.0f,
                                    localPosition: new Vector2(-10.5f, 11.0f));

            Manager.AttachRectangle(ground,
                                    width: 26.0f, height: 0.5f,
                                    localPosition: new Vector2(-4.0f, 6.0f),
                                    localAngle: -0.25f);

            var frictions = new[] {0.75f, 0.5f, 0.35f, 0.1f, 0.0f};

            for (var i = 0; i < frictions.Length; ++i)
            {
                Manager.AddRectangle(width: 1, height: 1,
                                     type: Body.BodyType.Dynamic,
                                     worldPosition: new WorldPoint(-15.0f + 4.0f * i, 28.0f),
                                     density: 25,
                                     friction: frictions[i]);
            }
        }
    }
}
