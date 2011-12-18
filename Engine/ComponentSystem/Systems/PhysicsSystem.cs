using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// System responsible for updating physical components.
    /// </summary>
    public class PhysicsSystem : AbstractComponentSystem<PhysicsParameterization>
    {
        private PhysicsParameterization _parameterization = new PhysicsParameterization();

        public override void Update(ComponentSystemUpdateType updateType)
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
