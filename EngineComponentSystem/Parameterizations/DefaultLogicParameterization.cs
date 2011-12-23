namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// The default parameterization for components doing some logic update
    /// that does not require special parameterization.
    /// </summary>
    public sealed class DefaultLogicParameterization
    {
        /// <summary>
        /// The frame that the simulation is currently in.
        /// </summary>
        public long Frame { get; set; }
    }
}
