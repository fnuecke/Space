using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Systems
{
    public class ShipControlSystem : AbstractComponentSystem<InputParameterization>
    {
        private InputParameterization _parameterization = new InputParameterization();

        public override void Update(ComponentSystemUpdateType updateType)
        {
            if (updateType != ComponentSystemUpdateType.Logic)
            {
                return;
            }

            foreach (var component in components)
            {
                component.Update(_parameterization);
            }
        }
    }
}
