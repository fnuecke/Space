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
    public sealed class DefaultLogicSystem : AbstractComponentSystem<DefaultLogicParameterization>
    {
        /// <summary>
        /// Default parameterization used for every update call.
        /// </summary>
        private static readonly DefaultLogicParameterization _parameterization = new DefaultLogicParameterization();

        /// <summary>
        /// Triggers the update for all components which do a default logic
        /// update. Only does something for logic updates.
        /// </summary>
        /// <param name="updateType">The type of update to perform.</param>
        /// <param name="frame">The frame the simulation is currently in.</param>
        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                _parameterization.Frame = frame;
                foreach (var component in Components)
                {
                    component.Update(_parameterization);
                }
            }
        }
    }
}
