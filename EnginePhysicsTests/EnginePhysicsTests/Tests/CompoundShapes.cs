using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class CompoundShapes : AbstractTest
    {
        protected override void Create()
        {
            Manager.AddEdge(new Vector2(50.0f, 0.0f), new Vector2(-50.0f, 0.0f));

            for (var i = 0; i < 10; ++i)
            {
                var circle = Manager.AddCircle(radius: 0.5f,
                                               type: Body.BodyType.Dynamic,
                                               localPosition: new Vector2(-0.5f, 0.5f),
                                               worldPosition:
                                                   new WorldPoint((float)Random.NextDouble(-0.1f, 0.1f) + 5.0f,
                                                                  1.05f + 2.5f * i),
                                               worldAngle: (float)Random.NextDouble(-MathHelper.Pi, MathHelper.Pi),
                                               density: 2).Body;
                Manager.AttachCircle(circle,
                                     radius: 0.5f,
                                     localPosition: new Vector2(0.5f, 0.5f));
            }

            for (var i = 0; i < 10; ++i)
            {
                var position = new WorldPoint((float)Random.NextDouble(-0.1f, 0.1f) - 5.0f, 1.05f + 2.5f * i);
                var box = Manager.AddRectangle(width: 0.5f, height: 1f,
                                               type: Body.BodyType.Dynamic,
                                               worldPosition: position,
                                               worldAngle: (float)Random.NextDouble(-MathHelper.Pi, MathHelper.Pi),
                                               density: 2).Body;
                Manager.AttachRectangle(box,
                                        width: 0.5f, height: 1f,
                                        localPosition: new Vector2(0.0f, -0.5f),
                                        localAngle: MathHelper.PiOver2,
                                        density: 2);
            }

            {
                var xf1 = Matrix.CreateRotationZ(0.3524f * MathHelper.Pi);
                xf1.Translation = new Vector3(
                    Vector2.Transform(Vector2.UnitX, Quaternion.CreateFromRotationMatrix(xf1)), 0);
                var triangle1 = new[]
                {
                    Vector2.Transform(new Vector2(-1.0f, 0.0f), xf1),
                    Vector2.Transform(new Vector2(1.0f, 0.0f), xf1),
                    Vector2.Transform(new Vector2(0.0f, 0.5f), xf1)
                };

                var xf2 = Matrix.CreateRotationZ(-0.3524f * MathHelper.Pi);
                xf2.Translation =
                    new Vector3(Vector2.Transform(-Vector2.UnitX, Quaternion.CreateFromRotationMatrix(xf2)), 0);
                var triangle2 = new[]
                {
                    Vector2.Transform(new Vector2(-1.0f, 0.0f), xf2),
                    Vector2.Transform(new Vector2(1.0f, 0.0f), xf2),
                    Vector2.Transform(new Vector2(0.0f, 0.5f), xf2)
                };

                for (var i = 0; i < 10; ++i)
                {
                    var x = (float)Random.NextDouble(-0.1f, 0.1f);

                    var shape = Manager.AddPolygon(triangle1,
                                                   type: Body.BodyType.Dynamic,
                                                   worldPosition: new WorldPoint(x, 2.05f + 2.5f * i),
                                                   density: 2).Body;
                    Manager.AttachPolygon(shape, triangle2,
                                          density: 2);
                }
            }

            {
                var entity = Manager.AddRectangle(width: 3, height: 0.3f,
                                                  type: Body.BodyType.Dynamic,
                                                  worldPosition: new WorldPoint(0, 2),
                                                  density: 4).Body;
                Manager.AttachRectangle(entity,
                                        width: 0.3f, height: 5.4f,
                                        localPosition: new Vector2(-1.45f, 2.35f),
                                        localAngle: 0.2f,
                                        density: 4);
                Manager.AttachRectangle(entity,
                                        width: 0.3f, height: 5.4f,
                                        localPosition: new Vector2(1.45f, 2.35f),
                                        localAngle: -0.2f,
                                        density: 4);
            }
        }
    }
}
