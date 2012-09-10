using Engine.ComponentSystem.Systems;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.FarseerPhysics.Systems
{
    /// <summary>
    /// Drives the physics engine's world.
    /// </summary>
    public sealed class PhysicsSystem : AbstractSystem, IUpdatingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the physical world.
        /// </summary>
        public World World
        {
            get { return _world; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The fixed timestep for each update.
        /// </summary>
        private readonly float _timeStep;

        /// <summary>
        /// The physics world.
        /// </summary>
        private World _world = new World(Vector2.Zero);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicsSystem"/> class.
        /// </summary>
        /// <param name="fixedTimeStep">The fixed time step.</param>
        public PhysicsSystem(float fixedTimeStep = 1f / 60f)
        {
            _timeStep = fixedTimeStep;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            _world.Step(_timeStep);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Serialization.Packet Packetize(Serialization.Packet packet)
        {
            base.Packetize(packet);

            // TODO implement binary serialization for world

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);

            // TODO implement binary serialization for world
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Serialization.Hasher hasher)
        {
            base.Hash(hasher);

            // TODO implement hashing in world
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", World=" + World;
        }

        #endregion
    }
}
