using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>Controls whether weapons on an entity should be shooting.</summary>
    public sealed class WeaponControl : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>Whether ima currently firin mah lazer or not.</summary>
        public bool Shooting;

        #endregion

        #region Initialization

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Shooting = false;
        }

        #endregion
    }
}