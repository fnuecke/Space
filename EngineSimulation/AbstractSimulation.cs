using System.Collections.Generic;
using Engine.ComponentSystem.Systems;
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
        #region Logger

#if DEBUG && GAMELOG
        /// <summary>
        /// Logger for game log (i.e. steps happening in a simulation).
        /// </summary>
        private static NLog.Logger gamelog = NLog.LogManager.GetLogger("GameLog.Simulation");
#endif

        #endregion

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

#if DEBUG && GAMELOG
        /// <summary>
        /// Whether to log any game state changes in detail, for debugging.
        /// </summary>
        public bool GameLogEnabled { get { return _gameLogEnabled; } set { _gameLogEnabled = value; EntityManager.GameLogEnabled = value; } }

        private bool _gameLogEnabled;
#endif

        #endregion

        #region Fields

        /// <summary>
        /// List of queued commands to execute in the next step.
        /// </summary>
        protected List<Command> commands = new List<Command>();

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
        public virtual void PushCommand(Command command)
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

#if DEBUG && GAMELOG
            if (GameLogEnabled)
            {
                gamelog.Trace("Transitioning to frame {0}.", CurrentFrame);
            }
#endif

            // Execute any commands for the current frame.
            foreach (var command in commands)
            {
#if DEBUG && GAMELOG
                if (GameLogEnabled)
                {
                    gamelog.Trace("Handling command: {0}", command);
                }
#endif
                HandleCommand(command);
            }
            commands.Clear();

            // Update all systems.
            EntityManager.SystemManager.Update(CurrentFrame);
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
            packet.Write(EntityManager);

            // Then serialize all pending commands for the next frame.
            packet.WriteWithTypeInfo(commands);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
#if DEBUG && GAMELOG
            // Disable logging per default.
            GameLogEnabled = false;
#endif

            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Get entities.
            packet.ReadPacketizableInto(EntityManager);

            // Continue with reading the list of commands.
            commands.Clear();
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
            EntityManager.Hash(hasher);
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
                copy.EntityManager = EntityManager.DeepCopy(copy.EntityManager);
                copy.commands.Clear();
                copy.commands.AddRange(commands);
            }
            else
            {
                // Clone system manager.
                copy.EntityManager = EntityManager.DeepCopy();
                // Copy commands directly (they are immutable).
                copy.commands = new List<Command>(commands);
            }

            return copy;
        }

        #endregion
    }
}
