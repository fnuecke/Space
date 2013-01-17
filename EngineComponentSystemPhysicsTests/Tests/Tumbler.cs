using Engine.ComponentSystem.Physics.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Tests.Tests
{
    internal sealed class Tumbler : AbstractTest
    {
        private const int Count = 800;
        private int _count;

        protected override void  Create()
        {
            var ground = Manager.AddBody();

            var tumbler = Manager.AddBody(
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(0, 10),
                allowSleep: false);

            Manager.AttachRectangle(tumbler, 1, 20, new Vector2(10, 0), density: 5);
            Manager.AttachRectangle(tumbler, 1, 20, new Vector2(-10, 0), density: 5);
            Manager.AttachRectangle(tumbler, 20, 1, new Vector2(0, 10), density: 5);
            Manager.AttachRectangle(tumbler, 20, 1, new Vector2(0, -10), density: 5);

            Manager.AddRevoluteJoint(
                ground,
                tumbler,
                tumbler.Position,
                motorSpeed: 0.05f * MathHelper.Pi,
                maxMotorTorque: 1e8f,
                enableMotor: true);

            _count = 0;
        }

        protected override void Step()
        {
            if (_count < Count)
            {
                Manager.AddRectangle(
                    0.25f,
                    0.25f,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(0, 10),
                    density: 1);
                ++_count;
            }
        }
    }
}
