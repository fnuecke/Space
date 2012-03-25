using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Applies gravitational force.
    /// </summary>
    public sealed class GravitationSystem : AbstractComponentSystem<Gravitation>
    {
        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private HashSet<int> _reusableNeighborList = new HashSet<int>();

        #endregion

        #region Logic

        protected override void UpdateComponent(GameTime gameTime, long frame, Gravitation component)
        {
            // Only do something if we're attracting stuff.
            if ((component.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
            {
                // Get our position.
                var myTransform = Manager.GetComponent<Transform>(component.Entity);
                if (myTransform == null)
                {
                    return;
                }
                // And the index.
                var index = Manager.GetSystem<IndexSystem>();
                if (index == null)
                {
                    return;
                }

                // Then check all our neighbors.
                foreach (var neigbor in index.
                    RangeQuery(component.Entity, 2 << 13, Gravitation.IndexGroup, _reusableNeighborList))
                {
                    // If they have an enabled gravitation component...
                    var otherGravitation = Manager.GetComponent<Gravitation>(neigbor);

#if DEBUG
                    // Validation.
                    if ((otherGravitation.GravitationType & Gravitation.GravitationTypes.Attractee) == 0)
                    {
                        throw new InvalidOperationException("Non-attractees must not be added to the index.");
                    }
#endif

                    // Is it enabled?
                    if (!otherGravitation.Enabled)
                    {
                        continue;
                    }

                    // Get their velocity (which is what we'll change) and position.
                    var otherVelocity = Manager.GetComponent<Velocity>(neigbor);
                    var otherTransform = Manager.GetComponent<Transform>(neigbor);

                    // We need both.
                    if (otherVelocity != null && otherTransform != null)
                    {
                        // Get the delta vector between the two positions.
                        var delta = otherTransform.Translation - myTransform.Translation;

                        // Compute the angle between us and the other entity.
                        float distanceSquared = delta.LengthSquared();

                        // If we're near the core only pull if  the other
                        // object isn't currently accelerating.
                        const int nearDistanceSquared = 512 * 512; // We allow overriding gravity at radius 512.
                        if (distanceSquared < nearDistanceSquared)
                        {
                            var accleration = Manager.GetComponent<Acceleration>(neigbor);
                            if (accleration == null || accleration.Value == Vector2.Zero)
                            {
                                if (otherVelocity.Value.LengthSquared() < 16 && distanceSquared < 4)
                                {
                                    // Dock.
                                    var translation = myTransform.Translation;
                                    otherTransform.SetTranslation(ref translation);
                                    otherVelocity.Value = Vector2.Zero;
                                }
                                else
                                {
                                    // Adjust velocity.
                                    delta.Normalize();
                                    float gravitation = component.Mass * otherGravitation.Mass / Math.Max(nearDistanceSquared, distanceSquared);
                                    var directedGravitation = delta * gravitation;

                                    otherVelocity.Value.X -= directedGravitation.X;
                                    otherVelocity.Value.Y -= directedGravitation.Y;
                                }
                            }
                        }
                        else
                        {
                            // Adjust velocity.
                            delta.Normalize();
                            float gravitation = component.Mass * otherGravitation.Mass / distanceSquared;
                            var directedGravitation = delta * gravitation;

                            otherVelocity.Value.X -= directedGravitation.X;
                            otherVelocity.Value.Y -= directedGravitation.Y;
                        }
                    }
                }

                // Clear the list for the next iteration (and after the
                // iteration so we don't keep references to stuff).
                _reusableNeighborList.Clear();
            }
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (GravitationSystem)base.DeepCopy(into);

            if (copy != into)
            {
                copy._reusableNeighborList = new HashSet<int>();
            }

            return copy;
        }

        #endregion
    }
}
