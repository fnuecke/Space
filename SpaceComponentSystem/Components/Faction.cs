using Engine.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>Allows assigning entities to factions.</summary>
    public class Faction : Component
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

        /// <summary>The faction this component's entity belongs to.</summary>
        public Factions Value;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified faction.</summary>
        /// <param name="factions">The factions.</param>
        public Faction Initialize(Factions factions)
        {
            Value = factions;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = Factions.Nature;
        }

        #endregion
    }
}