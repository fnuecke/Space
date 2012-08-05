using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Engine.Collections
{
    /// <summary>
    /// Dictionary wrapper that supports XML serialization.
    /// </summary>
    /// <typeparam name="TKey">The type of key stored in this dictionary.</typeparam>
    /// <typeparam name="TVal">The type of value stored in this dictionary.</typeparam>
    [Serializable]
    public sealed class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable
    {
        #region Properties

        /// <summary>
        /// Lazy initialization of serializer used for keys mapping values stored in the dictionary.
        /// </summary>
        private XmlSerializer KeySerializer
        {
            get { return _keySerializer ?? (_keySerializer = new XmlSerializer(typeof(TKey))); }
        }

        /// <summary>
        /// Lazy initialization of serializer used for values stored in the dictionary.
        /// </summary>
        private XmlSerializer ValueSerializer
        {
            get { return _valueSerializer ?? (_valueSerializer = new XmlSerializer(typeof(TVal))); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Serializer used for keys mapping to values stored in the dictionary.
        /// </summary>
        private XmlSerializer _keySerializer;

        /// <summary>
        /// Serializer used for values stored in the dictionary.
        /// </summary>
        private XmlSerializer _valueSerializer;

        #endregion

        #region Constructor

        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TVal> dictionary)
            : base(dictionary)
        {
        }

        public SerializableDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
        }

        public SerializableDictionary(int capacity)
            : base(capacity)
        {
        }

        public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer)
        {
        }

        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer)
        {
        }

        #endregion

        #region ISerializable Members

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDictionary&lt;TKey, TVal&gt;"/>
        /// class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        private SerializableDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            var itemCount = info.GetInt32("itemsCount");
            for (var i = 0; i < itemCount; i++)
            {
                var kvp = (KeyValuePair<TKey, TVal>)info.GetValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", i), typeof(KeyValuePair<TKey, TVal>));
                Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Implements the <see cref="T:System.Runtime.Serialization.ISerializable"/> interface and
        /// returns the data needed to serialize the <see cref="T:System.Collections.Generic.Dictionary`2"/>
        /// instance.
        /// </summary>
        /// <param name="info">A <see cref="T:System.Runtime.Serialization.SerializationInfo"/>
        /// object that contains the information required to serialize the <see cref="T:System.Collections.Generic.Dictionary`2"/>
        /// instance.</param>
        /// <param name="context">A <see cref="T:System.Runtime.Serialization.StreamingContext"/>
        /// structure that contains the source and destination of the serialized stream associated
        /// with the <see cref="T:System.Collections.Generic.Dictionary`2"/> instance.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="info"/> is null.
        ///   </exception>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("itemsCount", Count);
            var itemIdx = 0;
            foreach (KeyValuePair<TKey, TVal> kvp in this)
            {
                info.AddValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TVal>));
                itemIdx++;
            }
        }
        #endregion

        #region IXmlSerializable Members

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the
        /// object is serialized.</param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (KeyValuePair<TKey, TVal> kvp in this)
            {
                KeySerializer.Serialize(writer, kvp.Key);
                ValueSerializer.Serialize(writer, kvp.Value);
            }
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the
        /// object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }
            // Move past container
            if (reader.NodeType == XmlNodeType.Element && !reader.Read())
            {
                throw new XmlException("Error in Deserialization of SerializableDictionary");
            }
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var key = (TKey)KeySerializer.Deserialize(reader);
                var value = (TVal)ValueSerializer.Deserialize(reader);
                Add(key, value);
                reader.MoveToContent();
            }
            // Move past container
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                reader.ReadEndElement();
            }
            else
            {
                throw new XmlException("Error in Deserialization of SerializableDictionary");
            }
        }

        /// <summary>
        /// This method is reserved and should not be used. When implementing the IXmlSerializable
        /// interface, you should return null (Nothing in Visual Basic) from this method, and
        /// instead, if specifying a custom schema is required, apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"/>
        /// to the class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"/> that describes the XML representation
        /// of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"/>
        /// method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"/>
        /// method.
        /// </returns>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        #endregion
    }
}
