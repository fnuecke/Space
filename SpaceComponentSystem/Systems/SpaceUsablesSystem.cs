using Engine.ComponentSystem.RPG.Systems;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public sealed class SpaceUsablesSystem : UsablesSystem<UsableResponse>
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        protected override void Activate(UsableResponse action, int entity)
        {
        }
    }
}
