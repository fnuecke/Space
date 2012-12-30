using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    sealed class CharacterCollision : AbstractTest
    {
        private Body _character;

        protected override void Create()
        {
            // Ground body
            Manager.AddEdge(new Vector2(-20.0f, 0.0f), new Vector2(20.0f, 0.0f));

            // Collinear edges with no adjacency information.
            // This shows the problematic case where a box shape can hit
            // an internal vertex.
            {
                var ground = Manager.AddEdge(new Vector2(-8.0f, 1.0f), new Vector2(-6.0f, 1.0f));
                Manager.AttachEdge(ground, new Vector2(-6.0f, 1.0f), new Vector2(-4.0f, 1.0f));
                Manager.AttachEdge(ground, new Vector2(-4.0f, 1.0f), new Vector2(-2.0f, 1.0f));
            }

            // Chain shape
            {
                var vertices = new[]
                {
                    new Vector2(5.0f, 7.0f),
                    new Vector2(6.0f, 8.0f),
                    new Vector2(7.0f, 8.0f),
                    new Vector2(8.0f, 7.0f)
                };
                Manager.AddChain(vertices, worldAngle: 0.25f * MathHelper.Pi);
            }

            // Square tiles. This shows that adjacency shapes may
            // have non-smooth collision. There is no solution
            // to this problem.
            {
                var ground = Manager.AddRectangle(width: 2, height: 2, localPosition: new Vector2(4.0f, 3.0f));
                Manager.AttachRectangle(ground, width: 2, height: 2, localPosition: new Vector2(6.0f, 3.0f));
                Manager.AttachRectangle(ground, width: 2, height: 2, localPosition: new Vector2(8.0f, 3.0f));
            }

            // Square made from an edge loop. Collision should be smooth.
            {
                var vertices = new[]
                {
                    new Vector2(-1.0f, 3.0f),
                    new Vector2(1.0f, 3.0f),
                    new Vector2(1.0f, 5.0f),
                    new Vector2(-1.0f, 5.0f)
                };
                Manager.AddLoop(vertices);
            }

            // Edge loop. Collision should be smooth.
            {
                var vertices = new[]
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(6.0f, 0.0f),
                    new Vector2(6.0f, 2.0f),
                    new Vector2(4.0f, 1.0f),
                    new Vector2(2.0f, 2.0f),
                    new Vector2(0.0f, 2.0f),
                    new Vector2(-2.0f, 2.0f),
                    new Vector2(-4.0f, 3.0f),
                    new Vector2(-6.0f, 2.0f),
                    new Vector2(-6.0f, 0.0f)
                };
                Manager.AddLoop(vertices, worldPosition: new WorldPoint(-10.0f, 4.0f));
            }

            // Square character 1
            {
                Manager.AddRectangle(width: 1, height: 1,
                                     type: Body.BodyType.Dynamic,
                                     worldPosition: new WorldPoint(-3, 8),
                                     allowSleep: false, density: 20);
            }

            // Square character 2
            {
                Manager.AddRectangle(width: 0.5f, height: 0.5f,
                                     type: Body.BodyType.Dynamic,
                                     worldPosition: new WorldPoint(-5, 5),
                                     fixedRotation: true, allowSleep: false,
                                     density: 20);
            }

            // Hexagon character
            {
                var angle = 0.0f;
                const float delta = MathHelper.Pi / 3.0f;
                var vertices = new Vector2[6];
                for (var i = 0; i < 6; ++i)
                {
                    vertices[i] = new Vector2(0.5f * (float)System.Math.Cos(angle),
                                              0.5f * (float)System.Math.Sin(angle));
                    angle += delta;
                }

                Manager.AddPolygon(vertices,
                                   type: Body.BodyType.Dynamic,
                                   worldPosition: new WorldPoint(-5, 8),
                                   fixedRotation: true, allowSleep: false,
                                   density: 20);
            }

            // Circle character
            {
                Manager.AddCircle(radius: 0.5f,
                                  type: Body.BodyType.Dynamic,
                                  worldPosition: new WorldPoint(3, 5),
                                  fixedRotation: true, allowSleep: false,
                                  density: 20);
            }

            // Circle character
            {
                _character = Manager.AddCircle(radius: 0.25f,
                                                type: Body.BodyType.Dynamic,
                                                worldPosition: new WorldPoint(-7, 6),
                                                allowSleep: false, density: 20, friction: 1);
            }
        }
        
        protected override void Step()
        {
            var v = _character.LinearVelocity;
            v.X = -5.0f;
            _character.LinearVelocity = v;

            DrawString("This tests various character collision shapes.");
            DrawString("Limitation: square and hexagon can snag on aligned boxes.");
            DrawString("Feature: edge chains have smooth collision inside and out.");
        }
    }
}
