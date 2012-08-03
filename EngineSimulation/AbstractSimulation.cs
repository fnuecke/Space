using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for state implementation.
    /// 
    /// <para>
    /// State implementations sub-classing this base class must take care of
    /// (at least) two things:
    /// - Handling of commands (via the HandleCommand function).
    /// - Cloning of the state (may use CloneTo to take care of the basics).
    /// </para>
    /// </summary>
    public abstract class AbstractSimulation : ISimulation
    {
        #region Properties

        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        public long CurrentFrame { get; private set; }

        /// <summary>
        /// All entities registered with this manager.
        /// </summary>
        public IManager Manager { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of queued commands to execute in the next step.
        /// </summary>
        protected List<Command> Commands = new List<Command>();

        #endregion

        #region Constructor

        protected AbstractSimulation()
        {
            this.Manager = new Manager();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        public virtual void PushCommand(Command command)
        {
            Debug.Assert(!Commands.Contains(command));
            Commands.Add(command);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
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
        /// Implement this to handle commands. This will be called for each command
        /// at the moment it should be applied. The implementation must be done in
        /// a way that behaves the same for any permutation of a given set of non-equal
        /// commands. I.e. the order of the command execution must not make a difference.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(Command command);

        #endregion

        #region Hashing / Cloning / Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            // Write the frame number we're currently in.
            packet.Write(CurrentFrame);

            // Write entities.
            packet.Write(Manager);

            // Then serialize all pending commands for the next frame.
            packet.WriteWithTypeInfo(Commands);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Get entities.
            var manager = Manager;
            packet.ReadPacketizableInto(ref manager);

            // Continue with reading the list of commands.
            Commands.Clear();
            foreach (var command in packet.ReadPacketizablesWithTypeInfo<Command>())
            {
                PushCommand(command);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            Manager.Hash(hasher);
        }

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public ISimulation NewInstance()
        {
            var copy = (AbstractSimulation)MemberwiseClone();

            copy.CurrentFrame = 0;
            copy.Manager = Manager.NewInstance();
            copy.Commands = new List<Command>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual void CopyInto(ISimulation into)
        {
            Debug.Assert(into.GetType().TypeHandle.Equals(GetType().TypeHandle));
            Debug.Assert(into != this);

            var copy = (AbstractSimulation)into;

            copy.CurrentFrame = CurrentFrame;
            Manager.CopyInto(copy.Manager);
            copy.Commands.Clear();
            copy.Commands.AddRange(Commands);
        }

        #endregion
    }
}
