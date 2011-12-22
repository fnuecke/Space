using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Systems
{
    public class ShipControlSystem : AbstractComponentSystem<InputParameterization>
    {
        private InputParameterization _parameterization = new InputParameterization();

        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType != ComponentSystemUpdateType.Logic)
            {
                return;
            }

            foreach (var component in Components)
            {
                component.Update(_parameterization);
            }
        }
    }
}
