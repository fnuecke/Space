namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Parameterization that must not be used by any component. Only meant to
    /// be used for systems that wish to use the existing functionality of the
    /// <c>AbstractComponentSystem<c> class, but do not actually need a list of
    /// components to work on.
    /// </summary>
    public sealed class NullParameterization
    {
    }
}
