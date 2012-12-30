using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace EnginePhysicsTests.Tests
{
    internal class BulletTest : AbstractTest
    {
        private Body _block;

        private Body _bullet;

        protected override void Create()
        {
            {
                var ground = Manager.AddEdge(new Vector2(-10.0f, 0.0f), new Vector2(10.0f, 0.0f));
                Manager.AttachRectangle(ground, width: 0.4f, height: 2f,
                                        localPosition: new Vector2(0.5f, 1.0f));
            }

            {
                _block = Manager.AddRectangle(width: 4f, height: 0.2f,
                                              type: Body.BodyType.Dynamic,
                                              worldPosition: new WorldPoint(0, 4),
                                              density: 1);

                //var x = (float)_random.NextDouble(-1.0f, 1.0f);
                const float x = 0.20352793f;

                _bullet = Manager.AddRectangle(width: 0.5f, height: 0.5f,
                                               type: Body.BodyType.Dynamic,
                                               worldPosition: new WorldPoint(x, 10f),
                                               isBullet: true,
                                               density: 100);
                _bullet.LinearVelocity = new Vector2(0.0f, -50.0f);
            }
        }

        private void Launch()
        {
            _block.SetTransform(new WorldPoint(0.0f, 4.0f), 0);
            _block.LinearVelocity = Vector2.Zero;
            _block.AngularVelocity = 0;

            var x = (float)Random.NextDouble(-1.0f, 1.0f);
            _bullet.SetTransform(new WorldPoint(x, 10.0f), 0);
            _bullet.LinearVelocity = new Vector2(0.0f, -50.0f);
            _bullet.AngularVelocity = 0;
        }

        protected override void Step()
        {
            if (StepCount % 60 == 0)
            {
                Launch();
            }
        }
    }
}
