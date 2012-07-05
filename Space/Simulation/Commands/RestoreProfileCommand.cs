using Engine.Serialization;
using Engine.Simulation.Commands;
using Space.Session;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// This command is used to load a player's profile, which is done right
    /// after the player joined a game.
    /// </summary>
    sealed class RestoreProfileCommand : FrameCommand
    {
        #region Fields
        
        /// <summary>
        /// The profile data to use.
        /// </summary>
        public Profile Profile;

        #endregion

        #region Constructor
        
        public RestoreProfileCommand(int playerNumber, Profile profile, long frame)
            : base(SpaceCommandType.RestoreProfile)
        {
            this.PlayerNumber = playerNumber;
            this.Frame = frame;
            this.Profile = profile;
        }

        public RestoreProfileCommand()
            : this(0, null, 0)
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
                .Write(Profile);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Profile = packet.ReadPacketizable<Profile>();
        }

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public override bool Equals(Command other)
        {
            return base.Equals(other) &&
                   (Profile == null
                        ? ((RestoreProfileCommand)other).Profile == null
                        : Profile.Equals(((RestoreProfileCommand)other).Profile));
        }

        #endregion
    }
}
