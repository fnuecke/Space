using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Messages;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of components that support collision (anything
    /// that extends <c>AbstractCollidable</c>). It fetches the components
    /// neighbors and checks their collision groups, keeping the number of
    /// actual collision checks that have to be performed low.
    /// </summary>
    public sealed class CollisionSystem : AbstractComponentSystem<Collidable>
    {
        #region Constants

        /// <summary>
        /// Start using indexes after the collision index.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The buffer area to use when querying, to take fast moving objects
        /// into account.
        /// </summary>
        private int _bufferArea;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private List<int> _reusableNeighborList = new List<int>();

        #endregion

        #region Constructor

        public CollisionSystem(int bufferArea)
        {
            // Use a range a little larger than the max collidable size, to
            // account for fast moving objects (sweep test).
            _bufferArea = bufferArea;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Does a normal update but clears the list of performed checks
        /// afterwards..
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        public override void Update(GameTime gameTime, long frame)
        {
            var index = Manager.GetSystem<IndexSystem>();
            ICollection<int> neighbors = _reusableNeighborList;

            UpdatingComponents.AddRange(Components);
            for (var i = 0; i < UpdatingComponents.Count; ++i)
            {
                var component1 = UpdatingComponents[i];

                // Skip disabled components.
                if (!component1.Enabled)
                {
                    continue;
                }

                // Prepare the collision message.
                Collision message;
                message.FirstEntity = component1.Entity;

                // Get the components' bounds and look for nearby elements.
                var bounds = component1.ComputeBounds();
                var translation = Manager.GetComponent<Transform>(component1.Entity).Translation;
                bounds.X = (int)translation.X - bounds.Width / 2;
                bounds.Y = (int)translation.Y - bounds.Height / 2;
                bounds.Inflate(_bufferArea, _bufferArea);
                index.Find(ref bounds, ref neighbors, IndexGroupMask);

                // Iterate over the remaining collidables.
                for (var j = i + 1; j < UpdatingComponents.Count && component1.Enabled; ++j)
                {
                    var component2 = UpdatingComponents[j];

                    // Skip disabled components.
                    if (!component2.Enabled)
                    {
                        continue;
                    }

                    // Only test if its from a different collision group.
                    if ((component1.CollisionGroups & component2.CollisionGroups) != 0)
                    {
                        continue;
                    }

                    // Don't bother testing unless the component is nearby.
                    if (!neighbors.Contains(component2.Entity))
                    {
                        continue;
                    }

                    // Test for collision.
                    if (!component1.Intersects(component2))
                    {
                        continue;
                    }

                    // If there is one, let both parties know.
                    message.SecondEntity = component2.Entity;
                    Manager.SendMessage(ref message);
                }

                // Clear list for the next run.
                _reusableNeighborList.Clear();
            }

            // Clear list for the next update.
            UpdatingComponents.Clear();
        }

        /// <summary>
        /// Update the previous position to the current one when adding a component.
        /// </summary>
        /// <param name="component">The added component.</param>
        protected override void OnComponentAdded(Collidable component)
        {
            var transform = Manager.GetComponent<Transform>(component.Entity);
            if (transform != null)
            {
                component.PreviousPosition = transform.Translation;
            }
        }

        /// <summary>
        /// Update the previous position when a collidable component changes its position.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is TranslationChanged)
            {
                var changedMessage = (TranslationChanged)(ValueType)message;

                var collidable = Manager.GetComponent<Collidable>(changedMessage.Entity);
                if (collidable == null)
                {
                    return;
                }

                collidable.PreviousPosition = changedMessage.PreviousPosition;
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
        public override AbstractSystem DeepCopy()
        {
            var copy = (CollisionSystem)base.DeepCopy();

            copy._reusableNeighborList = new List<int>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (CollisionSystem)base.DeepCopy(into);

            copy._bufferArea = _bufferArea;

            return copy;
        }

        #endregion
    }
}
