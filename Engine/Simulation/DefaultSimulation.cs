using Engine.Commands;
using Engine.ComponentSystem.Systems;

namespace Engine.Simulation
{
    /// <summary>
    /// A method that can handle a single command and apply it to a component system manager.
    /// 
    /// <para>
    /// This handler must not hold any state information, as it may be used by different
    /// iterations of the simulation, and is passed on to clones directly.
    /// </para>
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="systemManager">The relevant entity manager.</param>
    public delegate void CommandHandler(ICommand command, IEntityManager manager);

    /// <summary>
    /// A delegate-based implementation of a simulation that supports pruning non-authoritative
    /// commands. Actual command application is delegated to registered handlers.
    /// </summary>
    public sealed class DefaultSimulation : AbstractSimulation, IAuthoritativeSimulation
    {
        #region Events

        /// <summary>
        /// Dispatched when a command needs handling.
        /// </summary>
        public event CommandHandler Command;

        #endregion

        #region Command delegating
        
        /// <summary>
        /// Implemented by delegating commands to registered command handlers.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        protected override void HandleCommand(ICommand command)
        {
            OnCommand(command);
        }

        #endregion

        #region Interface

        /// <summary>
        /// Forces the state to remove any pending commands that
        /// would be handled in the next <c>Update()</c> run.
        /// </summary>
        /// <returns><c>true</c> if any commands were removed.</returns>
        public bool SkipTentativeCommands()
        {
            bool hadTentative = false;
            for (int i = commands.Count - 1; i >= 0; --i)
            {
                if (!commands[i].IsAuthoritative)
                {
                    hadTentative = true;
                    commands.RemoveAt(i);
                }
            }
            return hadTentative;
        }

        #endregion

        #region Event dispatching

        private void OnCommand(ICommand command)
        {
            if (Command != null)
            {
                Command(command, EntityManager);
            }
        }

        #endregion
    }
}
