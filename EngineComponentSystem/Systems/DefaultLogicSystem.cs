using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    public sealed class DefaultLogicSystem : AbstractComponentSystem<DefaultLogicParameterization>
    {
        private static readonly DefaultLogicParameterization _parameterization = new DefaultLogicParameterization();

        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                foreach (var component in Components)
                {
                    component.Update(_parameterization);
                }
            }
        }
    }
}
