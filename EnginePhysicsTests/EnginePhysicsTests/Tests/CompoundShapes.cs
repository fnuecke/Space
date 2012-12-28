using Engine.Physics.Components;
using Engine.Random;
using Microsoft.Xna.Framework;

namespace EnginePhysicsTests.Tests
{
    sealed class CompoundShapes : AbstractTest
    {
        protected override void Create()
        {
            var random = new MersenneTwister(0);

            Manager.AddEdge(new Vector2(50.0f, 0.0f), new Vector2(-50.0f, 0.0f));

            for (var i = 0; i < 10; ++i)
            {
                var entity = Manager.AddCircle(radius: 0.5f,
                                               type: Body.BodyType.Dynamic,
                                               localPosition: new Vector2(-0.5f, 0.5f),
                                               worldPosition:
                                                   new Vector2((float)random.NextDouble(-0.1f, 0.1f) + 5.0f,
                                                               1.05f + 2.5f * i),
                                               worldAngle: (float)random.NextDouble(-MathHelper.Pi, MathHelper.Pi),
                                               density: 2);
                Manager.AttachCircle(entity,
                                     radius: 0.5f,
                                     localPosition: new Vector2(0.5f, 0.5f));
            }

            //*
            for (var i = 0; i < 10; ++i)
            {
                var entity = Manager.AddRectangle(width: 0.5f, height: 1f,
                                                  type: Body.BodyType.Dynamic,
                                                  worldPosition:
                                                      new Vector2((float)random.NextDouble(-0.1f, 0.1f) - 5.0f,
                                                                  1.05f + 2.5f * i),
                                                  worldAngle: (float)random.NextDouble(-MathHelper.Pi, MathHelper.Pi),
                                                  density: 2);
                Manager.AttachRectangle(entity,
                                        width: 0.5f, height: 1f,
                                        localPosition: new Vector2(0.0f, -0.5f),
                                        localAngle: MathHelper.PiOver2,
                                        density: 2);
            }
            //*/

            //{
            //    b2Transform xf1;
            //    xf1.q.Set(0.3524f * b2_pi);
            //    xf1.p = xf1.q.GetXAxis();

            //    Vector2
            //    vertices[3];

            //    b2PolygonShape triangle1;
            //    vertices[0] = b2Mul(xf1, Vector2(-1.0f, 0.0f));
            //    vertices[1] = b2Mul(xf1, Vector2(1.0f, 0.0f));
            //    vertices[2] = b2Mul(xf1, Vector2(0.0f, 0.5f));
            //    triangle1.Set(vertices, 3);

            //    b2Transform xf2;
            //    xf2.q.Set(-0.3524f * b2_pi);
            //    xf2.p = -xf2.q.GetXAxis();

            //    b2PolygonShape triangle2;
            //    vertices[0] = b2Mul(xf2, Vector2(-1.0f, 0.0f));
            //    vertices[1] = b2Mul(xf2, Vector2(1.0f, 0.0f));
            //    vertices[2] = b2Mul(xf2, Vector2(0.0f, 0.5f));
            //    triangle2.Set(vertices, 3);

            //    for (int i = 0; i < 10; ++i)
            //    {
            //        float x = RandomFloat(-0.1f, 0.1f);
            //        b2BodyDef bd;
            //        bd.type = b2_dynamicBody;
            //        bd.position.Set(x, 2.05f + 2.5f * i);
            //        bd.angle = 0.0f;
            //        b2Body* body = m_world->CreateBody(&bd);
            //        body->CreateFixture(&triangle1, 2.0f);
            //        body->CreateFixture(&triangle2, 2.0f);
            //    }
            //}

            {
                var entity = Manager.AddRectangle(width: 3, height: 0.3f,
                                                  type: Body.BodyType.Dynamic,
                                                  worldPosition: new Vector2(0, 2),
                                                  density: 4);
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
