using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Engine.Diagnostics
{
    /// <summary>
    /// Utility proxy for easier viewing of deep hierarchy objects in debugger.
    /// </summary>
    public sealed class FlattenHierarchyProxy
    {
        /// <summary>
        /// Entries representing members, to allow further inspection.
        /// </summary>
        [DebuggerDisplay("{Value}", Name = "{Name,nq}", Type = "{Type.ToString(),nq}")]
        private struct Member
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal string Name;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal Type Type;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            internal object Value;
        }

        /// <summary>
        /// The object we're a proxy for.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _target;

        /// <summary>
        /// Flat list of members of the target.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Member[] _memberList;

        /// <summary>
        /// Lazy initialization of member list. This is what's actually shown in the debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private Member[] Items
        {
            get { return _memberList ?? (_memberList = BuildMemberList().ToArray()); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlattenHierarchyProxy"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public FlattenHierarchyProxy(object target)
        {
            _target = target;
        }

        /// <summary>
        /// Lazy initialization of the list representing the flat hierarchy of our target.
        /// </summary>
        /// <returns>The flat list of fields and properties of our target.</returns>
        private List<Member> BuildMemberList()
        {
            // If the target is null return null.
            if (_target == null)
            {
                return null;
            }

            // Else build the list.
            var list = new List<Member>();

            // Get all public and non public fields of the object.
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = _target.GetType();
            foreach (var field in type.GetFields(flags))
            {
                Member member;
                member.Name = field.Name;
                member.Type = field.FieldType;
                try
                {
                    member.Value = field.GetValue(_target);
                }
                catch (Exception ex)
                {
                    member.Value = ex;
                }
                list.Add(member);
            }

            // Get all public and non-public properties of the object.
            foreach (var prop in type.GetProperties(flags))
            {
                Member member;
                member.Name = prop.Name;
                member.Type = prop.PropertyType;
                try
                {
                    member.Value = prop.GetValue(_target, null);
                }
                catch (Exception ex)
                {
                    member.Value = ex;
                }
                list.Add(member);
            }

            return list;
        }
    }
}
