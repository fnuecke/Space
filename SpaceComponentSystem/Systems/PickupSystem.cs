using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;

namespace Space.ComponentSystem.Systems
{
    /// <summary>System that implements logic for picking up items.</summary>
    public class PickupSystem : AbstractSystem
    {
        #region Constants

        /// <summary>Index group that tracks items</summary>
        public static readonly int IndexId = IndexSystem.GetIndexId();

        #endregion
    }
}