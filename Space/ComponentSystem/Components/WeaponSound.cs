using System;
using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Components.Messages;

namespace Space.ComponentSystem.Components
{
    public sealed class WeaponSound : AbstractSound
    {
        protected override string GetSoundForMessage(ValueType message)
        {
            if (message is WeaponFired)
            {
                return ((WeaponFired)message).Weapon.Sound;
            }
            return String.Empty;
        }
    }
}
