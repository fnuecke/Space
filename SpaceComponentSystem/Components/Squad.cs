using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;

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
        /// Gets the leader of this squad.
        /// </summary>
        public int Leader
        {
            get { return _members[0]; }
        }

        /// <summary>
        /// Gets the size of this squad, i.e. the number of members in it.
        /// </summary>
        public int Count
        {
            get { return _members.Count; }
        }

        /// <summary>
        /// Gets or sets the formation spacing, i.e. the space to keep between individual
        /// formation slots. This should at least be as large as the flocking separation
        /// of AI behaviors.
        /// </summary>
        public float FormationSpacing { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The list of ships in this squad.
        /// </summary>
        private readonly List<int> _members = new List<int>();

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Squad"/> class.
        /// </summary>
        public Squad()
        {
            FormationSpacing = 200;
        }

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            _members.AddRange(((Squad)other)._members);

            return this;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public Squad Initialize()
        {
            _members.Add(Entity);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _members.Clear();
            FormationSpacing = 200;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Gets the position of this entity in the squad formation (i.e. where it
        /// should be at this time).
        /// </summary>
        public FarPosition ComputeFormationOffset()
        {
            var leaderTransform = (Transform)Manager.GetComponent(_members[0], Transform.TypeId);

            // Note: we may one day decide to extend this to support multiple formations.

            // Note: all formation positions are computed without taking the leader's rotation
            // into account directly. Instead, the final position is rotated at the end, around
            // the leader's position, based on the leader's orientation.

            // Note: in the following formation diagrams 'L' is the leader and 'F' are the
            // followers, filled up top-down left-to-right.

            // First get the own index in the formation.
            var index = _members.IndexOf(Entity);

            // The position relative to the leader. This will be in a unit scale and "expanded"
            // based on the spacing set for the squad.
            var position = Vector2.Zero;

            // This is an implementation for a 'box' formation, i.e. the formation will look
            // like this:
            //  F  F  L  F  F
            //  F  F  F  F  F 
            // The balance that a formation is determined how it expands: it toggles between
            // vertical and horizontal expansion whenever the formation becomes "full". So
            // in numbers:
            // 6 1 0 2 7
            // 8 4 3 5 9
            //    ...
            {
                // Note: if someone can be bothered to figure out a procedural way to generate
                // this formation, that'd be nice.
                Vector2[] box =
                    {
                        new Vector2(0, 0),
                        new Vector2(-1, 0),
                        new Vector2(1, 0),
                        new Vector2(0, 1),
                        new Vector2(-1, 1),
                        new Vector2(1, 1),
                        new Vector2(-2, 0),
                        new Vector2(2, 0),
                        new Vector2(-2, 1),
                        new Vector2(2, 1),
                        new Vector2(0, 2),
                        new Vector2(-1, 2),
                        new Vector2(1, 2),
                        new Vector2(-2, 2),
                        new Vector2(2, 2),
                        new Vector2(-3, 0),
                        new Vector2(3, 0),
                        new Vector2(-3, 1),
                        new Vector2(3, 1),
                        new Vector2(-3, 2),
                        new Vector2(3, 2),
                        new Vector2(0, 3),
                        new Vector2(-1, 3),
                        new Vector2(1, 3),
                        new Vector2(-2, 3),
                        new Vector2(2, 3),
                        new Vector2(-3, 3),
                        new Vector2(3, 3)
                    };
                if (index < box.Length)
                {
                    position = box[index];
                }
            }

            // This is an implementation for a 'triangular' formation, i.e. the formation
            // will look like this:
            //           L
            //         F   F
            //       F   F   F
            //     F   F   F   F
            //          ...

            // Rotate around origin of the formation (which should be the leader's position in
            // most cases).
            var finalPosition = leaderTransform.Translation;
            var cosRadians = (float)Math.Cos(leaderTransform.Rotation);
            var sinRadians = (float)Math.Sin(leaderTransform.Rotation);
            finalPosition.X += (-position.Y * cosRadians - position.X * sinRadians) * FormationSpacing;
            finalPosition.Y += (-position.Y * sinRadians + position.X * cosRadians) * FormationSpacing;

            return finalPosition;
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
            return _members.Contains(entity);
        }

        /// <summary>
        /// Adds a new member to this squad. Note that this will automatically
        /// register the entity with the squad component of all other already-
        /// members of this squad.
        /// </summary>
        /// <param name="entity">The entity to add to the squad.</param>
        public void AddMember(int entity)
        {
            // Skip if the entity is already there.
            if (_members.Contains(entity))
            {
                return;
            }

            // Make sure the entity isn't in a squad (except the identity squad).
            var newMemberSquad = (Squad)Manager.GetComponent(entity, TypeId);
            newMemberSquad.RemoveMember(entity);
            // Register that entity will all existing members.
            foreach (var member in _members)
            {
                // Skip this instance to avoid breaking the iterator.
                if (member != Entity)
                {
                    ((Squad)Manager.GetComponent(member, TypeId))._members.Add(entity);
                }
            }
            _members.Add(entity);
            // Tell the new member about its new sqad mates (after clearing, to make
            // sure the order is the same -- in particular that the first entry is
            // the same, which must be the squad leader).
            newMemberSquad._members.Clear();
            newMemberSquad._members.AddRange(_members);
        }

        /// <summary>
        /// Removes an entity from this squad. Note that this will automatically
        /// remove the entity from the squad components of all other members.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveMember(int entity)
        {
            // Error if there's no such member in this squad.
            if (!_members.Contains(entity))
            {
                throw new ArgumentException("No such member in this squad.", "entity");
            }

            // Remove from all lists.
            foreach (var member in _members)
            {
                // Skip this instance to avoid breaking the iterator.
                if (member != Entity)
                {
                    ((Squad)Manager.GetComponent(member, TypeId))._members.Remove(entity);
                }
            }
            _members.Remove(entity);

            // Reset the squad component of that entity to a blank squad with only that
            // entity in it.
            ((Squad)Manager.GetComponent(entity, TypeId))._members.Clear();
            ((Squad)Manager.GetComponent(entity, TypeId))._members.Add(entity);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);
            packet.Write(_members.Count);
            for (var i = 0; i < _members.Count; i++)
            {
                packet.Write(_members[i]);
            }
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _members.Clear();
            var memberCount = packet.ReadInt32();
            for (var i = 0; i < memberCount; i++)
            {
                _members.Add(packet.ReadInt32());
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            for (var i = 0; i < _members.Count; i++)
            {
                hasher.Put(_members[i]);
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Members=" + string.Join(", ", _members);
        }

        #endregion
    }
}
