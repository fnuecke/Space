using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Physics.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class TheoJansen : AbstractTest
    {
        private int _motorJoint;

        /// <summary>
        /// Called when the scene should be set up (bodies and fixtures created).
        /// </summary>
        protected override void Create()
        {
            var offset = new WorldPoint(0.0f, 8.0f);
            var pivot = new WorldPoint(0.0f, 0.8f);

            // Ground
            {
                var ground = Manager.AddBody();
                Manager.AttachEdge(ground, new Vector2(-50.0f, 0.0f), new Vector2(50.0f, 0.0f));
                Manager.AttachEdge(ground, new Vector2(-50.0f, 0.0f), new Vector2(-50.0f, 10.0f));
                Manager.AttachEdge(ground, new Vector2(50.0f, 0.0f), new Vector2(50.0f, 10.0f));
            }

            // Balls
            for (var i = 0; i < 40; ++i)
            {
                Manager.AddCircle(radius: 0.25f, type: Body.BodyType.Dynamic,
                                  worldPosition: new WorldPoint(-40.0f + 2.0f * i, 0.5f),
                                  density: 1);
            }

            // Chassis
            var chassis = Manager.AddRectangle(width: 5, height: 2,
                                               type: Body.BodyType.Dynamic,
                                               worldPosition: pivot + offset,
                                               density: 1, collisionGroups: 1);

            // Wheel
            var wheel = Manager.AddCircle(radius: 1.6f,
                                          type: Body.BodyType.Dynamic,
                                          worldPosition: pivot + offset,
                                          density: 1,
                                          collisionGroups: 1);

            // Motor
            _motorJoint = Manager.AddRevoluteJoint(wheel, chassis, pivot + offset,
                                                   motorSpeed: 2.0f,
                                                   maxMotorTorque: 400,
                                                   enableMotor: true).Id;

            var wheelAnchor = pivot + new Vector2(0.0f, -0.8f);

            CreateLeg(-1.0f, wheelAnchor, offset, chassis, wheel);
            CreateLeg(1.0f, wheelAnchor, offset, chassis, wheel);

            wheel.SetTransform(wheel.Position, 120.0f * MathHelper.Pi / 180.0f);
            CreateLeg(-1.0f, wheelAnchor, offset, chassis, wheel);
            CreateLeg(1.0f, wheelAnchor, offset, chassis, wheel);

            wheel.SetTransform(wheel.Position, -120.0f * MathHelper.Pi / 180.0f);
            CreateLeg(-1.0f, wheelAnchor, offset, chassis, wheel);
            CreateLeg(1.0f, wheelAnchor, offset, chassis, wheel);
        }

        private void CreateLeg(float s, WorldPoint wheelAnchor, WorldPoint offset, Body chassis, Body wheel)
        {
            var p1 = new Vector2(5.4f * s, -6.1f);
            var p2 = new Vector2(7.2f * s, -1.2f);
            var p3 = new Vector2(4.3f * s, -1.9f);
            var p4 = new Vector2(3.1f * s, 0.8f);
            var p5 = new Vector2(6.0f * s, 1.5f);
            var p6 = new Vector2(2.5f * s, 3.7f);

            var vertices1 = new Vector2[3];
            var vertices2 = new Vector2[3];
            if (s > 0.0f)
            {
                vertices1[0] = p1;
                vertices1[1] = p2;
                vertices1[2] = p3;

                vertices2[0] = Vector2.Zero;
                vertices2[1] = p5 - p4;
                vertices2[2] = p6 - p4;
            }
            else
            {
                vertices1[0] = p1;
                vertices1[1] = p3;
                vertices1[2] = p2;

                vertices2[0] = Vector2.Zero;
                vertices2[1] = p6 - p4;
                vertices2[2] = p5 - p4;
            }

            var body1 = Manager.AddPolygon(vertices1, type: Body.BodyType.Dynamic,
                                           worldPosition: offset,
                                           density: 1, collisionGroups: 1);
            var body2 = Manager.AddPolygon(vertices2, type: Body.BodyType.Dynamic,
                                           worldPosition: p4 + offset,
                                           density: 1, collisionGroups: 1);

            body1.AngularDamping = 10.0f;
            body2.AngularDamping = 10.0f;

            // Using a soft distance constraint can reduce some jitter.
            // It also makes the structure seem a bit more fluid by
            // acting like a suspension system.
            Manager.AddDistanceJoint(body1, body2, p2 + offset, p5 + offset, frequency: 10, dampingRatio: 0.5f);
            Manager.AddDistanceJoint(body1, body2, p3 + offset, p4 + offset, frequency: 10, dampingRatio: 0.5f);
            Manager.AddDistanceJoint(body1, wheel, p3 + offset, wheelAnchor + offset, frequency: 10, dampingRatio: 0.5f);
            Manager.AddDistanceJoint(body2, wheel, p6 + offset, wheelAnchor + offset, frequency: 10, dampingRatio: 0.5f);
            Manager.AddRevoluteJoint(body2, chassis, p4 + offset);
        }

        protected override void Step()
        {
            DrawString("Keys: left = a, brake = s, right = d, toggle motor = m");
        }

        public override void OnKeyDown(Keys key)
        {
            var motorJoint = (RevoluteJoint)Manager.GetJointById(_motorJoint);
            switch (key)
            {
                case Keys.A:
                    motorJoint.MotorSpeed = -2.0f;
                    break;

                case Keys.S:
                    motorJoint.MotorSpeed = 0.0f;
                    break;

                case Keys.D:
                    motorJoint.MotorSpeed = 2.0f;
                    break;

                case Keys.M:
                    motorJoint.IsMotorEnabled = !motorJoint.IsMotorEnabled;
                    break;
            }
        }
    }
}