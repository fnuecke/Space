using System;
using Engine.ComponentSystem.RPG.Components;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to freeze entities when hit by non-blocked damage.
    /// </summary>
    public sealed class FreezeOnHitSystem : AbstractDamageApplyingSystem
    {
        /// <summary>
        /// Applies the damage for this system.
        /// </summary>
        /// <param name="owner">The entity that caused the damage.</param>
        /// <param name="attributes">The attributes of the entity doing the damage.</param>
        /// <param name="damagee">The entity being damage.</param>
        protected override void ApplyDamage(int owner, Attributes<AttributeType> attributes, int damagee)
        {
            // See if we should proc.
            var chance = attributes.GetValue(AttributeType.AttackFreezeChance);
            if (Random.NextDouble() >= chance)
            {
                return;
            }

            // See how long we last.
            var minDuration = attributes.GetValue(AttributeType.AttackFreezeMinDuration);
            var maxDuration = attributes.GetValue(AttributeType.AttackFreezeMaxDuration);
            if (minDuration < 0)
            {
                minDuration = 0;
            }
            if (maxDuration < minDuration)
            {
                maxDuration = minDuration;
            }
            var reduction = attributes.GetValue(AttributeType.ColdAndFreezeDurationReduction);
            var duration = (int)Math.Round((Random.NextDouble(minDuration, maxDuration) - reduction) * Settings.TicksPerSecond);
            if (duration <= 0)
            {
                return;
            }

            // See if the target is already frozen, if so just prolong
            // the effect (if our duration is longer).
            var effect = (FreezeStatusEffect)Manager.GetComponent(damagee, FreezeStatusEffect.TypeId);
            if (effect != null)
            {
                effect.Remaining = Math.Max(effect.Remaining, duration);
            }
            else
            {
                Manager.AddComponent<FreezeStatusEffect>(damagee).Initialize(duration);
            }
        }
    }
}
