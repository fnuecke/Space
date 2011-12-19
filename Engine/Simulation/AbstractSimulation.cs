using System.Collections.Generic;
using Engine.Commands;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
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
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// <summary>
        /// All entities registered with this manager.
        /// </summary>
        public IEntityManager EntityManager { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of queued commands to execute in the next step.
        /// </summary>
        protected List<ICommand> commands = new List<ICommand>();

        #endregion

        #region Constructor

        protected AbstractSimulation()
        {
            this.EntityManager = new EntityManager();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        public virtual void PushCommand(ICommand command)
        {
            // There's a chance we have that command in a tentative version. Let's check.
            int index = commands.FindIndex(x => x.Equals(command));
            if (index >= 0)
            {
                // Already there! Use the authoritative one (or if neither is do nothing).
                if (!commands[index].IsAuthoritative && command.IsAuthoritative)
                {
                    commands.RemoveAt(index);
                    commands.Insert(index, command);
                }
            }
            else
            {
                // New one, append.
                commands.Add(command);
            }
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
            foreach (var command in commands)
            {
                HandleCommand(command);
            }
            commands.Clear();

            // Update all systems.
            EntityManager.SystemManager.Update(ComponentSystemUpdateType.Logic);
        }

        /// <summary>
        /// Implement this to handle commands. This will be called for each command
        /// at the moment it should be applied. The implementation must be done in
        /// a way that behaves the same for any permutation of a given set of non-equal
        /// commands. I.e. the order of the command execution must not make a difference.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(ICommand command);

        #endregion

        #region Hashing / Cloning / Serialization

        public virtual Packet Packetize(Packet packet)
        {
            // Write the frame number we're currently in.
            packet.Write(CurrentFrame);

            // Write entities.
            EntityManager.Packetize(packet);

            // Then serialize all pending commands for the next frame.
            packet.Write(commands.Count);
            foreach (var command in commands)
            {
                Packetizer.Packetize(command, packet);
            }

            return packet;
        }

        public virtual void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Get entities.
            EntityManager.Depacketize(packet);

            // Continue with reading the list of commands.
            commands.Clear();
            int numCommands = packet.ReadInt32();
            for (int j = 0; j < numCommands; ++j)
            {
                PushCommand(Packetizer.Depacketize<ICommand>(packet));
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            EntityManager.Hash(hasher);
        }

        /// <summary>
        /// Implements deep cloning.
        /// </summary>
        /// <returns>A deep copy of this simulation.</returns>
        public virtual object Clone()
        {
            var copy = (AbstractSimulation)MemberwiseClone();

            // Clone system manager.
            copy.EntityManager = (IEntityManager)EntityManager.Clone();

            // Copy commands directly (they are immutable).
            copy.commands = new List<ICommand>(commands);

            return copy;
        }

        #endregion
    }
}
