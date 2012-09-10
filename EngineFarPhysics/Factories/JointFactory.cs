using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Factories
{
    /// <summary>
    /// An easy to use factory for using joints.
    /// </summary>
    public static class JointFactory
    {
        #region Revolute Joint

        /// <summary>
        /// Creates a revolute joint.
        /// </summary>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="localAnchorB">The anchor of bodyB in local coordinates</param>
        /// <returns></returns>
        public static RevoluteJoint CreateRevoluteJoint(Body bodyA, Body bodyB, Vector2 localAnchorB)
        {
            Vector2 localanchorA = bodyA.GetLocalPoint(bodyB.GetWorldPoint(localAnchorB));
            RevoluteJoint joint = new RevoluteJoint(bodyA, bodyB, localanchorA, localAnchorB);
            return joint;
        }

        /// <summary>
        /// Creates a revolute joint and adds it to the world
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns></returns>
        public static RevoluteJoint CreateRevoluteJoint(World world, Body bodyA, Body bodyB, Vector2 anchor)
        {
            RevoluteJoint joint = CreateRevoluteJoint(bodyA, bodyB, anchor);
            world.AddJoint(joint);
            return joint;
        }

        /// <summary>
        /// Creates the fixed revolute joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="body">The body.</param>
        /// <param name="bodyAnchor">The body anchor.</param>
        /// <param name="worldAnchor">The world anchor.</param>
        /// <returns></returns>
        public static FixedRevoluteJoint CreateFixedRevoluteJoint(World world, Body body, Vector2 bodyAnchor,
                                                                  WorldVector2 worldAnchor)
        {
            FixedRevoluteJoint fixedRevoluteJoint = new FixedRevoluteJoint(body, bodyAnchor, worldAnchor);
            world.AddJoint(fixedRevoluteJoint);
            return fixedRevoluteJoint;
        }

        #endregion

        #region Weld Joint

        /// <summary>
        /// Creates a weld joint
        /// </summary>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="worldAnchor">World space coordinates of weld joint</param>
        /// <returns></returns>
        public static WeldJoint CreateWeldJoint(Body bodyA, Body bodyB, WorldVector2 worldAnchor)
        {
            WeldJoint joint = new WeldJoint(bodyA, bodyB, bodyA.GetLocalPoint(worldAnchor),
                                            bodyB.GetLocalPoint(worldAnchor));
            return joint;
        }

        /// <summary>
        /// Creates a weld joint and adds it to the world
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="worldAnchor">World space coordinates of weld joint</param>
        /// <returns></returns>
        public static WeldJoint CreateWeldJoint(World world, Body bodyA, Body bodyB, WorldVector2 worldAnchor)
        {
            WeldJoint joint = CreateWeldJoint(bodyA, bodyB, worldAnchor);
            world.AddJoint(joint);
            return joint;
        }

        /// <summary>
        /// Creates the weld joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="localAnchorA">The local anchor A.</param>
        /// <param name="localAnchorB">The local anchor B.</param>
        /// <returns></returns>
        public static WeldJoint CreateWeldJoint(World world, Body bodyA, Body bodyB, Vector2 localAnchorA,
                                                Vector2 localAnchorB)
        {
            WeldJoint weldJoint = new WeldJoint(bodyA, bodyB, localAnchorA, localAnchorB);
            world.AddJoint(weldJoint);
            return weldJoint;
        }

        #endregion

        #region Prismatic Joint

        /// <summary>
        /// Creates a prsimatic joint
        /// </summary>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="localanchorB">The localanchor B.</param>
        /// <param name="axis">The axis.</param>
        /// <returns></returns>
        public static PrismaticJoint CreatePrismaticJoint(Body bodyA, Body bodyB, Vector2 localanchorB, Vector2 axis)
        {
            Vector2 localanchorA = bodyA.GetLocalPoint(bodyB.GetWorldPoint(localanchorB));
            PrismaticJoint joint = new PrismaticJoint(bodyA, bodyB, localanchorA, localanchorB, axis);
            return joint;
        }

        /// <summary>
        /// Creates a prismatic joint and adds it to the world
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="localanchorB">The localanchor B.</param>
        /// <param name="axis">The axis.</param>
        /// <returns></returns>
        public static PrismaticJoint CreatePrismaticJoint(World world, Body bodyA, Body bodyB, Vector2 localanchorB,
                                                          Vector2 axis)
        {
            PrismaticJoint joint = CreatePrismaticJoint(bodyA, bodyB, localanchorB, axis);
            world.AddJoint(joint);
            return joint;
        }

        /// <summary>
        /// Creates the fixed prismatic joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="body">The body.</param>
        /// <param name="worldAnchor">The world anchor.</param>
        /// <param name="axis">The axis.</param>
        /// <returns></returns>
        public static FixedPrismaticJoint CreateFixedPrismaticJoint(World world, Body body, WorldVector2 worldAnchor,
                                                                    Vector2 axis)
        {
            FixedPrismaticJoint joint = new FixedPrismaticJoint(body, worldAnchor, axis);
            world.AddJoint(joint);
            return joint;
        }

        #endregion

        #region Line Joint

        /// <summary>
        /// Creates a line joint
        /// </summary>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="worldAnchor">The world anchor.</param>
        /// <param name="axis">The axis.</param>
        /// <returns></returns>
        public static LineJoint CreateLineJoint(Body bodyA, Body bodyB, WorldVector2 worldAnchor, Vector2 axis)
        {
            LineJoint joint = new LineJoint(bodyA, bodyB, worldAnchor, axis);
            return joint;
        }

        /// <summary>
        /// Creates a line joint and adds it to the world
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="worldAnchor">The world anchor.</param>
        /// <param name="axis">The axis.</param>
        /// <returns></returns>
        public static LineJoint CreateLineJoint(World world, Body bodyA, Body bodyB, WorldVector2 worldAnchor, Vector2 axis)
        {
            LineJoint joint = CreateLineJoint(bodyA, bodyB, worldAnchor, axis);
            world.AddJoint(joint);
            return joint;
        }

        #endregion

        #region Angle Joint

        /// <summary>
        /// Creates an angle joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <returns></returns>
        public static AngleJoint CreateAngleJoint(World world, Body bodyA, Body bodyB)
        {
            AngleJoint angleJoint = new AngleJoint(bodyA, bodyB);
            world.AddJoint(angleJoint);

            return angleJoint;
        }

        /// <summary>
        /// Creates a fixed angle joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        public static FixedAngleJoint CreateFixedAngleJoint(World world, Body body)
        {
            FixedAngleJoint angleJoint = new FixedAngleJoint(body);
            world.AddJoint(angleJoint);

            return angleJoint;
        }

        #endregion

        #region Distance Joint

        /// <summary>
        /// Creates the distance joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="anchorA">The anchor A.</param>
        /// <param name="anchorB">The anchor B.</param>
        /// <returns></returns>
        public static DistanceJoint CreateDistanceJoint(World world, Body bodyA, Body bodyB, Vector2 anchorA,
                                                        Vector2 anchorB)
        {
            DistanceJoint distanceJoint = new DistanceJoint(bodyA, bodyB, anchorA, anchorB);
            world.AddJoint(distanceJoint);
            return distanceJoint;
        }

        /// <summary>
        /// Creates the fixed distance joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="body">The body.</param>
        /// <param name="localAnchor">The local anchor.</param>
        /// <param name="worldAnchor">The world anchor.</param>
        /// <returns></returns>
        public static FixedDistanceJoint CreateFixedDistanceJoint(World world, Body body, Vector2 localAnchor,
                                                                  WorldVector2 worldAnchor)
        {
            FixedDistanceJoint distanceJoint = new FixedDistanceJoint(body, localAnchor, worldAnchor);
            world.AddJoint(distanceJoint);
            return distanceJoint;
        }

        #endregion

        #region Friction Joint

        /// <summary>
        /// Creates the friction joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="anchorA">The anchor A.</param>
        /// <param name="anchorB">The anchor B.</param>
        /// <returns></returns>
        public static FrictionJoint CreateFrictionJoint(World world, Body bodyA, Body bodyB, Vector2 anchorA,
                                                        Vector2 anchorB)
        {
            FrictionJoint frictionJoint = new FrictionJoint(bodyA, bodyB, anchorA, anchorB);
            world.AddJoint(frictionJoint);
            return frictionJoint;
        }

        /// <summary>
        /// Creates the fixed friction joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="body">The body.</param>
        /// <param name="bodyAnchor">The body anchor.</param>
        /// <returns></returns>
        public static FixedFrictionJoint CreateFixedFrictionJoint(World world, Body body, Vector2 bodyAnchor)
        {
            FixedFrictionJoint frictionJoint = new FixedFrictionJoint(body, bodyAnchor);
            world.AddJoint(frictionJoint);
            return frictionJoint;
        }

        #endregion

        #region Gear Joint

        /// <summary>
        /// Creates the gear joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="jointA">The joint A.</param>
        /// <param name="jointB">The joint B.</param>
        /// <param name="ratio">The ratio.</param>
        /// <returns></returns>
        public static GearJoint CreateGearJoint(World world, Joint jointA, Joint jointB, float ratio)
        {
            GearJoint gearJoint = new GearJoint(jointA, jointB, ratio);
            world.AddJoint(gearJoint);
            return gearJoint;
        }

        #endregion

        #region Pulley Joint

        /// <summary>
        /// Creates the pulley joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="groundAnchorA">The ground anchor A.</param>
        /// <param name="groundAnchorB">The ground anchor B.</param>
        /// <param name="anchorA">The anchor A.</param>
        /// <param name="anchorB">The anchor B.</param>
        /// <param name="ratio">The ratio.</param>
        /// <returns></returns>
        public static PulleyJoint CreatePulleyJoint(World world, Body bodyA, Body bodyB, WorldVector2 groundAnchorA,
                                                    WorldVector2 groundAnchorB, Vector2 anchorA, Vector2 anchorB, float ratio)
        {
            PulleyJoint pulleyJoint = new PulleyJoint(bodyA, bodyB, groundAnchorA, groundAnchorB, anchorA, anchorB,
                                                      ratio);
            world.AddJoint(pulleyJoint);
            return pulleyJoint;
        }

        #endregion

        #region Slider Joint

        /// <summary>
        /// Creates the slider joint.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="bodyA">The body A.</param>
        /// <param name="bodyB">The body B.</param>
        /// <param name="anchorA">The anchor A.</param>
        /// <param name="anchorB">The anchor B.</param>
        /// <param name="minLength">Length of the min.</param>
        /// <param name="maxLength">Length of the max.</param>
        /// <returns></returns>
        public static SliderJoint CreateSliderJoint(World world, Body bodyA, Body bodyB, Vector2 anchorA,
                                                    Vector2 anchorB, float minLength, float maxLength)
        {
            SliderJoint sliderJoint = new SliderJoint(bodyA, bodyB, anchorA, anchorB, minLength, maxLength);
            world.AddJoint(sliderJoint);
            return sliderJoint;
        }

        #endregion
    }
}