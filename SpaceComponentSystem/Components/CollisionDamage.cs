using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    ///     This component can be used to apply damage to another entity upon collision with the entity this component belongs
    ///     to.
    ///     <para>
    ///         Used for making bullets damage ships, and causing ships to damage each other. In the latter case, the damage is
    ///         applied with a certain frequency (controlled via the <c>Cooldown</c>).
    ///     </para>
    /// </summary>
    public sealed class CollisionDamage : Component
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

        /// <summary>Whether to destroy this instance on collision.</summary>
        public bool RemoveOnCollision;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            RemoveOnCollision = ((CollisionDamage) other).RemoveOnCollision;

            return this;
        }

        /// <summary>Initialize with the specified parameters.</summary>
        /// <param name="removeOnCollision">whether to self-destruct on collision.</param>
        /// <returns></returns>
        public CollisionDamage Initialize(bool removeOnCollision)
        {
            RemoveOnCollision = removeOnCollision;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            RemoveOnCollision = false;
        }

        #endregion
    }
}