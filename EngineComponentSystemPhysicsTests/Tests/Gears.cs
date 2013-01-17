using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Physics.Joints;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Tests.Tests
{
    internal sealed class Gears : AbstractTest
    {
        private int _joint1;
        private int _joint2;
        private int _joint3;
        private int _joint4;
        private int _joint5;

        protected override void Create()
        {
            Manager.AddEdge(new Vector2(50.0f, 0.0f), new Vector2(-50.0f, 0.0f));

            // Right construct.
            {
                const float radius1 = 1f;
                const float radius2 = 2f;

                // Small center circle.
                var center = Manager.AddCircle(
                    radius1,
                    worldPosition: new WorldPoint(10, 9),
                    density: 5).Body;

                // Attached rectangle.
                var rectangle = Manager.AddRectangle(
                    1,
                    10,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(10, 8),
                    density: 5).Body;

                // Larger, circling circle.
                var wheel = Manager.AddCircle(
                    radius2,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(10, 6),
                    density: 5).Body;

                // Rectangle rotates attached to center circle.
                var joint1 = Manager.AddRevoluteJoint(rectangle, center, center.Position);
                // Large circle rotates attached to rectangle.
                var joint2 = Manager.AddRevoluteJoint(wheel, rectangle, wheel.Position);

                // Wheel rotation is locked to rectangle rotation to make it appear
                // to rotate around center circle.
                Manager.AddGearJoint(joint1, joint2, rectangle, wheel, radius2 / radius1);
            }

            // Left construct.
            {
                const float radius1 = 1f;
                const float radius2 = 2f;

                // Small left circle.
                var smallCircle = Manager.AddCircle(
                    radius1,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(-3, 12),
                    density: 5).Body;

                // Fixed position rotation.
                var joint1 = Manager.AddRevoluteJoint(smallCircle, smallCircle.Position);
                _joint1 = joint1.Id;

                // Large middle circle.
                var largeCircle = Manager.AddCircle(
                    radius2,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(0, 12),
                    density: 5).Body;
                
                // Fixed position rotation.
                var joint2 = Manager.AddRevoluteJoint(largeCircle, largeCircle.Position);
                _joint2 = joint2.Id;

                // Right rectangle.
                var rectangle = Manager.AddRectangle(
                    1,
                    10,
                    type: Body.BodyType.Dynamic,
                    worldPosition: new WorldPoint(2.5f, 12),
                    density: 5).Body;

                // Allow rectangle to only move up and down.
                var joint3 = Manager.AddPrismaticJoint(
                    rectangle,
                    rectangle.Position,
                    new Vector2(0.0f, 1.0f),
                    lowerTranslation: -5,
                    upperTranslation: 5,
                    enableLimit: true);
                _joint3 = joint3.Id;

                // Fix rotation of small circle to that of large one.
                _joint4 = Manager.AddGearJoint(joint1, joint2, smallCircle, largeCircle, radius2 / radius1).Id;

                // Fix rotation of large circle to position of rectangle.
                _joint5 = Manager.AddGearJoint(joint2, joint3, largeCircle, rectangle, -1.0f / radius2).Id;
            }
        }

        protected override void Step()
        {
            var joint1 = (RevoluteJoint)Manager.GetJointById(_joint1);
            var joint2 = (RevoluteJoint)Manager.GetJointById(_joint2);
            var joint3 = (PrismaticJoint)Manager.GetJointById(_joint3);
            var joint4 = (GearJoint)Manager.GetJointById(_joint4);
            var joint5 = (GearJoint)Manager.GetJointById(_joint5);

            var ratio = joint4.Ratio;
            var value = joint1.JointAngle + ratio * joint2.JointAngle;
            DrawString("theta1 + {0} * theta2 = {1}", ratio, value);

            ratio = joint5.Ratio;
            value = joint2.JointAngle + ratio * joint3.JointTranslation;
            DrawString("theta2 + {0} * delta = {1}", ratio, value);
        }
    }
}
