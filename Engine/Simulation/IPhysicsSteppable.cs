using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Steppable interface for phyiscal simulations, allowing for
    /// better collision handling.
    /// </summary>
    public interface IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData> : ISteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
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
