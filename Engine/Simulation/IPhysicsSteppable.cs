namespace Engine.Simulation
{
    /// <summary>
    /// Steppable interface for phyiscal simulations, allowing for
    /// better collision handling.
    /// </summary>
    public interface IPhysicsSteppable<TState, TSteppable> : ISteppable<TState, TSteppable>
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
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
