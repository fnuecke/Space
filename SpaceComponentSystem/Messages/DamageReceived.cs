using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Messages
{
    /// <summary>
    ///     This message is fired when damage should actually be applied (this is essentially the alternative branch to
    ///     <c>DamageBlocked</c>).
    /// </summary>
    internal struct DamageReceived
    {
        /// <summary>The root cause for the damage.</summary>
        public int Owner;

        /// <summary>Attributes of the entity doing the damage.</summary>
        public Attributes<AttributeType> Attributes;

        /// <summary>The entity being damaged.</summary>
        public int Damagee;
    }
}