using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    public class CollisionSystem : AbstractComponentSystem<CollisionParameterization>
    {
        #region Fields
        
        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private CollisionParameterization _parameterization = new CollisionParameterization();

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
                // Loop through all components. Run backwards to be immune to
                // removes during the iteration.
                for (int i = Components.Count - 2; i >= 0 ; --i)
                {
                    var currentCollidable = (AbstractCollidable)Components[i];

                    // Get a list of components actually nearby.
                    HashSet<IEntity> neighbors = null;
                    var index = Manager.GetSystem<IndexSystem>();
                    if (index != null)
                    {
                        neighbors = new HashSet<IEntity>(index.GetNeighbors(currentCollidable.Entity));
                    }

                    // Loop through all other components. Only do the interval
                    // (i, #components) avoid duplicate checks (i vs j == j vs i).
                    for (int j = Components.Count - 1; j > i; --j)
                    {
                        var otherCollidable = (AbstractCollidable)Components[j];

                        // Only test if its from a different collision group.
                        if (currentCollidable.CollisionGroup == otherCollidable.CollisionGroup)
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

                    // Call update, to update components previous position.
                    currentCollidable.Update(_parameterization);
                }
            }
        }

        #endregion
    }
}
