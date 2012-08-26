namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public sealed class Thruster : SpaceItem
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
