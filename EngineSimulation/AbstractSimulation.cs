using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;

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
            // There's a chance we have that command in a tentative version. Let's check.
            int index = Commands.FindIndex(x => x.Equals(command));
            if (index >= 0)
            {
                // Already there! Use the authoritative one (or if neither is do nothing).
                if (!Commands[index].IsAuthoritative && command.IsAuthoritative)
                {
                    Commands.RemoveAt(index);
                    Commands.Insert(index, command);
                }
            }
            else
            {
                // New one, append.
                Commands.Add(command);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        public void Update(GameTime gameTime)
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
            Manager.Update(gameTime, CurrentFrame);
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
            packet.ReadPacketizableInto(Manager);

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
        /// Implements deep cloning.
        /// </summary>
        /// <returns>A deep copy of this simulation.</returns>
        public ISimulation DeepCopy()
        {
            return DeepCopy(null);
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public virtual ISimulation DeepCopy(ISimulation into)
        {
            // Get something to start with.
            var copy = (AbstractSimulation)
                ((into != null && into.GetType() == this.GetType())
                ? into
                : MemberwiseClone());
            if (copy == into)
            {
                copy.CurrentFrame = CurrentFrame;
                // Clone system manager.
                copy.Manager = Manager.DeepCopy(copy.Manager);
                copy.Commands.Clear();
                copy.Commands.AddRange(Commands);
            }
            else
            {
                // Clone system manager.
                copy.Manager = Manager.DeepCopy();
                // Copy commands directly (they are immutable).
                copy.Commands = new List<Command>(Commands);
            }

            return copy;
        }

        #endregion
    }
}
