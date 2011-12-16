namespace Engine.ComponentSystem.Components
{
    public interface IComponent<TUpdateParameterization>
    {
        void Update(TUpdateParameterization parameterization);
    }
}
