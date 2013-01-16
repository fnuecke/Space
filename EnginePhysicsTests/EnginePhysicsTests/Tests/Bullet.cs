using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class Bullet : AbstractTest
    {
        private int _block;

        private int _bullet;

        protected override void Create()
        {
            {
                var ground = Manager.AddEdge(new Vector2(-10.0f, 0.0f), new Vector2(10.0f, 0.0f)).Body;
                Manager.AttachRectangle(ground, width: 0.4f, height: 2f,
                                        localPosition: new Vector2(0.5f, 1.0f));
            }

            {
                var block = Manager.AddRectangle(width: 4f, height: 0.2f,
                                                 type: Body.BodyType.Dynamic,
                                                 worldPosition: new WorldPoint(0, 4),
                                                 density: 1);
                _block = block.Entity;

                //var x = (float)_random.NextDouble(-1.0f, 1.0f);
                const float x = 0.20352793f;

                var bullet = Manager.AddRectangle(width: 0.5f, height: 0.5f,
                                                  type: Body.BodyType.Dynamic,
                                                  worldPosition: new WorldPoint(x, 10f),
                                                  isBullet: true,
                                                  density: 100).Body;
                bullet.LinearVelocity = new Vector2(0.0f, -50.0f);
                _bullet = bullet.Entity;
            }
        }

        private void Launch()
        {
            var block = (Body)Manager.GetComponent(_block, Body.TypeId);
            block.SetTransform(new WorldPoint(0.0f, 4.0f), 0);
            block.LinearVelocity = Vector2.Zero;
            block.AngularVelocity = 0;

            var bullet = (Body)Manager.GetComponent(_bullet, Body.TypeId);
            var x = (float)Random.NextDouble(-1.0f, 1.0f);
            bullet.SetTransform(new WorldPoint(x, 10.0f), 0);
            bullet.LinearVelocity = new Vector2(0.0f, -50.0f);
            bullet.AngularVelocity = 0; 
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
