using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Physics.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;

#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Tests.Tests
{
    internal sealed class Car : AbstractTest
    {
        private float _hz = 4;
        private const float Zeta = 0.7f;
        private const float Speed = 50;
        private const float Friction = 0.6f;

        private int _spring1;
        private int _spring2;

        protected override void Create()
        {
            var ground = Manager.AddEdge(
                new Vector2(-20.0f, 0.0f),
                new Vector2(20.0f, 0.0f),
                friction: Friction,
                collisionGroups: 1).Body;
            {
                var hs = new[] {0.25f, 1.0f, 4.0f, 0.0f, 0.0f, -1.0f, -2.0f, -2.0f, -1.25f, 0.0f};

                float x = 20.0f, y1 = 0.0f;
                const float dx = 5.0f;

                for (var i = 0; i < 10; ++i)
                {
                    var y2 = hs[i];
                    Manager.AttachEdge(
                        ground,
                        new Vector2(x, y1),
                        new Vector2(x + dx, y2),
                        friction: Friction,
                        collisionGroups: 1);
                    y1 = y2;
                    x += dx;
                }

                for (var i = 0; i < 10; ++i)
                {
                    var y2 = hs[i];
                    Manager.AttachEdge(
                        ground,
                        new Vector2(x, y1),
                        new Vector2(x + dx, y2),
                        friction: Friction,
                        collisionGroups: 1);
                    y1 = y2;
                    x += dx;
                }

                Manager.AttachEdge(
                    ground,
                    new Vector2(x, 0.0f),
                    new Vector2(x + 40.0f, 0.0f),
                    friction: Friction,
                    collisionGroups: 1);

                x += 80.0f;
                Manager.AttachEdge(
                    ground,
                    new Vector2(x, 0.0f),
                    new Vector2(x + 40.0f, 0.0f),
                    friction: Friction,
                    collisionGroups: 1);

                x += 40.0f;
                Manager.AttachEdge(
                    ground,
                    new Vector2(x, 0.0f),
                    new Vector2(x + 10.0f, 5.0f),
                    friction: Friction,
                    collisionGroups: 1);

                x += 20.0f;
                Manager.AttachEdge(
                    ground,
                    new Vector2(x, 0.0f),
                    new Vector2(x + 40.0f, 0.0f),
                    friction: Friction,
                    collisionGroups: 1);

                x += 40.0f;
                Manager.AttachEdge(
                    ground,
                    new Vector2(x, 0.0f),
                    new Vector2(x, 20.0f),
                    friction: Friction,
                    collisionGroups: 1);
            }

            // Teeter
            {
                var body = Manager.AddRectangle(
                    20,
                    0.5f,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(140, 1),
                    density: 1,
                    collisionGroups: 1).Body;

                Manager.AddRevoluteJoint(
                    body,
                    body.Position,
                    lowerAngle: MathHelper.ToRadians(-8),
                    upperAngle: MathHelper.ToRadians(8),
                    enableLimit: true);

                body.ApplyAngularImpulse(100.0f);
            }

            //Bridge
            {
                const int n = 20;

                var previousBody = ground;
                for (var i = 0; i < n; ++i)
                {
                    var body = Manager.AddRectangle(
                        2,
                        0.25f,
                        type: Body.BodyType.Dynamic,
                        worldPosition: new WorldPoint(161 + 2 * i, -0.125f),
                        density: 1,
                        friction: Friction,
                        collisionGroups: 1).Body;

                    Manager.AddRevoluteJoint(previousBody, body, new WorldPoint(160 + 2 * i, -0.125f));

                    previousBody = body;
                }

                Manager.AddRevoluteJoint(previousBody, new WorldPoint(160.0f + 2.0f * n, -0.125f));
            }

            // Boxes

            Manager.AddRectangle(
                1,
                1,
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(230, 0.5f),
                density: 0.5f);

            Manager.AddRectangle(
                1,
                1,
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(230, 1.5f),
                density: 0.5f);

            Manager.AddRectangle(
                1,
                1,
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(230, 2.5f),
                density: 0.5f);

            Manager.AddRectangle(
                1,
                1,
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(230, 3.5f),
                density: 0.5f);

            Manager.AddRectangle(
                1,
                1,
                type: Body.BodyType.Dynamic,
                worldPosition: new WorldPoint(230, 4.5f),
                density: 0.5f);

            // Car
            {
                var vertices = new[]
                {
                    new Vector2(-1.5f, -0.5f),
                    new Vector2(1.5f, -0.5f),
                    new Vector2(1.5f, 0.0f),
                    new Vector2(0.0f, 0.9f),
                    new Vector2(-1.15f, 0.9f),
                    new Vector2(-1.5f, 0.2f)
                };

                var car = Manager.AddPolygon(
                    vertices,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(0, 1),
                    density: 1).Body;

                var wheel1 = Manager.AddCircle(
                    0.4f,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(-1, 0.35f),
                    density: 1,
                    friction: 0.9f).Body;

                var wheel2 = Manager.AddCircle(
                    0.4f,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(1, 0.4f),
                    density: 1,
                    friction: 0.9f).Body;

                var spring1 = Manager.AddWheelJoint(
                    car,
                    wheel1,
                    wheel1.Position,
                    Vector2.UnitY,
                    maxMotorTorque: 20,
                    enableMotor: true,
                    frequency: _hz,
                    dampingRatio: Zeta);
                _spring1 = spring1.Id;

                var spring2 = Manager.AddWheelJoint(
                    car,
                    wheel2,
                    wheel2.Position,
                    Vector2.UnitY,
                    maxMotorTorque: 10,
                    frequency: _hz,
                    dampingRatio: Zeta);
                _spring2 = spring2.Id;
            }
        }

        public override void OnKeyDown(Keys key)
        {
            var spring1 = (WheelJoint) Manager.GetJointById(_spring1);
            var spring2 = (WheelJoint) Manager.GetJointById(_spring2);
            switch (key)
            {
                case Keys.A:
                    spring1.MotorSpeed = Speed;
                    break;
                case Keys.S:
                    spring1.MotorSpeed = 0.0f;
                    break;
                case Keys.D:
                    spring1.MotorSpeed = -Speed;
                    break;
                case Keys.Q:
                {
                    _hz = System.Math.Max(0.0f, _hz - 1.0f);
                    spring1.SpringFrequency = _hz;
                    spring2.SpringFrequency = _hz;
                }
                    break;
                case Keys.E:
                {
                    _hz += 1.0f;
                    spring1.SpringFrequency = _hz;
                    spring2.SpringFrequency = _hz;
                }
                    break;
            }
        }

        protected override void Step()
        {
            var spring1 = (WheelJoint) Manager.GetJointById(_spring1);

            DrawString("Keys: left = a, brake = s, right = d, hz down = q, hz up = e");
            DrawString("frequency = {0} hz, damping ratio = {1}", _hz, Zeta);
            DrawString("actual speed = {0} rad/sec", spring1.JointSpeed);

            //GameInstance.ViewCenter = _car.Position;
        }
    }
}