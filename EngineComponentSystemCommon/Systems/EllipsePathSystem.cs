using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Makes an entity move along an ellipsoid path.
    /// </summary>
    public sealed class EllipsePathSystem : AbstractComponentSystem<EllipsePath>
    {
        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private List<int> _markedForDeletion = new List<int>();

        #endregion

        protected override void UpdateComponent(GameTime gameTime, long frame, EllipsePath component)
        {
            // Get the center, the position of the entity we're rotating around.
            var center = Manager.GetComponent<Transform>(component.CenterEntityId).Translation;

            // Get the angle based on the time passed.
            var t = component.PeriodOffset + MathHelper.Pi * frame / component.Period;
            var sinT = (float)Math.Sin(t);
            var cosT = (float)Math.Cos(t);

            // Compute the current position and set it.
            Manager.GetComponent<Transform>(component.Entity).SetTranslation(
                center.X + component.PrecomputedA + component.PrecomputedB * cosT - component.PrecomputedC * sinT,
                center.Y + component.PrecomputedD + component.PrecomputedE * cosT + component.PrecomputedF * sinT
                );
        }

        #region Messaging

        /// <summary>
        /// Checks for removed entities being center entities of ellipse paths,
        /// and if so removes all objects orbiting the center one.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is EntityRemoved)
            {
                if (_markedForDeletion.Count == 0)
                {
                    var entity = ((EntityRemoved)(ValueType)message).Entity;
                    MarkForDeletion(entity);
                    foreach (var entityToDelete in _markedForDeletion)
                    {
                        Manager.RemoveEntity(entityToDelete);
                    }
                    _markedForDeletion.Clear();
                }
            }
        }

        /// <summary>
        /// Recursively find entities to remove.
        /// </summary>
        /// <param name="center"></param>
        private void MarkForDeletion(int center)
        {
            foreach (var component in Components)
            {
                if (component.CenterEntityId == center)
                {
                    _markedForDeletion.Add(component.Entity);
                    MarkForDeletion(component.Entity);
                }
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity. If possible, the
        /// specified instance will be reused.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            // Get something to start with.
            var copy = (EllipsePathSystem)base.DeepCopy(into);

            if (copy != into)
            {
                copy._markedForDeletion = new List<int>();
            }

            return copy;
        }

        #endregion
    }
}
