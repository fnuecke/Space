using Engine.Simulation.Commands;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for command emitters.
    /// </summary>
    public abstract class AbstractCommandEmitter<TCommand>
        : ICommandEmitter<TCommand>
        where TCommand : Command
    {
        /// <summary>
        /// Event dispatched whenever a new command was generated. This command
        /// will be injected into the simulation at it's current frame.
        /// 
        /// The dispatched events must be of type <c>CommandEmittedEventArgs</c>,
        /// with the proper generics as to match the controller it'll be registered
        /// with.
        /// </summary>
        public event CommandEmittedEventHandler<TCommand> CommandEmitted;

        /// <summary>
        /// Use this to dispatch new command events.
        /// </summary>
        /// <param name="e">the command that was generated.</param>
        protected void OnCommand(TCommand command)
        {
            if (CommandEmitted != null)
            {
                CommandEmitted(command);
            }
        }
    }
}
