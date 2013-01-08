using System;

namespace Engine.Simulation.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand : Command
    {
        #region Fields

        /// <summary>
        /// The frame this command applies to.
        /// </summary>
        public long Frame;

        #endregion
        
        #region Constructor

        protected FrameCommand(Enum type)
            : base(type)
        {
        }

        #endregion
    }
}
