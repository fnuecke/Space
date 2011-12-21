using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    public class Attributes<TAttribute> : AbstractComponent
        where TAttribute : struct
    {
        #region Packetizer registration

        static Attributes()
        {
            Packetizer.Register<Attributes<TAttribute>>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// A list of all known attributes.
        /// </summary>
        public ReadOnlyCollection<EntityAttribute<TAttribute>> AllAttributes { get { return _attributes.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// Actual list of attributes registered.
        /// </summary>
        private List<EntityAttribute<TAttribute>> _attributes = new List<EntityAttribute<TAttribute>>();

        /// <summary>
        /// Cached computation results for accumulative attribute values.
        /// </summary>
        private Dictionary<TAttribute, Fixed> _cached = new Dictionary<TAttribute, Fixed>();

        /// <summary>
        /// Running counter to uniquely number attributes.
        /// </summary>
        private int _nextAttributeId = 1;

        #endregion

        #region Attributes
        
        /// <summary>
        /// Get the accumulative value of all attributes in this component.
        /// 
        /// <para>
        /// This will result in the same value as calling <c>EntityAttribute.Accumulate</c>,
        /// but will cache the result, so repetitive calls will be faster.
        /// </para>
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes tracked by this component.</returns>
        public Fixed GetValue(TAttribute attributeType)
        {
            if (_cached.ContainsKey(attributeType))
            {
                return _cached[attributeType];
            }
            var result = _attributes.Accumulate(attributeType);
            _cached[attributeType] = result;
            return result;
        }

        /// <summary>
        /// Registers a new attribute with this component.
        /// 
        /// <para>
        /// Note that it is not a good idea to change an attributes type while
        /// it is tracked by this component, as this will break validated
        /// computed accumulative values. In the odd case this is required,
        /// remove the attribute first, then add it again.
        /// </para>
        /// </summary>
        /// <param name="component">The attribute to add.</param>
        public void AddAttribute(EntityAttribute<TAttribute> attribute)
        {
            _attributes.Add(attribute);
            attribute.UID = _nextAttributeId++;
            _cached.Remove(attribute.Type); // Invalidate.
        }

        /// <summary>
        /// Removes an attribute from this component.
        /// </summary>
        /// <param name="component">The attribute to remove.</param>
        public void RemoveAttribute(EntityAttribute<TAttribute> attribute)
        {
            if (attribute.UID < 1)
            {
                return;
            }
            RemoveAttribute(attribute.UID);
        }

        /// <summary>
        /// Removes an attribute by its id from this component.
        /// </summary>
        /// <param name="attributeUid">The id of the attribute to remove.</param>
        /// <returns>The removed attribute, or <c>null</c> if this component
        /// has no attribute with the specified id.</returns>
        public EntityAttribute<TAttribute> RemoveAttribute(int attributeUid)
        {
            if (attributeUid > 0)
            {
                int index = _attributes.FindIndex(a => a.UID == attributeUid);
                if (index >= 0)
                {
                    var attribute = _attributes[index];
                    _attributes.RemoveAt(index);
                    attribute.UID = -1;
                    _cached.Remove(attribute.Type);
                    return attribute;
                }
            }
            return null;
        }

        #endregion

        #region Serialization / Hashing

        public override Serialization.Packet Packetize(Packet packet)
        {
            return base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
        }

        public override object Clone()
        {
            var copy = (Attributes<TAttribute>)base.Clone();
            copy._attributes = new List<EntityAttribute<TAttribute>>();
            foreach (var attribute in _attributes)
            {
                copy._attributes.Add((EntityAttribute<TAttribute>)attribute.Clone());
            }
            copy._cached = new Dictionary<TAttribute, Fixed>(_cached);
            return copy;
        }

        #endregion
    }
}
