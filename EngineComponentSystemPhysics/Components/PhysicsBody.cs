using Engine.ComponentSystem.Components;
using Engine.FarMath;
using FarseerPhysics.Dynamics;

namespace Engine.ComponentSystem.FarseerPhysics.Components
{
    /// <summary>
    /// Represents a single body in the physics simulation.
    /// </summary>
    public abstract class PhysicsBody : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The id of the body representing the entity of this component in the physics engine.
        /// </summary>
        public int BodyId;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherBody = (PhysicsBody)other;
            BodyId = otherBody.BodyId;

            return this;
        }

        /// <summary>
        /// Initializes the component with the specified body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        public PhysicsBody Initialize(Body body)
        {
            BodyId = body.BodyId;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            BodyId = 0;
        }

        /// <summary>
        /// Creates the body, fixture and shape representing the entity this component belongs to.
        /// </summary>
        /// <param name="world">The world to create the parts in.</param>
        /// <param name="position">The position at which to create the body.</param>
        /// <returns>The created body.</returns>
        protected abstract Body CreateBody(World world, FarPosition position);

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
            return base.Packetize(packet)
                .Write(BodyId);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);

            BodyId = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Serialization.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BodyId);
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
            return base.ToString() + ", BodyId=" + BodyId;
        }

        #endregion
    }
}
