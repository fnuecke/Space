namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single armor item, which determines an entity's armor
    /// rating.
    /// </summary>
    public sealed class Armor : SpaceItem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion
    }
}
