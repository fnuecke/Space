﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Physics.Joints;
using Engine.ComponentSystem.Physics.Systems;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics
{
    /// <summary>
    ///     This class contains extension methods for the <see cref="IManager"/> that allow comfortable creation of joints.
    ///     Joints are deleted when one of the bodies they are attached to is removed. To remove a joint manually use the
    ///     <see cref="Joint.Destroy"/> method of the returned joint.
    /// </summary>
    public static class JointFactory
    {
        #region Accessors

        /// <summary>Determines whether the specified joint is valid/exists.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="jointId">The ID of the joint.</param>
        /// <returns>
        ///     <c>true</c> if the specified joint exists in the manager's context; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasJoint(this IManager manager, int jointId)
        {
            return manager.GetSimulation().HasJoint(jointId);
        }

        /// <summary>Gets a joint by its ID.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="jointId">The joint id.</param>
        /// <returns>A reference to the joint with the specified ID.</returns>
        public static Joint GetJointById(this IManager manager, int jointId)
        {
            return manager.GetSimulation().GetJointById(jointId);
        }

        /// <summary>Gets all joints attached to the body with the specified entity ID.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="bodyId">The ID of the entity the body belongs to.</param>
        /// <returns>A list of all joints attached to that body.</returns>
        public static IEnumerable<Joint> GetJoints(this IManager manager, int bodyId)
        {
            var body = manager.GetComponent(bodyId, Body.TypeId) as Body;
            if (body == null)
            {
                throw new ArgumentException("The specified entity is not a body.", "bodyId");
            }
            return manager.GetSimulation().GetJoints(body);
        }

        /// <summary>Gets all joints attached to the specified body.</summary>
        /// <param name="manager">The manager to check in.</param>
        /// <param name="body">The body to check for.</param>
        /// <returns>A list of all joints attached to that body.</returns>
        public static IEnumerable<Joint> GetJoints(this IManager manager, Body body)
        {
            return manager.GetSimulation().GetJoints(body);
        }

        /// <summary>Removes the specified joint from the simulation.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="joint">The joint to remove.</param>
        public static void RemoveJoint(this IManager manager, Joint joint)
        {
            joint.Destroy();
        }

        /// <summary>Removes the joint with the specified id from the simulation.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="jointId">The joint id.</param>
        public static void RemoveJoint(this IManager manager, int jointId)
        {
            manager.GetJointById(jointId).Destroy();
        }

        #endregion

        #region Joint creation

        /// <summary>
        ///     Adds a distance joint. A distance joint constrains two points on two bodies to remain at a fixed distance from
        ///     each other. You can view this as a massless, rigid rod.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchorA">The anchor on the first body, in world coordinates.</param>
        /// <param name="anchorB">The anchor on the second body, in world coordinates.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. A value of 0 disables softness.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        /// <remarks>Do not use a zero or short length.</remarks>
        public static DistanceJoint AddDistanceJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchorA,
            WorldPoint anchorB,
            float frequency = 0,
            float dampingRatio = 0,
            bool collideConnected = false)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            if (WorldPoint.DistanceSquared(anchorA, anchorB) < Settings.LinearSlop * Settings.LinearSlop)
            {
                throw new ArgumentException("Points are too close together.");
            }

            var joint = (DistanceJoint) manager.GetSimulation()
                                               .CreateJoint(Joint.JointType.Distance, bodyA, bodyB, collideConnected);

            joint.Initialize(anchorA, anchorB, frequency, dampingRatio);

            return joint;
        }

        /// <summary>
        ///     Adds a distance joint. A distance joint constrains two points on two bodies to remain at a fixed distance from each
        ///     other. You can view this as a massless, rigid rod.
        ///     <para/>
        ///     This overload attaches a body to a fixed point in the world.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body.</param>
        /// <param name="anchorWorld">The anchor in the world, in world coordinates.</param>
        /// <param name="anchorBody">The anchor on the body, in world coordinates.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. A value of 0 disables softness.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        /// <remarks>Do not use a zero or short length.</remarks>
        public static DistanceJoint AddDistanceJoint(
            this IManager manager,
            Body body,
            WorldPoint anchorWorld,
            WorldPoint anchorBody,
            float frequency = 0,
            float dampingRatio = 0,
            bool collideConnected = false)
        {
            return manager.AddDistanceJoint(
                body, manager.GetSimulation().FixPoint, anchorWorld, anchorBody, frequency, dampingRatio, collideConnected);
        }

        /// <summary>
        ///     Adds a revolute joint. A revolute joint constrains two bodies to share a common point while they are free to
        ///     rotate about the point. The relative rotation about the shared point is the joint angle. You can limit the relative
        ///     rotation with a joint limit that specifies a lower and upper angle. You can use a motor to drive the relative
        ///     rotation about the shared point. A maximum motor torque is provided so that infinite forces are not generated.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchor">The anchor point in world coordinates.</param>
        /// <param name="lowerAngle">The lower angle.</param>
        /// <param name="upperAngle">The upper angle.</param>
        /// <param name="maxMotorTorque">The maximum motor torque.</param>
        /// <param name="motorSpeed">The motor speed.</param>
        /// <param name="enableLimit">Whether to enable the lower and upper angle limits.</param>
        /// <param name="enableMotor">Whether to enable the motor.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static RevoluteJoint AddRevoluteJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchor,
            float lowerAngle = 0,
            float upperAngle = 0,
            float maxMotorTorque = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false,
            bool collideConnected = false)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (RevoluteJoint) manager.GetSimulation()
                                               .CreateJoint(Joint.JointType.Revolute, bodyA, bodyB, collideConnected);

            joint.Initialize(anchor, lowerAngle, upperAngle, maxMotorTorque, motorSpeed, enableLimit, enableMotor);

            return joint;
        }
        
        /// <summary>
        ///     Adds a revolute joint. A revolute joint constrains two bodies to share a common point while they are free to
        ///     rotate about the point. The relative rotation about the shared point is the joint angle. You can limit the relative
        ///     rotation with a joint limit that specifies a lower and upper angle. You can use a motor to drive the relative
        ///     rotation about the shared point. A maximum motor torque is provided so that infinite forces are not generated.
        ///     <para/>
        ///     This overload attaches a body to a fixed point in the world.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body.</param>
        /// <param name="anchor">The anchor point in world coordinates.</param>
        /// <param name="lowerAngle">The lower angle.</param>
        /// <param name="upperAngle">The upper angle.</param>
        /// <param name="maxMotorTorque">The maximum motor torque.</param>
        /// <param name="motorSpeed">The motor speed.</param>
        /// <param name="enableLimit">Whether to enable the lower and upper angle limits.</param>
        /// <param name="enableMotor">Whether to enable the motor.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static RevoluteJoint AddRevoluteJoint(
            this IManager manager,
            Body body,
            WorldPoint anchor,
            float lowerAngle = 0,
            float upperAngle = 0,
            float maxMotorTorque = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false,
            bool collideConnected = false)
        {
            return manager.AddRevoluteJoint(
                manager.GetSimulation().FixPoint,
                body,
                anchor,
                lowerAngle,
                upperAngle,
                maxMotorTorque,
                motorSpeed,
                enableLimit,
                enableMotor,
                collideConnected);
        }

        /// <summary>
        ///     Adds a prismatic joint. This joint provides one degree of freedom: translation along an axis fixed in bodyA.
        ///     Relative rotation is prevented. You can use a joint limit to restrict the range of motion and a joint motor to
        ///     drive the motion or to model joint friction.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchor">The anchor in world coordinates.</param>
        /// <param name="axis">The axis as a world vector.</param>
        /// <param name="lowerTranslation">The lower translation limit.</param>
        /// <param name="upperTranslation">The upper translation limit.</param>
        /// <param name="maxMotorForce">The maximum motor force.</param>
        /// <param name="motorSpeed">The motor speed.</param>
        /// <param name="enableLimit">Whether to enable the translation limits.</param>
        /// <param name="enableMotor">Whether to enable the motor.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static PrismaticJoint AddPrismaticJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchor,
            Vector2 axis,
            float lowerTranslation = 0,
            float upperTranslation = 0,
            float maxMotorForce = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false,
            bool collideConnected = false)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (PrismaticJoint) manager.GetSimulation()
                                                .CreateJoint(Joint.JointType.Prismatic, bodyA, bodyB, collideConnected);

            joint.Initialize(
                anchor,
                axis,
                lowerTranslation,
                upperTranslation,
                maxMotorForce,
                motorSpeed,
                enableLimit,
                enableMotor);

            return joint;
        }
        
        /// <summary>
        ///     Adds a prismatic joint. This joint provides one degree of freedom: translation along an axis fixed in bodyA.
        ///     Relative rotation is prevented. You can use a joint limit to restrict the range of motion and a joint motor to
        ///     drive the motion or to model joint friction.
        ///     <para/>
        ///     This overload attaches a body to a fixed point in the world.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body.</param>
        /// <param name="anchor">The anchor in world coordinates.</param>
        /// <param name="axis">The axis as a world vector.</param>
        /// <param name="lowerTranslation">The lower translation limit.</param>
        /// <param name="upperTranslation">The upper translation limit.</param>
        /// <param name="maxMotorForce">The maximum motor force.</param>
        /// <param name="motorSpeed">The motor speed.</param>
        /// <param name="enableLimit">Whether to enable the translation limits.</param>
        /// <param name="enableMotor">Whether to enable the motor.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static PrismaticJoint AddPrismaticJoint(
            this IManager manager,
            Body body,
            WorldPoint anchor,
            Vector2 axis,
            float lowerTranslation = 0,
            float upperTranslation = 0,
            float maxMotorForce = 0,
            float motorSpeed = 0,
            bool enableLimit = false,
            bool enableMotor = false,
            bool collideConnected = false)
        {
            return manager.AddPrismaticJoint(
                manager.GetSimulation().FixPoint,
                body,
                anchor,
                axis,
                lowerTranslation,
                upperTranslation,
                maxMotorForce,
                motorSpeed,
                enableLimit,
                enableMotor,
                collideConnected);
        }

        /// <summary>
        ///     Adds a pulley joint. The pulley joint is connected to two bodies and two fixed ground points. The pulley supports a
        ///     ratio such that
        ///     <c>length1 + ratio * length2 &lt;= constant</c>.
        ///     <para>Thus, the force transmitted is scaled by the ratio.</para>
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="groundAnchorA">The first ground anchor, in world coordinates.</param>
        /// <param name="groundAnchorB">The second ground anchor, in world coordinates.</param>
        /// <param name="anchorA">The anchor for the first body, in world coordinates.</param>
        /// <param name="anchorB">The anchor for the second body, in world coordinates.</param>
        /// <param name="ratio">The transmission ratio.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        /// <remarks>
        ///     Warning: the pulley joint can get a bit squirrelly by itself. They often work better when combined with
        ///     prismatic joints. You should also cover the the anchor points with static shapes to prevent one side from going to
        ///     zero length.
        /// </remarks>
        public static PulleyJoint AddPulleyJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint groundAnchorA,
            WorldPoint groundAnchorB,
            WorldPoint anchorA,
            WorldPoint anchorB,
            float ratio = 1,
            bool collideConnected = true)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (PulleyJoint) manager.GetSimulation()
                                             .CreateJoint(Joint.JointType.Pulley, bodyA, bodyB, collideConnected);

            joint.Initialize(groundAnchorA, groundAnchorB, anchorA, anchorB, ratio);

            return joint;
        }

        /// <summary>
        ///     Adds a gear joint. A gear joint is used to connect two joints together. Either joint can be a revolute or prismatic
        ///     joint. You specify a gear ratio to bind the motions together:
        ///     <c>coordinate1 + ratio * coordinate2 = constant</c>
        ///     <para>The ratio can be negative or positive.</para>
        ///     <para>
        ///         If one joint is a revolute joint and the other joint is a prismatic joint, then the ratio will have units of
        ///         length or units of <c>1/length</c>.
        ///     </para>
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="jointA">The first joint.</param>
        /// <param name="jointB">The second joint.</param>
        /// <param name="bodyA">The relevant body of the first joint.</param>
        /// <param name="bodyB">The relevant body of the second joint.</param>
        /// <param name="ratio">The ratio.</param>
        /// <returns></returns>
        /// <remarks>
        ///     Unlike in Box2D, a gear joint is automatically destroyed if either of the joints it is attached to is
        ///     destroyed. This also means that is destroyed if any involved body is destroyed, because joints are automatically
        ///     removed if one of the bodies they are attached to is destroyed.
        /// </remarks>
        public static GearJoint AddGearJoint(this IManager manager, Joint jointA, Joint jointB, Body bodyA, Body bodyB, float ratio = 1)
        {
            if (jointA == null)
            {
                throw new ArgumentNullException("jointA");
            }
            if (jointA.Type != Joint.JointType.Revolute && jointA.Type != Joint.JointType.Prismatic)
            {
                throw new ArgumentException("Gear joints must be attached to revolute or prismatic joints.", "jointA");
            }
            if (jointB == null)
            {
                throw new ArgumentNullException("jointB");
            }
            if (jointB.Type != Joint.JointType.Revolute && jointB.Type != Joint.JointType.Prismatic)
            {
                throw new ArgumentException("Gear joints must be attached to revolute or prismatic joints.", "jointB");
            }
            if (jointA == jointB)
            {
                throw new ArgumentException("Gear joints must attach to two different joints.", "jointA");
            }
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyA != jointA.BodyA && bodyA != jointA.BodyB)
            {
                throw new ArgumentException("First joint must be attached to first body.", "bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyB != jointB.BodyA && bodyB != jointB.BodyB)
            {
                throw new ArgumentException("Second joint must be attached to second body.", "bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must be attached to two different bodies.", "bodyA");
            }

            return manager.GetSimulation().CreateGearJoint(jointA, jointB, bodyA, bodyB, ratio);
        }

        /// <summary>
        ///     Adds a new mouse joint. A mouse joint is used to make a point on a body track a specified world point. This a
        ///     soft constraint with a maximum force. This allows the constraint to stretch and without applying huge forces.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body to drag.</param>
        /// <param name="target">The initial world target point. This is assumed to coincide with the body anchor initially.</param>
        /// <param name="maxForce">
        ///     The maximum constraint force that can be exerted to move the candidate body. Usually you will
        ///     express as some multiple of the weight (multiplier * mass * gravity).
        /// </param>
        /// <param name="frequency">The response speed in Hz.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <returns>The created joint.</returns>
        public static MouseJoint AddMouseJoint(
            this IManager manager,
            Body body,
            WorldPoint target,
            float maxForce = 0,
            float frequency = 5,
            float dampingRatio = 0.7f)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var joint = (MouseJoint) manager.GetSimulation()
                                            .CreateJoint(Joint.JointType.Mouse, bodyB: body);

            joint.Initialize(target, maxForce, frequency, dampingRatio);

            return joint;
        }

        /// <summary>
        ///     Adds a wheel joint. This joint provides two degrees of freedom: translation along an axis fixed in the first
        ///     body and rotation in the plane. You can use a joint limit to restrict the range of motion and a joint motor to
        ///     drive the rotation or to model rotational friction. This joint is designed for vehicle suspensions.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body (the wheel).</param>
        /// <param name="bodyB">The second body (the car).</param>
        /// <param name="anchor">The anchor on the first body in world coordinates.</param>
        /// <param name="axis">The connection axis in world space.</param>
        /// <param name="frequency">The suspension frequency, zero indicates no suspension.</param>
        /// <param name="dampingRatio">The suspension damping ratio, one indicates critical damping.</param>
        /// <param name="maxMotorTorque">The maximum motor torque, usually in N-m.</param>
        /// <param name="motorSpeed">The desired motor speed in radians per second.</param>
        /// <param name="enableMotor">Whether to initially enable the motor..</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static WheelJoint AddWheelJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchor,
            Vector2 axis,
            float frequency = 2,
            float dampingRatio = 0.7f,
            float maxMotorTorque = 0,
            float motorSpeed = 0,
            bool enableMotor = false,
            bool collideConnected = false)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (WheelJoint) manager.GetSimulation()
                                            .CreateJoint(Joint.JointType.Wheel, bodyA, bodyB, collideConnected);

            joint.Initialize(anchor, axis, frequency, dampingRatio, maxMotorTorque, motorSpeed, enableMotor);

            return joint;
        }

        /// <summary>
        ///     Adds a weld joint. A weld joint essentially glues two bodies together. A weld joint may distort somewhat
        ///     because the island constraint solver is approximate.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchor">The anchor to weld the bodies at in world coordinates.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. Rotation only. Disable softness with a value of 0.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <returns>The created joint.</returns>
        public static WeldJoint AddWeldJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchor,
            float frequency,
            float dampingRatio)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (WeldJoint) manager.GetSimulation()
                                           .CreateJoint(Joint.JointType.Weld, bodyA, bodyB, false);

            joint.Initialize(anchor, frequency, dampingRatio);

            return joint;
        }
        
        /// <summary>
        ///     Adds a weld joint. A weld joint essentially glues two bodies together. A weld joint may distort somewhat
        ///     because the island constraint solver is approximate.
        ///     <para/>
        ///     This overload attaches a body to a fixed point in the world.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body.</param>
        /// <param name="anchor">The anchor to weld the body at in world coordinates.</param>
        /// <param name="frequency">The mass-spring-damper frequency in Hertz. Rotation only. Disable softness with a value of 0.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <returns>The created joint.</returns>
        public static WeldJoint AddWeldJoint(
            this IManager manager,
            Body body,
            WorldPoint anchor,
            float frequency,
            float dampingRatio)
        {
            return manager.AddWeldJoint(manager.GetSimulation().FixPoint, body, anchor, frequency, dampingRatio);
        }

        /// <summary>
        ///     Adds a weld joint. A rope joint enforces a maximum distance between two points on two bodies. It has no other
        ///     effect.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchorA">The position at which to attach the rope to the first body, in world coordinates.</param>
        /// <param name="anchorB">The position at which to attach the rope to the second body, in world coordinates.</param>
        /// <param name="length">The maximum length of the rope.</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        /// <remarks>
        ///     Changing the maximum length during the simulation would result in non-physical behavior, thus it is not
        ///     allowed. A model that would allow you to dynamically modify the length would have some sponginess, so Erin chose
        ///     not to implement it that way. See b2DistanceJoint if you want to dynamically control length.
        /// </remarks>
        public static RopeJoint AddRopeJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            WorldPoint anchorA,
            WorldPoint anchorB,
            float length,
            bool collideConnected)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }
            if (length < Settings.LinearSlop)
            {
                throw new ArgumentException("Length is too short.", "length");
            }

            var joint = (RopeJoint) manager.GetSimulation()
                                           .CreateJoint(Joint.JointType.Rope, bodyA, bodyB, collideConnected);

            joint.Initialize(anchorA, anchorB, length);

            return joint;
        }

        /// <summary>
        ///     Adds a motor joint. A motor joint is used to control the relative motion between two bodies. A typical usage
        ///     is to control the movement of a dynamic body with respect to the ground.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="maxForce">The maximum friction force in N.</param>
        /// <param name="maxTorque">The maximum friction torque in N*m.</param>
        /// <param name="correctionFactor">The position correction factor in the range [0,1].</param>
        /// <param name="collideConnected">Whether the two bodies still collide.</param>
        /// <returns>The created joint.</returns>
        public static MotorJoint AddMotorJoint(
            this IManager manager,
            Body bodyA,
            Body bodyB,
            float maxForce = 1.0f,
            float maxTorque = 1.0f,
            float correctionFactor = 0.3f,
            bool collideConnected = false)
        {
            if (bodyA == null)
            {
                throw new ArgumentNullException("bodyA");
            }
            if (bodyB == null)
            {
                throw new ArgumentNullException("bodyB");
            }
            if (bodyA == bodyB)
            {
                throw new ArgumentException("Joints must attach to two different bodies.", "bodyA");
            }

            var joint = (MotorJoint) manager.GetSimulation()
                                            .CreateJoint(Joint.JointType.Motor, bodyA, bodyB, collideConnected);

            joint.Initialize(maxForce, maxTorque, correctionFactor);

            return joint;
        }

        #endregion

        #region Joint management

        /// <summary>Gets the simulation for the specified manager.</summary>
        private static PhysicsSystem GetSimulation(this IManager manager)
        {
            return manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
        }

        #endregion
    }
}