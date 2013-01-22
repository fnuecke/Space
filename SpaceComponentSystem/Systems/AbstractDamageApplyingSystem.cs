using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Base class for systems implementing damage status effect application or other 'on hit' effects.</summary>
    public abstract class AbstractDamageApplyingSystem : AbstractSystem
    {
        #region Fields

        /// <summary>Randomizer used for determining whether certain effects should be applied (e.g. dots, blocking, ...).</summary>
        protected MersenneTwister Random = new MersenneTwister(0);

        #endregion

        #region Logic

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<DamageReceived>(OnDamageReceived);
        }

        private void OnDamageReceived(DamageReceived message)
        {
            ApplyDamage(message.Owner, message.Attributes, message.Damagee);
        }

        /// <summary>Applies the damage for this system.</summary>
        /// <param name="owner">The entity that caused the damage.</param>
        /// <param name="attributes">The attributes of the entity doing the damage.</param>
        /// <param name="damagee">The entity being damage.</param>
        protected abstract void ApplyDamage(int owner, Attributes<AttributeType> attributes, int damagee);

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (AbstractDamageApplyingSystem) base.NewInstance();

            copy.Random = new MersenneTwister(0);

            return copy;
        }

        #endregion
    }
}