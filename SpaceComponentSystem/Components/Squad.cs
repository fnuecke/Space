using System;
using System.Collections.Generic;
using System.Linq;
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
            /// Line formation, i.e. units arrange in single column.
            /// </summary>
            Line,

            /// <summary>
            /// A column formation, i.e. units arrange in a jagged line.
            /// </summary>
            Column,

            /// <summary>
            /// Vee formation, i.e. units align in an inverse open triangular shape (V).
            /// </summary>
            Vee,

            /// <summary>
            /// Wedge formation, i.e. units align in an open, triangular shape (inverse V).
            /// </summary>
            Wedge,

            /// <summary>
            /// Filled wedge formation, i.e. units align in a closed triangular shape.
            /// </summary>
            FilledWedge,

            /// <summary>
            /// A block formation, i.e. units arrange in a rectangular shape.
            /// </summary>
            Block,

            /// <summary>
            /// A Sierpinski formation, i.e. units arrange in a fractal trianglular shape with holes.
            /// </summary>
            Sierpinski
        }

        #endregion

        #region Types

        /// <summary>
        /// Helper class for formation implementations, taking care of result
        /// caching for fast lookups.
        /// </summary>
        private sealed class CachedFormation
        {
            /// <summary>
            /// The formation implementation.
            /// </summary>
            private readonly Func<int, Vector2> _formation; 

            /// <summary>
            /// Internal cache of already-computed coordiantes. This simply
            /// holds the coordinate at the corresponding index.
            /// </summary>
            private readonly List<Vector2> _cache = new List<Vector2>();

            /// <summary>
            /// Initializes a new instance of the <see cref="CachedFormation"/> class.
            /// </summary>
            /// <param name="formation">The formationimplementation. This
            /// should compute the relative offset to the leading squad
            /// member, based on the index the member has inside the squad.
            /// The result must be in unit scale (i.e. it will be scaled by
            /// the squad's <see cref="Squad.FormationSpacing"/> property to
            /// get the final position.
            /// </param>
            public CachedFormation(Func<int, Vector2> formation)
            {
                _formation = formation;
            }

            /// <summary>
            /// Gets the relative position of the squad member at the specified
            /// offset to the leader of the squad. This is in unit scale.
            /// </summary>
            /// <param name="index">The index of the squad member.</param>
            /// <returns></returns>
            public Vector2 GetOffset(int index)
            {
                // Build cache for missing items up to requested one, if necessary.
                for (var i = _cache.Count; i <= index; i++)
                {
                    _cache.Add(_formation(i));
                }
                // Return cached coordinate.
                return _cache[index];
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// The list of available formations, mapping enum to implementation.
        /// Therefore they must be in the exact same order as the corresponding
        /// enum declaration. These are ordered roughly by formation complexity.
        /// </summary>
        private static readonly CachedFormation[] Formations = new[]
        {
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

            // This is the 'null' implementation, which simply returns zero.
            new CachedFormation(index => Vector2.Zero),
            
            // This is an implementation for a line formation, i.e. the formation
            // will look like this:
            //           L
            //           F
            //           F
            //          ...
            // The order goes like so:
            //           0
            //           1
            //           2
            //          ...
            new CachedFormation(index => new Vector2(0, index)),

            // This is an implementation for a column formation, i.e. the formation
            // will look like this:
            //           L
            //         F
            //           F
            //         F
            //          ...
            // The order goes like so:
            //           0
            //         1
            //           2
            //          ...
            new CachedFormation(index => new Vector2(-(index & 1), index)),

            // This is an implementation for a vee formation, i.e. the formation
            // will look like this:
            //          ...
            //     F           F
            //       F       F
            //         F   F
            //           L
            // The order goes like so:
            //          ...
            //       3       4
            //         1   2
            //           0
            new CachedFormation(index =>
            {
                var k = index + 1;
                return new Vector2((k >> 1) * (((k & 1) == 0) ? -0.5f : 0.5f), -(k >> 1));
            }),

            // This is an implementation for an open wedge formation, i.e. the formation
            // will look like this:
            //           L
            //         F   F
            //       F       F
            //     F           F
            //          ...
            // The order goes like so:
            //           0
            //         1   2
            //       3       4
            //          ...
            new CachedFormation(index =>
            {
                var k = index + 1;
                return new Vector2((k >> 1) * (((k & 1) == 0) ? -0.5f : 0.5f), k >> 1);
            }),

            // This is an implementation for a filled wedge formation, i.e. the formation
            // will look like this:
            //           L
            //         F   F
            //       F   F   F
            //     F   F   F   F
            //          ...
            // The order goes like so:
            //           0
            //         1   2
            //       4   3   5
            //     8   6   7   9
            //          ...
            new CachedFormation(index =>
            {
                var k = index + 1;
                var h = 0;
                while ((((h + 1) * ((h + 1) + 1)) >> 1) < k)
                {
                    ++h;
                }
                var i = k - ((h * (h + 1)) >> 1);
                var x = (((i & 1) == 0) ? 2 : -2) * (i / 2) - (h & 1);
                return new Vector2(0.5f * x, h);
            }),

            // This is an implementation for a block formation, i.e. the formation will look
            // like this:
            //  F  F  L  F  F
            //  F  F  F  F  F
            //       ...
            // The balance that a formation is determined how it expands: it toggles between
            // vertical and horizontal expansion whenever the formation becomes "full". So
            // in numbers:
            // 6 1 0 2 7
            // 8 4 3 5 9
            //    ...
            new CachedFormation(index =>
            {
                var k = index + 1;
                var h = 0;
                while ((h + 1) * Math.Max(0, 2 * (h + 1) - 1) < k)
                {
                    ++h;
                }
                var i = k - h * Math.Max(0, 2 * h - 1) - 1;
                var j = i - 2 * h + 1;
                var y = Math.Min(h, i >> 1);
                var x = (h > y)
                            ? (((i & 1) == 0) ? h : -h)
                            : ((((j & 1) == 0) ? (j >> 1) : -(j >> 1)));
                return new Vector2(x, y);
            }),

            // This in an implementation for a Sierpinski formation. See
            // https://en.wikipedia.org/wiki/Sierpinski_triangle
            new CachedFormation(index =>
                                // Fetch our raw data from the filled wedge formation. We
                                // it as our "blueprint" from which filter out some entries
                                // (the "holes" in the Sierpinski triangle).
                                Enumerable.Range(0, int.MaxValue)
                                    .Select(x => Formations[(int)FormationType.FilledWedge].GetOffset(x))
                                    // Translate coordinates back to be full integers and all positive.
                                    .Select(v => Tuple.Create((int)Math.Round(v.X + v.Y / 2), (int)v.Y))
                                    // Transform to rectangluar space (offset y to x axis).
                                    .Select(t => Tuple.Create(t.Item1, t.Item2 - t.Item1))
                                    // Because then we can wave our magic wand...
                                    .Select(t => (t.Item1 & t.Item2) == 0)
                                    // Merge it with our original filled wedge list again, so that we get
                                    // the association (bool keep, Vector2 entry).
                                    .Zip(Enumerable.Range(0, int.MaxValue)
                                             .Select(x => Formations[(int)FormationType.FilledWedge].GetOffset(x)), Tuple.Create)
                                    // Then filter by that association.
                                    .Where(t => t.Item1).Select(t => t.Item2)
                                    // And finally, return the element at the desired location.
                                    .ElementAt(index))
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
        /// Gets a list of all the members in this squad.
        /// </summary>
        public IEnumerable<int> Members
        {
            get { return _members; }
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
            var position = Formations[(int)Formation].GetOffset(index);

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
