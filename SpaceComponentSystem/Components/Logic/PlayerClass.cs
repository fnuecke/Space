using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tells the player class via a player's ship.
    /// </summary>
    public sealed class PlayerClass : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The player class of this ship's player.
        /// </summary>
        public PlayerClassType Value;

        #endregion

        #region Constructor

        public PlayerClass(PlayerClassType playerClass)
        {
            Value = playerClass;
        }

        /// <summary>
        /// For serialization.
        /// </summary>
        public PlayerClass()
        {
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((int)Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = (PlayerClassType)packet.ReadInt32();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (PlayerClass)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Value = Value;
            }

            return copy;
        }

        #endregion
    }
}
