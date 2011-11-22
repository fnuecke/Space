namespace Engine.Simulation
{
    /// <summary>
    /// Steppable interface for phyiscal simulations, allowing for
    /// better collision handling.
    /// </summary>
    public interface IPhysicsSteppable<TSteppable> : ISteppable<IPhysicsEnabledState<TSteppable>>
        where TSteppable : IPhysicsSteppable<TSteppable>
    {

        /// <summary>
        /// Handle pre-update adjustments.
        /// </summary>
        void PreUpdate();

        /// <summary>
        /// Handle post-update adjustments.
        /// </summary>
        void PostUpdate();

    }
}
