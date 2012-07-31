using System.Collections.Generic;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Base class for component systems, pre-implementing adding / removal
    /// of components.
    /// </summary>
    /// <typeparam name="TComponent">The type of component handled in this system.</typeparam>
    public abstract class AbstractUpdatingComponentSystem<TComponent> : AbstractComponentSystem<TComponent>
        where TComponent : Component
    {
        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        protected List<TComponent> UpdatingComponents = new List<TComponent>();

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>UpdateComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(long frame)
        {
            UpdatingComponents.AddRange(Components);
            for (int i = 0, j = UpdatingComponents.Count; i < j; ++i)
            {
                var component = UpdatingComponents[i];
                if (component.Enabled)
                {
                    UpdateComponent(frame, component);
                }
            }
            UpdatingComponents.Clear();
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(long frame)
        {
            for (int i = 0, j = Components.Count; i < j; ++i)
            {
                var component = Components[i];
                if (component.Enabled)
                {
                    DrawComponent(frame, component);
                }
            }
        }

        /// <summary>
        /// Applies the system's logic to the specified component.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to update.</param>
        protected virtual void UpdateComponent(long frame, TComponent component)
        {
        }

        /// <summary>
        /// Applies the system's rendering to the specified component.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to draw.</param>
        protected virtual void DrawComponent(long frame, TComponent component)
        {
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
            var copy = (AbstractUpdatingComponentSystem<TComponent>)base.NewInstance();

            copy.UpdatingComponents = new List<TComponent>();

            return copy;
        }

        #endregion
    }
}
