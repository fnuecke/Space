using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Steppable interface for phyiscal simulations, allowing for
    /// better collision handling.
    /// </summary>
    public interface IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
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
