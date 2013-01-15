using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Applies gravitational force.</summary>
    /// <remarks>
    ///     This is hard to parallelize because multiple attractors might affect one attractee. And locking the
    ///     attractee's acceleration is not sufficient, because floating point addition leads to different results when
    ///     performed in a different order. So, the additional overhead to sort the changes is too large to justify
    ///     parallelizing it.
    /// </remarks>
    public sealed class GravitationSystem : AbstractUpdatingComponentSystem<Gravitation>
    {
        #region Constants

        /// <summary>Index group to use for gravitational computations.</summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        /// <summary>The maximum distance at which an attractor may look for attractees.</summary>
        private const float MaxGravitationDistance = 30000f;

        /// <summary>The squared velocity below which an object should be before it's docked.</summary>
        private const float DockVelocity = 16f;

        /// <summary>Squared distance to the center an object should be before it's docked.</summary>
        private const float DockDistance = 4;

        #endregion

        #region Logic

        /// <summary>
        ///     Updates the component by checking if it attracts other entities, and if yes fetching all nearby ones and
        ///     applying it's acceleration to them.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, Gravitation component)
        {
            // Only do something if we're attracting stuff.
            if ((component.GravitationType & Gravitation.GravitationTypes.Attractor) == 0)
            {
                return;
            }

            // Get our position.
            var myTransform = ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId));
            Debug.Assert(myTransform != null);

            // And the index.
            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);
            Debug.Assert(index != null);

            // Then check all our neighbors. Use new list each time because we're running
            // in parallel, so we can't really keep one on a global level.
            ISet<int> neighbors = new HashSet<int>();
            index.Find(myTransform.Position, MaxGravitationDistance, neighbors, IndexGroupMask);
            foreach (IIndexable neighbor in neighbors.Select(Manager.GetComponentById))
            {
                // If they have an enabled gravitation component...
                var otherGravitation = ((Gravitation) Manager.GetComponent(neighbor.Entity, Gravitation.TypeId));

                // Validation.
                Debug.Assert(
                    (otherGravitation.GravitationType & Gravitation.GravitationTypes.Attractee) != 0,
                    "Non-attractees must not be added to the index.");

                // Is it enabled?
                if (!otherGravitation.Enabled)
                {
                    continue;
                }

                // Get their velocity and position.
                var otherVelocity = ((Velocity) Manager.GetComponent(neighbor.Entity, Velocity.TypeId));
                var otherTransform = ((Transform) Manager.GetComponent(neighbor.Entity, Transform.TypeId));

                // We need both.
                Debug.Assert(otherVelocity != null);
                Debug.Assert(otherTransform != null);

                // Get the delta vector between the two positions.
                var delta = (Vector2) (otherTransform.Position - myTransform.Position);

                // Compute the angle between us and the other entity.
                var distanceSquared = delta.LengthSquared();

                // If we're near the core only pull if  the other
                // object isn't currently accelerating.
                const int nearDistanceSquared = 512 * 512; // We allow overriding gravity at radius 512.
                if (distanceSquared < nearDistanceSquared)
                {
                    // If it's a ship it might accelerate itself, but acceleration doesn't
                    // carry over frames, and it has to come after gravitation for stabilization,
                    // so we must check manually if the ship is accelerating.
                    var otherShipInfo = (ShipInfo) Manager.GetComponent(neighbor.Entity, ShipInfo.TypeId);
                    if (otherShipInfo != null && !otherShipInfo.IsAccelerating &&
                        otherVelocity.LinearVelocity.LengthSquared() < DockVelocity && distanceSquared < DockDistance)
                    {
                        // It's a ship that's not accelerating, and in range for docking.
                        otherTransform.Position = myTransform.Position;
                        otherVelocity.LinearVelocity = Vector2.Zero;
                    }
                    else if ((otherShipInfo == null || !otherShipInfo.IsAccelerating) && distanceSquared > 0.001f)
                        // epsilon to avoid delta.Normalize() generating NaNs
                    {
                        // Adjust acceleration with a fixed value when we're getting
                        // close. This is to avoid objects jittering around an attractor
                        // at high speeds.
                        delta.Normalize();
                        var gravitation = component.Mass * otherGravitation.Mass / nearDistanceSquared;
                        var directedGravitation = delta * gravitation / Settings.TicksPerSecond;

                        Debug.Assert(!float.IsNaN(directedGravitation.X) && !float.IsNaN(directedGravitation.Y));

                        var acceleration = (Acceleration) Manager.GetComponent(neighbor.Entity, Acceleration.TypeId);
                        acceleration.Value.X -= directedGravitation.X;
                        acceleration.Value.Y -= directedGravitation.Y;
                    }
                }
                else if (distanceSquared > 0.001f) // epsilon to avoid delta.Normalize() generating NaNs
                {
                    // Adjust acceleration.
                    delta.Normalize();
                    var gravitation = component.Mass * otherGravitation.Mass / distanceSquared;
                    var directedGravitation = delta * gravitation / Settings.TicksPerSecond;

                    Debug.Assert(!float.IsNaN(directedGravitation.X) && !float.IsNaN(directedGravitation.Y));

                    var acceleration = (Acceleration) Manager.GetComponent(neighbor.Entity, Acceleration.TypeId);
                    acceleration.Value.X -= directedGravitation.X;
                    acceleration.Value.Y -= directedGravitation.Y;
                }
            }
        }

        #endregion
    }
}