using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Entities with this component are part of a squad (a group of ships),
    /// which allows to keep a grouping to pick a new lead should the old one
    /// die, as well as stuff like formation flying.
    /// </summary>
    public sealed class Squad : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the squad's ID, by which it is tracked in the squad system.
        /// </summary>
        public int SquadId { get; internal set; }

        /// <summary>
        /// Gets the leader of this squad.
        /// </summary>
        public int Leader
        {
            get { return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).GetLeader(SquadId); }
        }

        /// <summary>
        /// Gets a list of all the members in this squad.
        /// </summary>
        public IEnumerable<int> Members
        {
            get { return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).GetMembers(SquadId); }
        }

        /// <summary>
        /// Gets the size of this squad, i.e. the number of members in it.
        /// </summary>
        public int Count
        {
            get { return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).GetCount(SquadId); }
        }

        /// <summary>
        /// Gets or sets the formation the squad keeps.
        /// </summary>
        public SquadSystem.AbstractFormation Formation
        {
            get { return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).GetFormation(SquadId); }
            set { ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).SetFormation(SquadId, value); }
        }

        /// <summary>
        /// Gets or sets the formation spacing, i.e. the space to keep between individual
        /// formation slots. This should at least be as large as the flocking separation
        /// of AI behaviors.
        /// </summary>
        public float FormationSpacing
        {
            get { return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).GetFormationSpacing(SquadId); }
            set { ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).SetFormationSpacing(SquadId, value); }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            SquadId = ((Squad)other).SquadId;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            SquadId = 0;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Gets the position of this entity in the squad formation (i.e. where it
        /// should be at this time).
        /// </summary>
        public FarPosition ComputeFormationOffset()
        {
            return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).ComputeFormationOffset(SquadId, Entity);
        }

        /// <summary>
        /// Determines whether the squad contains the specified entity.
        /// </summary>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>
        ///   <c>true</c> if the squad contains the specified entity; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(int entity)
        {
            return ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).Contains(SquadId, entity);
        }

        /// <summary>
        /// Adds a new member to this squad. Note that this will automatically
        /// register the entity with the squad component of all other already-
        /// members of this squad.
        /// </summary>
        /// <param name="entity">The entity to add to the squad.</param>
        public void AddMember(int entity)
        {
            ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).AddMember(SquadId, entity);
        }

        /// <summary>
        /// Removes an entity from this squad. Note that this will automatically
        /// remove the entity from the squad components of all other members.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveMember(int entity)
        {
            ((SquadSystem)Manager.GetSystem(SquadSystem.TypeId)).RemoveMember(SquadId, entity);
        }

        #endregion
    }
}
