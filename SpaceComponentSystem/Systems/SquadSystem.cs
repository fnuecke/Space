using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Cleans up squads if a squad component is removed.</summary>
    public sealed class SquadSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields

        /// <summary>
        ///     IDs for squads (i.e. each squad gets its own ID by which is is referenced in the squad components of the
        ///     entities in that squad).
        /// </summary>
        private IdManager _squadIds = new IdManager();

        /// <summary>The list of actual squads, mapping squad id to squad data.</summary>
        [CopyIgnore, PacketizerIgnore]
        private SparseArray<SquadData> _squads = new SparseArray<SquadData>();

        #endregion

        #region Logic

        /// <summary>Determines whether the specified squad exists.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>
        ///     <c>true</c> if the specified squad exists; otherwise, <c>false</c>.
        /// </returns>
        public bool HasSquad(int squad)
        {
            return _squadIds.InUse(squad);
        }

        /// <summary>Determines whether the squad contains the specified entity.</summary>
        /// <param name="squad">The squad to check for.</param>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>
        ///     <c>true</c> if the squad contains the specified entity; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(int squad, int entity)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Members.Contains(entity);
        }

        /// <summary>Gets the leader of the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>The leader of the squad.</returns>
        public int GetLeader(int squad)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Members[0];
        }

        /// <summary>Gets the members of the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>The members of the squad.</returns>
        public IEnumerable<int> GetMembers(int squad)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Members;
        }

        /// <summary>Gets the number of members of the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>The number of squad members.</returns>
        public int GetCount(int squad)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Members.Count;
        }

        /// <summary>Gets the formation for the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>The formation type of that squad.</returns>
        public AbstractFormation GetFormation(int squad)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Formation;
        }

        /// <summary>Sets the formation of the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <param name="value">The formation to set.</param>
        public void SetFormation(int squad, AbstractFormation value)
        {
            Debug.Assert(HasSquad(squad));
            _squads[squad].Formation = value;
            _squads[squad].Cache = new FormationCache(value);
        }

        /// <summary>Gets the formation spacing for the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <returns>The formation spacing of that squad.</returns>
        public float GetFormationSpacing(int squad)
        {
            Debug.Assert(HasSquad(squad));
            return _squads[squad].Spacing;
        }

        /// <summary>Sets the formation spacing of the specified squad.</summary>
        /// <param name="squad">The squad.</param>
        /// <param name="value">The formation spacing to set.</param>
        public void SetFormationSpacing(int squad, float value)
        {
            Debug.Assert(HasSquad(squad));
            _squads[squad].Spacing = value;
        }

        /// <summary>
        ///     Adds a new member to this squad. Note that this will automatically register the entity with the squad
        ///     component of all other already- members of this squad.
        /// </summary>
        /// <param name="squad">The squad to add to.</param>
        /// <param name="entity">The entity to add to the squad.</param>
        public void AddMember(int squad, int entity)
        {
            Debug.Assert(HasSquad(squad));

            // Skip if the entity is already there.
            if (_squads[squad].Members.Contains(entity))
            {
                return;
            }

            // Make sure the entity isn't in a squad (except the identity squad,
            // into which it is moved if removed from another one).
            var memberSquad = (Squad) Manager.GetComponent(entity, Squad.TypeId);
            RemoveMember(memberSquad.SquadId, entity);

            // Remove the identity squad.
            _squadIds.ReleaseId(memberSquad.SquadId);

            // Register that entity.
            _squads[squad].Members.Add(entity);
            memberSquad.SquadId = squad;
        }

        /// <summary>
        ///     Removes an entity from this squad. Note that this will automatically remove the entity from the squad
        ///     components of all other members.
        /// </summary>
        /// <param name="squad">The squad to remove from.</param>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveMember(int squad, int entity)
        {
            Debug.Assert(HasSquad(squad));

            // Error if there's no such member in this squad.
            var members = _squads[squad].Members;
            if (!members.Contains(entity))
            {
                throw new ArgumentException(@"No such member in this squad.", "entity");
            }

            // If we're already in an identity squad we can skip this.
            if (members.Count == 1)
            {
                return;
            }

            // Remove by moving the last member to the removed one's slot.
            // This will reduce flux when a member leaves a formation.
            // Unless the leader is killed, in which case there's flux anyway,
            // so it's actually less noisy to just pick the next member as
            // the new leader.
            if (entity == members[0])
            {
                members.RemoveAt(0);
            }
            else
            {
                members[members.IndexOf(entity)] = members[members.Count - 1];
                members.RemoveAt(members.Count - 1);
            }

            // Put the entity into its own identity squad.
            MakeIdentitySquad((Squad) Manager.GetComponent(entity, Squad.TypeId));
        }

        /// <summary>Gets the position of the specified member in the squad formation (i.e. where it should be at this time).</summary>
        /// <param name="squad">The squad.</param>
        /// <param name="entity">The squad member.</param>
        /// <returns></returns>
        public FarPosition ComputeFormationOffset(int squad, int entity)
        {
            if (!Contains(squad, entity))
            {
                throw new ArgumentException(@"No such member in this squad.", "entity");
            }

            var data = _squads[squad];

            var leaderTransform = (Transform) Manager.GetComponent(data.Members[0], Transform.TypeId);

            // Get our own index in the formation.
            var index = data.Members.IndexOf(entity);

            // The position relative to the leader. This will be in a unit scale and
            // scaled based on the spacing set for the squad.
            var position = data.Cache[index];

            // Rotate around origin of the formation (which should be the leader's position in
            // most cases).
            var finalPosition = leaderTransform.Translation;
            var cosRadians = (float) Math.Cos(leaderTransform.Rotation);
            var sinRadians = (float) Math.Sin(leaderTransform.Rotation);
            finalPosition.X += (-position.Y * cosRadians - position.X * sinRadians) * data.Spacing;
            finalPosition.Y += (-position.Y * sinRadians + position.X * cosRadians) * data.Spacing;

            return finalPosition;
        }

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            base.OnComponentAdded(component);

            var cc = component as Squad;
            if (cc != null)
            {
                MakeIdentitySquad(cc);
            }
        }

        /// <summary>Called when a component is removed.</summary>
        /// <param name="component">The component.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            var cc = component as Squad;
            if (cc != null)
            {
                // Remove from squad moving it to its identity squad.
                RemoveMember(cc.SquadId, component.Entity);
                // Remove the component's identity squad.
                _squadIds.ReleaseId(cc.SquadId);
            }
        }

        /// <summary>Creates a new squad, only containing the entity of the specified squad component.</summary>
        /// <param name="squad">The squad.</param>
        private void MakeIdentitySquad(Squad squad)
        {
            // Get id and data slot, reset the data slot.
            var identitySquad = _squadIds.GetId();
            if (_squads[identitySquad] == null)
            {
                _squads[identitySquad] = new SquadData();
            }
            _squads[identitySquad].Members.Clear();
            _squads[identitySquad].Formation = Formations.None;
            _squads[identitySquad].Spacing = 200;
            _squads[identitySquad].Cache = new FormationCache(Formations.None);

            // Add member to squad and let it know it's in it.
            _squads[identitySquad].Members.Add(squad.Entity);
            squad.SquadId = identitySquad;
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (SquadSystem) base.NewInstance();

            copy._squadIds = new IdManager();
            copy._squads = new SparseArray<SquadData>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (SquadSystem) into;

            foreach (var id in _squadIds)
            {
                copy._squads[id] = new SquadData
                {
                    Formation = _squads[id].Formation,
                    Spacing = _squads[id].Spacing,
                    Cache = new FormationCache(_squads[id].Formation)
                };
                copy._squads[id].Members.AddRange(_squads[id].Members);
            }
        }

        #endregion

        #region Serialization

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            foreach (var id in _squadIds)
            {
                packet.Write(id);
                var data = _squads[id];
                packet.WriteWithTypeInfo(data.Formation);
                packet.Write(data.Spacing);
                packet.Write(data.Members.Count);
                for (var i = 0; i < data.Members.Count; i++)
                {
                    packet.Write(data.Members[i]);
                }
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            for (var i = 0; i < _squadIds.Count; i++)
            {
                var id = packet.ReadInt32();
                var data = _squads[id] = new SquadData();
                data.Formation = packet.ReadPacketizableWithTypeInfo<AbstractFormation>();
                data.Cache = new FormationCache(data.Formation);
                data.Spacing = packet.ReadSingle();
                var count = packet.ReadInt32();
                for (var j = 0; j < count; j++)
                {
                    data.Members.Add(packet.ReadInt32());
                }
            }
        }

        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Squads = {");
            foreach (var id in _squadIds)
            {
                var data = _squads[id];

                w.AppendIndent(indent + 1).Write(id);
                w.Write(" = {");
                w.AppendIndent(indent + 2).Write("Formation = ");
                w.Write(data.Formation.GetType().Name);
                w.AppendIndent(indent + 2).Write("Spacing = ");
                w.Write(data.Spacing);
                w.AppendIndent(indent + 2).Write("MemberCount = ");
                w.Write(data.Members.Count);
                w.AppendIndent(indent + 2).Write("Members = {");
                for (var i = 0; i < data.Members.Count; i++)
                {
                    if (i > 0)
                    {
                        w.Write(", ");
                    }
                    w.Write(data.Members[i]);
                }
                w.AppendIndent(indent + 2).Write("}");
                w.AppendIndent(indent + 1).Write("}");
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Types

        /// <summary>
        ///     Base class defining the interface all formation implementations must implement. Formations are implemented by
        ///     providing an enumerator over the positions of the single members of a squad.
        /// </summary>
        public abstract class AbstractFormation : IEnumerable<Vector2>, IPacketizable
        {
            /// <summary>Returns an enumerator that iterates through the collection.</summary>
            /// <returns>
            ///     A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public abstract IEnumerator<Vector2> GetEnumerator();

            /// <summary>Returns an enumerator that iterates through a collection.</summary>
            /// <returns>
            ///     An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>The list of available default formation implementations.</summary>
        public static class Formations
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

            /// <summary>
            ///     This is the 'null' implementation, which simply returns nothing and causes the cache to fall-back to the
            ///     default value.
            /// </summary>
            public static readonly AbstractFormation None = new NoneFormation();

            private sealed class NoneFormation : SimpleFormation
            {
                public NoneFormation() : base(new Vector2[0]) {}
            }

            /// <summary>
            ///     This is an implementation for a line formation, i.e. the formation will look like this:
            ///     <code>
            ///       L
            ///       F
            ///       F
            ///      ...
            ///     </code>
            ///     <para/>
            ///     The order goes like so:
            ///     <code>
            ///       0
            ///       1
            ///       2
            ///      ...
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation Line = new LineFormation();

            private sealed class LineFormation : SimpleFormation
            {
                public LineFormation() : base(
                    Enumerable.Range(0, int.MaxValue)
                              .Select(i => new Vector2(0, i))) {}
            }

            /// <summary>
            ///     This is an implementation for a column formation, i.e. the formation will look like this:
            ///     <code>
            ///       L
            ///     F
            ///       F
            ///     F
            ///      ...
            ///     </code>
            ///     <para/>
            ///     The order goes like so:
            ///     <code>
            ///       0
            ///     1
            ///       2
            ///      ...
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation Column = new ColumnFormation();

            private sealed class ColumnFormation : SimpleFormation
            {
                public ColumnFormation() : base(
                    Enumerable.Range(0, int.MaxValue)
                              .Select(i => new Vector2(-(i & 1), i))) {}
            }

            /// <summary>
            ///     This is an implementation for a vee formation, i.e. the formation will look like this:
            ///     <code>
            ///      ...
            /// F           F
            ///   F       F
            ///     F   F
            ///       L
            ///     </code>
            ///     <para/>
            ///     The order goes like so:
            ///     <code>
            ///      ...
            ///   3       4
            ///     1   2
            ///       0
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation Vee = new VeeFormation();

            private sealed class VeeFormation : SimpleFormation
            {
                public VeeFormation() : base(
                    Enumerable.Range(0, int.MaxValue)
                              .Select(i => i + 1)
                              .Select(i => new Vector2((i >> 1) * (((i & 1) == 0) ? -0.5f : 0.5f), -(i >> 1)))) {}
            }

            /// <summary>
            ///     This is an implementation for an open wedge formation, i.e. the formation will look like this:
            ///     <code>
            ///       L
            ///     F   F
            ///   F       F
            /// F           F
            ///      ...
            ///     </code>
            ///     <para/>
            ///     The order goes like so:
            ///     <code>
            ///       0
            ///     1   2
            ///   3       4
            ///      ...
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation Wedge = new WedgeFormation();

            private sealed class WedgeFormation : SimpleFormation
            {
                public WedgeFormation() : base(
                    Enumerable.Range(0, int.MaxValue)
                              .Select(i => i + 1)
                              .Select(i => new Vector2((i >> 1) * (((i & 1) == 0) ? -0.5f : 0.5f), i >> 1))) {}
            }

            /// <summary>
            ///     This is an implementation for a filled wedge formation, i.e. the formation will look like this:
            ///     <code>
            ///       L
            ///     F   F
            ///   F   F   F
            /// F   F   F   F
            ///      ...
            ///     </code>
            ///     <para/>
            ///     The order goes like so:
            ///     <code>
            ///       0
            ///     1   2
            ///   4   3   5
            /// 8   6   7   9
            ///      ...
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation FilledWedge = new FilledWedgeFormation();

            private sealed class FilledWedgeFormation : SimpleFormation
            {
                public FilledWedgeFormation() : base(
                    FilledWedgeBase.Select(t => new Vector2(0.5f * t.Item1, t.Item2))) {}
            }

            /// <summary>
            ///     This is an implementation for a block formation, i.e. the formation will look like this:
            ///     <code>
            ///  F  F  L  F  F
            ///  F  F  F  F  F
            ///       ...
            ///     </code>
            ///     <para/>
            ///     The balance that a formation is determined how it expands: it toggles between vertical and horizontal expansion
            ///     whenever the formation becomes "full". So in numbers:
            ///     <code>
            /// 6 1 0 2 7
            /// 8 4 3 5 9
            ///    ...
            ///     </code>
            /// </summary>
            public static readonly AbstractFormation Block = new BlockFormation();

            private sealed class BlockFormation : SimpleFormation
            {
                public BlockFormation() : base(BlockBase) {}
            }

            /// <summary>This in an implementation for a Sierpinski formation. See https://en.wikipedia.org/wiki/Sierpinski_triangle</summary>
            public static readonly AbstractFormation Sierpinski = new SierpinskiFormation();

            private sealed class SierpinskiFormation : SimpleFormation
            {
                public SierpinskiFormation() : base(
                    // Fetch our raw data from the filled wedge formation. We
                    // it as our "blueprint" from which filter out some entries
                    // (the "holes" in the Sierpinski triangle).
                    FilledWedgeBase
                        // Translate coordinates back to be full integers and all positive.
                        .Select(t => Tuple.Create((t.Item1 + t.Item2) / 2, t.Item2))
                        // Transform to rectangular space (offset y to x axis).
                        .Select(t => Tuple.Create(t.Item1, t.Item2 - t.Item1))
                        // Because then we can wave our magic wand...
                        .Select(t => (t.Item1 & t.Item2) == 0)
                        // Merge it with our original filled wedge list again, so that we get
                        // the association (bool keep, Vector2 entry).
                        .Zip(FilledWedge, Tuple.Create)
                        // Then filter by that association.
                        .Where(t => t.Item1).Select(t => t.Item2)) {}
            }

            // ReSharper disable FunctionNeverReturns Infinite generators.

            private static IEnumerable<Tuple<int, int>> FilledWedgeBase
            {
                get
                {
                    yield return Tuple.Create(0, 0);

                    var k = 0;
                    var line = 1;
                    for (;;)
                    {
                        var position = (((k & 1) == 0) ? -2 : 2) * ((k + 1) >> 1) - (line & 1);

                        yield return Tuple.Create(position, line);

                        var nk = (k + 1) % (line + 1);
                        var nl = line + k / line;

                        k = nk;
                        line = nl;
                    }
                }
            }

            private static IEnumerable<Vector2> BlockBase
            {
                get
                {
                    var h = 0;
                    var k = 0;
                    for (;;)
                    {
                        var l = k - 2 * h + 1;
                        var r = Math.Min(h, k >> 1);
                        var c = h > r
                                    ? (((k & 1) == 0) ? -h : h)
                                    : (((l & 1) == 0) ? -(l >> 1) : (l >> 1));

                        yield return new Vector2(c, r);

                        var s = 4 * h + 1;
                        h = h + (k + 1) / s;
                        k = (k + 1) % s;
                    }
                }
            }

            // ReSharper restore FunctionNeverReturns

            /// <summary>A simple wrapper for parameterless formation implementations.</summary>
            private class SimpleFormation : AbstractFormation
            {
                [PacketizerIgnore]
                private readonly IEnumerable<Vector2> _enumerable;

                protected SimpleFormation(IEnumerable<Vector2> formation)
                {
                    _enumerable = formation;
                }

                public override IEnumerator<Vector2> GetEnumerator()
                {
                    return _enumerable.GetEnumerator();
                }
            }
        }

        /// <summary>Tracks information about a single squad.</summary>
        private sealed class SquadData
        {
            /// <summary>The list of ships in this squad.</summary>
            public readonly List<int> Members = new List<int>();

            /// <summary>The current formation of this squad.</summary>
            public AbstractFormation Formation;

            /// <summary>The current formation spacing of this squad.</summary>
            public float Spacing;

            /// <summary>The cache used for position lookups in the current formation.</summary>
            public FormationCache Cache;
        }

        /// <summary>Helper class for formation implementations, taking care of result caching for fast lookups.</summary>
        private sealed class FormationCache
        {
            /// <summary>The formation implementation.</summary>
            private readonly IEnumerator<Vector2> _formation;

            /// <summary>Internal cache of already-computed coordinates. This simply holds the coordinate at the corresponding index.</summary>
            private readonly List<Vector2> _cache = new List<Vector2>();

            /// <summary>
            ///     Initializes a new instance of the <see cref="FormationCache"/> class.
            /// </summary>
            /// <param name="formation">
            ///     The formation implementation. This should compute the relative offset to the leading squad member, based on the
            ///     index the member has inside the squad. The result must be in unit scale (i.e. it will be scaled by the squad's
            ///     <see cref="Squad.FormationSpacing"/> property to get the final position.
            /// </param>
            public FormationCache(IEnumerable<Vector2> formation)
            {
                _formation = formation.GetEnumerator();
            }

            /// <summary>
            ///     Gets the relative position of the squad member at the specified offset to the leader of the squad. This is in
            ///     unit scale.
            /// </summary>
            /// <param name="index">The index of the squad member.</param>
            /// <returns></returns>
            public Vector2 this[int index]
            {
                get
                {
                    // Build cache for missing items up to requested one, if necessary.
                    lock (this)
                    {
                        for (var i = _cache.Count; i <= index && _formation.MoveNext(); i++)
                        {
                            _cache.Add(_formation.Current);
                        }
                    }
                    // Return cached coordinate.
                    return _cache.Count > index ? _cache[index] : Vector2.Zero;
                }
            }
        }

        #endregion
    }
}