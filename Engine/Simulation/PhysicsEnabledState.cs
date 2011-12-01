using System.Collections.Generic;
using Engine.Physics;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for states that takes care of some common functionality.
    /// </summary>
    public abstract class PhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : AbstractState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>, IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : PhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// List of collideables in the state (they register themselves upon being adding).
        /// </summary>
        public ICollection<ICollideable> Collideables { get; private set; }

        #endregion

        #region Constructor

        protected PhysicsEnabledState(IPacketizer<TPacketizerContext> packetizer)
            : base(packetizer)
        {
            Collideables = new List<ICollideable>();
        }

        #endregion

        #region Logic

        /// <summary>
        /// Override update to introduce pre- and post-update steppes for resolving collisions.
        /// </summary>
        public override void Update()
        {
            // Increment frame number.
            ++CurrentFrame;

            // Execute any commands for the current frame.
            foreach (var command in commands)
            {
                HandleCommand(command);
            }
            commands.Clear();

            // Update all objects in this state.
            foreach (var steppable in steppables)
            {
                steppable.PreUpdate();
            }
            foreach (var steppable in steppables)
            {
                steppable.Update();
            }
            foreach (var steppable in steppables)
            {
                steppable.PostUpdate();
            }
        }

        #endregion
    }
}
