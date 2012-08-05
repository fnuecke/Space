using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This system takes care of components that support collision (anything
    /// that extends <c>AbstractCollidable</c>). It fetches the components
    /// neighbors and checks their collision groups, keeping the number of
    /// actual collision checks that have to be performed low.
    /// </summary>
    public sealed class CollisionSystem : AbstractParallelComponentSystem<Collidable>
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

        #region Constructor

        public CollisionSystem(int bufferArea)
        {
            // Use a range a little larger than the max collidable size, to
            // account for fast moving objects (sweep test).
            _bufferArea = bufferArea;
        }

        #endregion

        #region Logic

        protected override void UpdateComponent(long frame, Collidable component)
        {
            // Get index and allocate neighbor result list.
            var index = (IndexSystem)Manager.GetSystem(IndexSystem.TypeId);
            ISet<int> neighbors = new HashSet<int>();

            // Get the component's bounds and look for nearby elements.
            var bounds = component.ComputeBounds();
            var translation = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).Translation;
            bounds.X = (int)translation.X - bounds.Width / 2;
            bounds.Y = (int)translation.Y - bounds.Height / 2;
            bounds.Inflate(_bufferArea, _bufferArea);
            index.Find(ref bounds, ref neighbors, IndexGroupMask);

            // If there are no neighbors, skip the rest.
            if (neighbors.Count <= 0)
            {
                return;
            }

            // Prepare the collision message.
            Collision message;
            message.FirstEntity = component.Entity;

            // Check each neighbor.
            foreach (var neighbor in neighbors)
            {
                var otherComponent = (Collidable)Manager.GetComponent(neighbor, Collidable.TypeId);

                // Skip disabled components.
                if (!otherComponent.Enabled)
                {
                    continue;
                }

                // Only test if its from a different collision group.
                if ((component.CollisionGroups & otherComponent.CollisionGroups) != 0)
                {
                    continue;
                }

                // Test for collision.
                if (!component.Intersects(otherComponent))
                {
                    continue;
                }

                // If there is one, let both parties know.
                message.SecondEntity = otherComponent.Entity;
                Manager.SendMessage(ref message);
            }
        }

        /// <summary>
        /// Update the previous position to the current one when adding a component.
        /// </summary>
        /// <param name="component">The added component.</param>
        protected override void OnComponentAdded(Collidable component)
        {
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));
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

                var collidable = ((Collidable)Manager.GetComponent(changedMessage.Entity, Collidable.TypeId));
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
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CollisionSystem)into;

            copy._bufferArea = _bufferArea;
        }

        #endregion
    }
}
