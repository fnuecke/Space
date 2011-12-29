using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of components that support collision (anything
    /// that extends <c>AbstractCollidable</c>). It fetches the components
    /// neighbors and checks their collision groups, keeping the number of
    /// actual collision checks that have to be performed low.
    /// </summary>
    public class CollisionSystem : AbstractComponentSystem<CollisionParameterization>
    {
        #region Constants

        /// <summary>
        /// Start using indexes after the collision index.
        /// </summary>
        public const int FirstIndexGroup = Gravitation.IndexGroup << 1;

        /// <summary>
        /// Last indexes we will possibly be using.
        /// </summary>
        public const int LastIndexGroup = FirstIndexGroup << 31;

        #endregion

        #region Fields

        /// <summary>
        /// The maximum radius any object ever used in this system can have.
        /// </summary>
        private readonly int _maxCollidableRadius;

        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private CollisionParameterization _parameterization = new CollisionParameterization();

        #endregion

        #region Constructor

        public CollisionSystem(int maxCollidableRadius)
        {
            // Use a range a little larger than the max collidable size, to
            // account for fast moving objects (sweep test).
            _maxCollidableRadius = maxCollidableRadius * 3;
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Tests for collisions and lets participating components know if
        /// there is one.
        /// </summary>
        /// <param name="updateType">The type of update to perform. We only do logic updates here.</param>
        /// <param name="frame">The current simulation frame.</param>
        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Loop through all components.
                var currentComponents = Components;
                var index = Manager.GetSystem<IndexSystem>();
                HashSet<IEntity> neighbors = null;
                for (int i = 0; i < currentComponents.Count; ++i)
                {
                    var currentCollidable = (AbstractCollidable)currentComponents[i];

                    // Get a list of components actually nearby.
                    if (index != null)
                    {
                        // Use the inverse of the collision group, i.e. get
                        // entries from all those entries where we're not in
                        // that group.
                        neighbors = new HashSet<IEntity>(index.GetNeighbors(
                            currentCollidable.Entity, _maxCollidableRadius,
                            (~currentCollidable.CollisionGroups) << FirstIndexGroup));
                    }

                    // Loop through all other components. Only do the interval
                    // (i, #components) avoid duplicate checks (i vs j == j vs i).
                    for (int j = i + 1; j < currentComponents.Count; ++j)
                    {
                        var otherCollidable = (AbstractCollidable)currentComponents[j];

                        // Only test if its from a different collision group.
                        if ((currentCollidable.CollisionGroups & otherCollidable.CollisionGroups) != 0)
                        {
                            continue;
                        }

                        // Only test if its in our neighbors list (if we have one).
                        if (neighbors != null && !neighbors.Contains(otherCollidable.Entity))
                        {
                            continue;
                        }

                        // Test for collision, if there is one, let both parties know.
                        if (currentCollidable.Intersects(otherCollidable))
                        {
                            currentCollidable.Entity.SendMessage(Collision.Create(otherCollidable.Entity));
                            otherCollidable.Entity.SendMessage(Collision.Create(currentCollidable.Entity));
                        }
                    }
                }

                // Update previous position for all collidables.
                foreach (var component in currentComponents)
                {
                    component.Update(_parameterization);
                }
            }
        }

        #endregion
    }
}
