using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Default system to trigger logic updates. This is used for components
    /// that do not need any parameterization for their updates.
    /// 
    /// <para>
    /// Note that the order will be determined by the order in which the
    /// components were added to the system (or the entity, which in turn
    /// triggered adding them to the system).
    /// </para>
    /// </summary>
    public sealed class DefaultLogicSystem : AbstractComponentSystem<DefaultLogicParameterization, NullParameterization>
    {
        #region Fields
        
        /// <summary>
        /// Default parameterization used for every update call.
        /// </summary>
        private DefaultLogicParameterization _parameterization = new DefaultLogicParameterization();

        #endregion

        #region Logic
        
        /// <summary>
        /// Triggers the update for all components which do a default logic
        /// update. Only does something for logic updates.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        /// <param name="frame">The frame the simulation is currently in.</param>
        public override void Update(long frame)
        {
            _parameterization.Frame = frame;
            var currentComponents = new List<AbstractComponent>(UpdateableComponents);
            foreach (var component in currentComponents)
            {
                // Only enabled components, and ones that have not been removed
                // in this very update run.
                if (component.Enabled && component.Entity.Manager != null)
                {
                    component.Update(_parameterization);
                }
            }
        }

        #endregion

        #region Copying

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (DefaultLogicSystem)base.DeepCopy(into);

            if (copy != into)
            {
                copy._parameterization = new DefaultLogicParameterization();
            }

            return copy;
        }

        #endregion
    }
}
