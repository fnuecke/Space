using System;
using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Components
{
    public sealed class WeaponSound : AbstractSound
    {
        #region Logic

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
