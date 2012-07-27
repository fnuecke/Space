namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a shield item, which blocks damage.
    /// </summary>
    public sealed class Shield : SpaceItem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = Engine.ComponentSystem.Manager.GetComponentTypeId(typeof(Shield));

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
