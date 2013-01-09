using Engine.Serialization;
using Engine.Simulation.Commands;
using Space.Session;

namespace Space.Simulation.Commands
{
    /// <summary>This command is used to load a player's profile, which is done right after the player joined a game.</summary>
    internal sealed class RestoreProfileCommand : FrameCommand
    {
        #region Fields

        /// <summary>The profile data to use.</summary>
        [PacketizerCreate]
        public readonly Profile Profile;

        #endregion

        #region Constructor

        public RestoreProfileCommand(int playerNumber, Profile profile, long frame)
            : base(SpaceCommandType.RestoreProfile)
        {
            PlayerNumber = playerNumber;
            Frame = frame;
            Profile = profile;
        }

        public RestoreProfileCommand()
            : this(0, null, 0) {}

        #endregion
    }
}