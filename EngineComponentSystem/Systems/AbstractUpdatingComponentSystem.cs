using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>Base class for component systems, pre-implementing adding / removal of components.</summary>
    /// <typeparam name="TComponent">The type of component handled in this system.</typeparam>
    public abstract class AbstractUpdatingComponentSystem<TComponent>
        : AbstractComponentSystem<TComponent>, IUpdatingSystem
        where TComponent : Component
    {
        #region Single-Allocation

        /// <summary>
        ///     Reused for iterating components when updating, to avoid modifications to the list of components breaking the
        ///     update.
        /// </summary>
        [PacketizeIgnore]
        private List<TComponent> _updatingComponents = new List<TComponent>();

        #endregion

        #region Logic

        /// <summary>
        ///     Loops over all components and calls <c>UpdateComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Update(long frame)
        {
            _updatingComponents.AddRange(Components);
            for (int i = 0, j = _updatingComponents.Count; i < j; ++i)
            {
                var component = _updatingComponents[i];
                if (component.Enabled)
                {
                    UpdateComponent(frame, component);
                }
            }
            _updatingComponents.Clear();
        }

        /// <summary>Applies the system's logic to the specified component.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to update.</param>
        protected virtual void UpdateComponent(long frame, TComponent component) {}

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (AbstractUpdatingComponentSystem<TComponent>) base.NewInstance();

            copy._updatingComponents = new List<TComponent>();

            return copy;
        }

        #endregion
    }
}