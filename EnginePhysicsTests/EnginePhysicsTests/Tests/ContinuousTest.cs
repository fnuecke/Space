using Engine.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace EnginePhysicsTests.Tests
{
    internal sealed class ContinuousTest : AbstractTest
    {
        private Body _body;

        protected override void Create()
        {
            {
                var entity = Manager.AddEdge(new Vector2(-10.0f, 0.0f), new Vector2(10.0f, 0.0f));
                Manager.AttachRectangle(entity, width: 0.4f, height: 2f,
                                        localPosition: new Vector2(0.5f, 1.0f));
            }

            {
                _body = Manager.AddRectangle(width: 4, height: 0.2f,
                                             type: Body.BodyType.Dynamic,
                                             worldPosition: new WorldPoint(0, 20),
                                             density: 1);

                //m_angularVelocity = 46.661274f;
                _body.LinearVelocity = new Vector2(0.0f, -100.0f);
                _body.AngularVelocity = (float)Random.NextDouble(-50.0f, 50.0f);
            }
        }

        private void Launch()
        {
            _body.SetTransform(new WorldPoint(0.0f, 20.0f), 0.0f);
            _body.LinearVelocity = new Vector2(0.0f, -100.0f);
            _body.AngularVelocity = (float)Random.NextDouble(-50.0f, 50.0f);
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
