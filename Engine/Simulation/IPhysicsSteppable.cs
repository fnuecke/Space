namespace Engine.Simulation
{
    /// <summary>
    /// Steppable interface for phyiscal simulations, allowing for
    /// better collision handling.
    /// </summary>
    public interface IPhysicsSteppable<TState, TSteppable, TCommandType> : ISteppable<TState, TSteppable, TCommandType>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
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
