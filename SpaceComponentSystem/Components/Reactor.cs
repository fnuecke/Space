namespace Space.ComponentSystem.Components
{
    /// <summary>Represents a reactor item, which is used to store and produce energy.</summary>
    public sealed class Reactor : SpaceItem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion
    }
}