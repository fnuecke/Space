using System;
using Engine.ComponentSystem;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Physics.Systems;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics
{
    /// <summary>
    /// This class contains extension methods for the <see cref="IManager"/> that
    /// allow comfortable creation of joints. Joints are deleted when one of the
    /// bodies they are attached to is removed. To remove a joint manually use the
    /// <see cref="Joint.Destroy"/> method of the returned joint.
    /// </summary>
    public static class JointFactory
    {
        /// <summary>
        /// Creates a new mouse joint. A mouse joint is used to make a point
        /// on a body track a specified world point. This a soft constraint
        /// with a maximum force. This allows the constraint to stretch and
        /// without applying huge forces.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="body">The body to drag.</param>
        /// <param name="target">The initial world target point. This is assumed
        /// to coincide with the body anchor initially.</param>
        /// <param name="maxForce">The maximum constraint force that can be exerted
        /// to move the candidate body. Usually you will express as some multiple
        /// of the weight (multiplier * mass * gravity).</param>
        /// <param name="frequency">The response speed.</param>
        /// <param name="dampingRatio">The damping ratio. 0 = no damping, 1 = critical damping.</param>
        /// <returns>The created joint.</returns>
        public static MouseJoint AddMouseJoint(this IManager manager, Body body, WorldPoint target, float maxForce = 0, float frequency = 5, float dampingRatio = 0.7f)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var joint = manager.GetSimulation().CreateJoint(Joint.JointType.Mouse, bodyB: body) as MouseJoint;
            System.Diagnostics.Debug.Assert(joint != null);

            joint.Initialize(target, maxForce, frequency, dampingRatio);

            return joint;
        }

        /// <summary>
        /// Gets the simulation for the specified manager.
        /// </summary>
        private static PhysicsSystem GetSimulation(this IManager manager)
        {
            return manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
        }
    }
}
