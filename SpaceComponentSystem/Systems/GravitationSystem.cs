using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
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
        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private HashSet<int> _reusableNeighborList = new HashSet<int>();

        #endregion

        #region Logic

        protected override void UpdateComponent(long frame, Gravitation component)
        {
            // Only do something if we're attracting stuff.
            if ((component.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
            {
                // Get our position.
                var myTransform = Manager.GetComponent<Transform>(component.Entity);
                Debug.Assert(myTransform != null);

                // And the index.
                var index = (IndexSystem)Manager.GetSystem(IndexSystem.TypeId);
                Debug.Assert(index != null);

                // Then check all our neighbors.
                ICollection<int> neighbors = _reusableNeighborList;
                index.Find(myTransform.Translation, 2 << 13, ref neighbors, IndexGroupMask);
                foreach (var neighbor in neighbors)
                {
                    // If they have an enabled gravitation component...
                    var otherGravitation = Manager.GetComponent<Gravitation>(neighbor);

                    // Validation.
                    Debug.Assert((otherGravitation.GravitationType & Gravitation.GravitationTypes.Attractee) != 0, "Non-attractees must not be added to the index.");

                    // Is it enabled?
                    if (!otherGravitation.Enabled)
                    {
                        continue;
                    }

                    // Get their velocity (which is what we'll change) and position.
                    var otherVelocity = Manager.GetComponent<Velocity>(neighbor);
                    var otherTransform = Manager.GetComponent<Transform>(neighbor);

                    // We need both.
                    Debug.Assert(otherVelocity != null);
                    Debug.Assert(otherTransform != null);

                    // Get the delta vector between the two positions.
                    var delta = otherTransform.Translation - myTransform.Translation;

                    // Compute the angle between us and the other entity.
                    var distanceSquared = delta.LengthSquared();

                    // If we're near the core only pull if  the other
                    // object isn't currently accelerating.
                    const int nearDistanceSquared = 512 * 512; // We allow overriding gravity at radius 512.
                    if (distanceSquared < nearDistanceSquared)
                    {
                        var accleration = Manager.GetComponent<Acceleration>(neighbor);
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
                                var gravitation = component.Mass * otherGravitation.Mass / Math.Max(nearDistanceSquared, distanceSquared);
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
                        var gravitation = component.Mass * otherGravitation.Mass / distanceSquared;
                        var directedGravitation = delta * gravitation;

                        otherVelocity.Value.X -= directedGravitation.X;
                        otherVelocity.Value.Y -= directedGravitation.Y;
                    }
                }

                // Clear the list for the next iteration (and after the
                // iteration so we don't keep references to stuff).
                _reusableNeighborList.Clear();
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (GravitationSystem)base.NewInstance();

            copy._reusableNeighborList = new HashSet<int>();

            return copy;
        }

        #endregion
    }
}
