using Engine.ComponentSystem.Systems;
using Engine.Serialization;

namespace Engine.ComponentSystem.Components
{
    public class Sound : Component
    {
        #region Constants

        /// <summary>
        /// Index group to use for sound computations.
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The name of the sound
        /// </summary>
        public string SoundName;

        #endregion

        #region Initialization
        
        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSound = (Sound)other;
            SoundName = otherSound.SoundName;

            return this;
        }

        /// <summary>
        /// Initialize the Sound with the given sound name
        /// </summary>
        /// <param name="soundname">The name of the sound to be played</param>
        /// <returns></returns>
        public Sound Initialize(string soundname)
        {
            SoundName = soundname;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            SoundName = null;
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
                .Write(SoundName);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            SoundName = packet.ReadString();
        }

        #endregion
    }
}
