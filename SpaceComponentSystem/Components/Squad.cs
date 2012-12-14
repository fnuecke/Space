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

        #region Types

        /// <summary>
        /// Possible formation types for squads.
        /// </summary>
        public enum FormationType
        {
            /// <summary>
            /// No formation.
            /// </summary>
            None,

            /// <summary>
            /// A block formation, i.e. units arrange in a rectangular shape.
            /// </summary>
            Block,

            /// <summary>
            /// A column formation, i.e. units arrange in a jagged line.
            /// </summary>
            Column,

            /// <summary>
            /// Line formation, i.e. units arrange in single column.
            /// </summary>
            Line,

            /// <summary>
            /// Wedge formation, i.e. units align in an open, triangular shape (inverse V).
            /// </summary>
            Wedge,

            /// <summary>
            /// Filled wedge formation, i.e. units align in a closed triangular shape.
            /// </summary>
            FilledWedge,

            /// <summary>
            /// Vee formation, i.e. units align in an inverse open triangular shape (V).
            /// </summary>
            Vee
        }

        #endregion

        #region Constants

        // Note: all formation positions are computed without taking the leader's rotation
        // into account directly. Instead, the final position is rotated at the end each time,
        // around the leader's position, based on the leader's orientation.

        // Note: all formations are defined in a coordinate system where forward is "up",
        // i.e. is the negative y axis. In game forward is actually to the right, but this
        // will be corrected for accordingly. For me it's easier to visualize formations
        // this way, which is the only reason it's like this.

        // Note: formations are defined by specifying the unit offset relative to the squad
        // leader for each member, with the index matching the member index. This has the
        // limitation of a maximum count supported, but the huge advantage of high flexibility
        // as well as good performance (simple lookup).

        // Note: in the following formation diagrams 'L' is the leader and 'F' are the
        // followers, filled up top-down left-to-right.

        /// <summary>
        /// This is an implementation for a block formation, i.e. the formation will look
        /// like this:
        ///  F  F  L  F  F
        ///  F  F  F  F  F
        ///       ...
        /// The balance that a formation is determined how it expands: it toggles between
        /// vertical and horizontal expansion whenever the formation becomes "full". So
        /// in numbers:
        /// 6 1 0 2 7
        /// 8 4 3 5 9
        ///    ...
        /// </summary>
        private static readonly Vector2[] BlockFormation =
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

        /// <summary>
        /// This is an implementation for a column formation, i.e. the formation
        /// will look like this:
        ///           L
        ///         F
        ///           F
        ///         F
        ///          ...
        /// The order goes like so:
        ///           0
        ///         1
        ///           2
        ///          ...
        /// </summary>
        private static readonly Vector2[] ColumnFormation =
            {
                new Vector2(0, 0),
                new Vector2(-1, 1),
                new Vector2(0, 2),
                new Vector2(-1, 3),
                new Vector2(0, 4),
                new Vector2(-1, 5),
                new Vector2(0, 6),
                new Vector2(-1, 7),
                new Vector2(0, 8),
                new Vector2(-1, 9),
                new Vector2(0, 10),
                new Vector2(-1, 11),
                new Vector2(0, 12),
                new Vector2(-1, 13),
                new Vector2(0, 14),
                new Vector2(-1, 15),
                new Vector2(0, 16),
                new Vector2(-1, 17),
                new Vector2(0, 18),
                new Vector2(-1, 19),
                new Vector2(0, 20),
                new Vector2(-1, 21),
                new Vector2(0, 22),
                new Vector2(-1, 23),
                new Vector2(0, 24),
                new Vector2(-1, 25),
                new Vector2(0, 26),
                new Vector2(-1, 27),
                new Vector2(0, 28),
                new Vector2(-1, 29)
            };

        /// <summary>
        /// This is an implementation for a line formation, i.e. the formation
        /// will look like this:
        ///           L
        ///           F
        ///           F
        ///          ...
        /// The order goes like so:
        ///           0
        ///           1
        ///           2
        ///          ...
        /// </summary>
        private static readonly Vector2[] LineFormation =
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(0, 2),
                new Vector2(0, 3),
                new Vector2(0, 4),
                new Vector2(0, 5),
                new Vector2(0, 6),
                new Vector2(0, 7),
                new Vector2(0, 8),
                new Vector2(0, 9),
                new Vector2(0, 10),
                new Vector2(0, 11),
                new Vector2(0, 12),
                new Vector2(0, 13),
                new Vector2(0, 14),
                new Vector2(0, 15),
                new Vector2(0, 16),
                new Vector2(0, 17),
                new Vector2(0, 18),
                new Vector2(0, 19),
                new Vector2(0, 20),
                new Vector2(0, 21),
                new Vector2(0, 22),
                new Vector2(0, 23),
                new Vector2(0, 24),
                new Vector2(0, 25),
                new Vector2(0, 26),
                new Vector2(0, 27),
                new Vector2(0, 28),
                new Vector2(0, 29)
            };

        /// <summary>
        /// This is an implementation for an open wedge formation, i.e. the formation
        /// will look like this:
        ///           L
        ///         F   F
        ///       F       F
        ///     F           F
        ///          ...
        /// The order goes like so:
        ///           0
        ///         1   2
        ///       3       4
        ///          ...
        /// </summary>
        private static readonly Vector2[] WedgeFormation =
            {
                new Vector2(0, 0),
                new Vector2(-0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(-1, 2),
                new Vector2(1, 2),
                new Vector2(-1.5f, 3),
                new Vector2(1.5f, 3),
                new Vector2(-2, 4),
                new Vector2(2, 4),
                new Vector2(-2.5f, 5),
                new Vector2(2.5f, 5),
                new Vector2(-3, 6),
                new Vector2(3, 6),
                new Vector2(-3.5f, 7),
                new Vector2(3.5f, 7),
                new Vector2(-4, 8),
                new Vector2(4, 8),
                new Vector2(-4.5f, 9),
                new Vector2(4.5f, 9),
                new Vector2(-5f, 10),
                new Vector2(5f, 10),
                new Vector2(-5.5f, 11),
                new Vector2(5.5f, 11),
                new Vector2(-6f, 12),
                new Vector2(6f, 12),
                new Vector2(-6.5f, 13),
                new Vector2(6.5f, 13),
                new Vector2(-7f, 14),
                new Vector2(7f, 14)
            };

        /// <summary>
        /// This is an implementation for a filled wedge formation, i.e. the formation
        /// will look like this:
        ///           L
        ///         F   F
        ///       F   F   F
        ///     F   F   F   F
        ///          ...
        /// The order goes like so:
        ///           0
        ///         1   2
        ///       4   3   5
        ///     8   6   7   9
        ///          ...
        /// </summary>
        private static readonly Vector2[] FilledWedgeFormation =
            {
                new Vector2(0, 0),
                new Vector2(-0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, 2),
                new Vector2(-1, 2),
                new Vector2(1, 2),
                new Vector2(-0.5f, 3),
                new Vector2(0.5f, 3),
                new Vector2(-1.5f, 3),
                new Vector2(1.5f, 3),
                new Vector2(0, 4),
                new Vector2(-1, 4),
                new Vector2(1, 4),
                new Vector2(-2, 4),
                new Vector2(2, 4),
                new Vector2(-0.5f, 5),
                new Vector2(0.5f, 5),
                new Vector2(-1.5f, 5),
                new Vector2(1.5f, 5),
                new Vector2(-2.5f, 5),
                new Vector2(2.5f, 5),
                new Vector2(0, 6),
                new Vector2(-1, 6),
                new Vector2(1, 6),
                new Vector2(-2, 6),
                new Vector2(2, 6),
                new Vector2(-3, 6),
                new Vector2(3, 6),
                new Vector2(-0.5f, 7),
                new Vector2(0.5f, 7),
                new Vector2(-1.5f, 7),
                new Vector2(1.5f, 7),
                new Vector2(-2.5f, 7),
                new Vector2(2.5f, 7),
                new Vector2(-3.5f, 7),
                new Vector2(3.5f, 7)
            };

        /// <summary>
        /// This is an implementation for a vee formation, i.e. the formation
        /// will look like this:
        ///          ...
        ///     F           F
        ///       F       F
        ///         F   F
        ///           L
        /// The order goes like so:
        ///          ...
        ///       3       4
        ///         1   2
        ///           0
        /// </summary>
        private static readonly Vector2[] VeeFormation =
            {
                new Vector2(0, 0),
                new Vector2(-0.5f, -1),
                new Vector2(0.5f, -1),
                new Vector2(-1, -2),
                new Vector2(1, -2),
                new Vector2(-1.5f, -3),
                new Vector2(1.5f, -3),
                new Vector2(-2, -4),
                new Vector2(2, -4),
                new Vector2(-2.5f, -5),
                new Vector2(2.5f, -5),
                new Vector2(-3, -6),
                new Vector2(3, -6),
                new Vector2(-3.5f, -7),
                new Vector2(3.5f, -7),
                new Vector2(-4, -8),
                new Vector2(4, -8),
                new Vector2(-4.5f, -9),
                new Vector2(4.5f, -9),
                new Vector2(-5f, -10),
                new Vector2(5f, -10),
                new Vector2(-5.5f, -11),
                new Vector2(5.5f, -11),
                new Vector2(-6f, -12),
                new Vector2(6f, -12),
                new Vector2(-6.5f, -13),
                new Vector2(6.5f, -13),
                new Vector2(-7f, -14),
                new Vector2(7f, -14)
            };

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
        /// Gets or sets the formation the squad keeps.
        /// </summary>
        public FormationType Formation
        {
            get { return _formation; }
            set
            {
                foreach (var member in _members)
                {
                    ((Squad)Manager.GetComponent(member, TypeId))._formation = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the formation spacing, i.e. the space to keep between individual
        /// formation slots. This should at least be as large as the flocking separation
        /// of AI behaviors.
        /// </summary>
        public float FormationSpacing
        {
            get { return _spacing; }
            set
            {
                foreach (var member in _members)
                {
                    ((Squad)Manager.GetComponent(member, TypeId))._spacing = value;
                }
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The list of ships in this squad.
        /// </summary>
        private readonly List<int> _members = new List<int>();

        /// <summary>
        /// The current formation of this squad.
        /// </summary>
        private FormationType _formation = FormationType.Block;

        /// <summary>
        /// The current formation spacing of this squad.
        /// </summary>
        private float _spacing = 200;

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

            // Get our own index in the formation.
            var index = _members.IndexOf(Entity);

            // The position relative to the leader. This will be in a unit scale and
            // scaled based on the spacing set for the squad.
            var position = Vector2.Zero;

            // Figure out which formation we're flying in, if any.
            Vector2[] formation = null;
            switch (Formation)
            {
                case FormationType.Block:
                    formation = BlockFormation;
                    break;
                case FormationType.Column:
                    formation = ColumnFormation;
                    break;
                case FormationType.Line:
                    formation = LineFormation;
                    break;
                case FormationType.Wedge:
                    formation = WedgeFormation;
                    break;
                case FormationType.FilledWedge:
                    formation = FilledWedgeFormation;
                    break;
                case FormationType.Vee:
                    formation = VeeFormation;
                    break;
            }
            if (formation != null && index < formation.Length)
            {
                position = formation[index];
            }

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
            var newMember = (Squad)Manager.GetComponent(entity, TypeId);
            newMember.RemoveMember(entity);
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
            newMember._members.Clear();
            newMember._members.AddRange(_members);
            newMember._formation = _formation;
            newMember._spacing = _spacing;
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
                    ((Squad)Manager.GetComponent(member, TypeId)).RemoveMemberInternal(entity);
                }
            }
            RemoveMemberInternal(entity);

            // Reset the squad component of that entity to a blank squad with only that
            // entity in it.
            ((Squad)Manager.GetComponent(entity, TypeId))._members.Clear();
            ((Squad)Manager.GetComponent(entity, TypeId))._members.Add(entity);
        }

        /// <summary>
        /// Removes the member by replacing it with the last member.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        private void RemoveMemberInternal(int entity)
        {
            // Remove by moving the last member to the removed one's slot.
            // This will reduce flux when a member leaves a formation.
            // Unless the leader is killed, in which case there's flux anyway,
            // so it's actually less noisy to just pick the next member as
            // the new leader.
            if (entity == _members[0])
            {
                _members.RemoveAt(0);
            }
            else
            {
                _members[_members.IndexOf(entity)] = _members[_members.Count - 1];
                _members.RemoveAt(_members.Count - 1);
            }
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
