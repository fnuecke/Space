using Engine.Commands;
using Engine.Serialization;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for command emitters.
    /// </summary>
    public abstract class AbstractCommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : ICommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Event dispatched whenever a new command was generated. This command
        /// will be injected into the simulation at it's current frame.
        /// 
        /// The dispatched events must be of type <c>CommandEmittedEventArgs</c>,
        /// with the proper generics as to match the controller it'll be registered
        /// with.
        /// </summary>
        public event CommandEmittedEventHandler<TCommand, TCommandType, TPlayerData, TPacketizerContext> CommandEmitted;

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
