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
    public class CollisionSystem : AbstractComponentSystem<CollisionParameterization, NullParameterization>
    {
        #region Constants

        /// <summary>
        /// Start using indexes after the collision index.
        /// </summary>
        public static readonly byte FirstIndexGroup = IndexSystem.GetGroups(32);

        #endregion

        #region Fields

        /// <summary>
        /// The maximum radius any object ever used in this system can have.
        /// </summary>
        private readonly int _maxCollidableRadius;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private static CollisionParameterization _parameterization = new CollisionParameterization();

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private static readonly List<AbstractComponent> _reusableComponentList = new List<AbstractComponent>(2048);

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private static readonly List<Entity> _reusableNeighborList = new List<Entity>(64);
        
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
        public override void Update(long frame)
        {
            // Loop through all components.
            _reusableComponentList.AddRange(UpdateableComponents);
            var index = Manager.GetSystem<IndexSystem>();
            while (_reusableComponentList.Count > 0)
            {
                // Get last element from the list as our current object, then
                // remove it from the list (avoids double checks, i.e. we won't
                // do test(x,y) *and* test(y,x), only the first one.
                var currentCollidable = (AbstractCollidable)_reusableComponentList[_reusableComponentList.Count - 1];
                _reusableComponentList.RemoveAt(_reusableComponentList.Count - 1);

                // Skip disabled components.
                if (!currentCollidable.Enabled)
                {
                    continue;
                }

                // Get a list of components actually nearby.
                if (index != null)
                {
                    // Use the inverse of the collision group, i.e. get
                    // entries from all those entries where we're not in
                    // that group.
                    foreach (var neighbor in index.GetNeighbors(
                        currentCollidable.Entity, _maxCollidableRadius,
                        (ulong)(~currentCollidable.CollisionGroups) << FirstIndexGroup,
                        _reusableNeighborList))
                    {
                        // See if it's among the list of components we still
                        // need to check.
                        var otherComponent = _reusableComponentList.
                            Find(c => c.Entity.UID == neighbor.UID);
                        if (otherComponent != null)
                        {
                            TestCollision(currentCollidable, (AbstractCollidable)otherComponent);
                        }
                    }

                    // Clear the list for the next iteration (and after the
                    // iteration so we don't keep references to stuff).
                    _reusableNeighborList.Clear();
                }
                else
                {
                    // Bruteforce. Loop through all other components.
                    foreach (var otherComponent in _reusableComponentList)
                    {
                        TestCollision(currentCollidable, (AbstractCollidable)otherComponent);
                    }
                }
            }

            // _reusableComponentList.Clear() unnecessary because empty per definition of the above loop.

            // Update previous position for all collidables.
            foreach (var component in UpdateableComponents)
            {
                if (component.Enabled)
                {
                    component.Update(_parameterization);
                }
            }
        }

        /// <summary>
        /// Performs a collision check between the two given collidable
        /// components.
        /// </summary>
        /// <param name="currentCollidable">The first object.</param>
        /// <param name="otherCollidable">The second object.</param>
        private void TestCollision(AbstractCollidable currentCollidable, AbstractCollidable otherCollidable)
        {
            if (
            // Skip disabled components.
            otherCollidable.Enabled &&
            // Only test if its from a different collision group.
            (currentCollidable.CollisionGroups & otherCollidable.CollisionGroups) == 0 &&
            // Test for collision, if there is one, let both parties know.
            currentCollidable.Intersects(otherCollidable))
            {
                Collision message;
                message.OtherEntity = otherCollidable.Entity;
                currentCollidable.Entity.SendMessage(ref message);
                message.OtherEntity = currentCollidable.Entity;
                otherCollidable.Entity.SendMessage(ref message);
            }
        }

        #endregion
    }
}
