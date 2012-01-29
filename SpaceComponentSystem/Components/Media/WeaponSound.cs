using System;
using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Plays a weapon sound when it is fired.
    /// </summary>
    public sealed class WeaponSound : AbstractSound
    {
        #region Logic

        /// <summary>
        /// Tells if a message should result in a sound being played, and if yes, which.
        /// </summary>
        /// <param name="message">The message to check.</param>
        /// <returns>
        /// The sound to be played.
        /// </returns>
        protected override string GetSoundForMessage(ValueType message)
        {
            if (message is WeaponFired)
            {
                return ((WeaponFired)message).Weapon.Sound;
            }
            return String.Empty;
        }

        #endregion
    }
}
