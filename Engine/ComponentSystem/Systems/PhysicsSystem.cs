using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// System responsible for updating physical components.
    /// </summary>
    public class PhysicsSystem : AbstractComponentSystem<PhysicsParameterization>
    {
        public override void Update()
        {
            foreach (var component in components)
            {
                component.Update(null);
            }
        }
    }
}
