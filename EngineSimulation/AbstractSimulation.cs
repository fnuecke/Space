using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Engine.Simulation
{
    /// <summary>
    ///     Base class for state implementation.
    ///     <para>
    ///         State implementations sub-classing this base class must take care of (at least) two things:
    ///         <list type="bullet">
    ///             <item>Handling of commands (via the HandleCommand function).</item>
    ///             <item>Cloning of the state (may use CloneTo to take care of the basics).</item>
    ///         </list>
    ///     </para>
    /// </summary>
    public abstract class AbstractSimulation : ISimulation
    {
        #region Properties

        /// <summary>The current frame of the simulation the state represents.</summary>
        public long CurrentFrame { get; private set; }

        /// <summary>All entities registered with this manager.</summary>
        public IManager Manager { get; private set; }

        #endregion

        #region Fields

        /// <summary>List of queued commands to execute in the next step.</summary>
        [PacketizerIgnore]
        protected List<Command> Commands = new List<Command>();

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractSimulation"/> class.
        /// </summary>
        protected AbstractSimulation()
        {
            Manager = new Manager();
        }

        #endregion

        #region Accessors

        /// <summary>Apply a given command to the simulation state.</summary>
        /// <param name="command">the command to apply.</param>
        public virtual void PushCommand(Command command)
        {
            Debug.Assert(!Commands.Contains(command));
            Commands.Add(command);
        }

        #endregion

        #region Logic

        /// <summary>Advance the simulation by one frame.</summary>
        public void Update()
        {
            // Increment frame number.
            ++CurrentFrame;

            // Execute any commands for the current frame.
            foreach (var command in Commands)
            {
                HandleCommand(command);
            }
            Commands.Clear();

            // Update all systems.
            Manager.Update(CurrentFrame);
        }

        /// <summary>
        ///     Implement this to handle commands. This will be called for each command at the moment it should be applied.
        ///     The implementation must be done in a way that behaves the same for any permutation of a given set of non-equal
        ///     commands. I.e. the order of the command execution must not make a difference.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(Command command);

        #endregion

        #region Serialization

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            // Serialize all pending commands for the next frame.
            packet.WriteWithTypeInfo((ICollection<Command>) Commands);

            return packet;
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            // Read the list of commands for the next frame.
            Commands.Clear();
            foreach (var command in packet.ReadPacketizablesWithTypeInfo<Command>())
            {
                PushCommand(command);
            }
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            w.AppendIndent(indent).Write("Commands = {");
            foreach (var command in Commands)
            {
                w.AppendIndent(indent + 1).Dump(command, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public ISimulation NewInstance()
        {
            var copy = (AbstractSimulation) MemberwiseClone();

            copy.CurrentFrame = 0;
            copy.Manager = new Manager();
            copy.Commands = new List<Command>();

            return copy;
        }

        /// <summary>Creates a deep copy of the object, reusing the given object.</summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual void CopyInto(ISimulation into)
        {
            Debug.Assert(into.GetType().TypeHandle.Equals(GetType().TypeHandle));
            Debug.Assert(into != this);

            var copy = (AbstractSimulation) into;

            copy.CurrentFrame = CurrentFrame;
            Manager.CopyInto(copy.Manager);
            copy.Commands.Clear();
            copy.Commands.AddRange(Commands);
        }

        #endregion
    }
}