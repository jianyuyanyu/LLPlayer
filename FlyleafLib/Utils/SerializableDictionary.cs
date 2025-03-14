﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;

namespace FlyleafLib;

public static partial class Utils
{
    [Serializable] // https://scatteredcode.net/c-serializable-dictionary/
    public class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Private Properties
        protected XmlSerializer ValueSerializer => _valueSerializer ??= new XmlSerializer(typeof(TVal));
        private XmlSerializer KeySerializer => _keySerializer ??= new XmlSerializer(typeof(TKey));
        #endregion
        #region Private Members
        private XmlSerializer _keySerializer;
        private XmlSerializer _valueSerializer;
        #endregion
        #region Constructors
        public SerializableDictionary()
        {
        }
        public SerializableDictionary(IDictionary<TKey, TVal> dictionary) : base(dictionary) { }
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public SerializableDictionary(int capacity) : base(capacity) { }
        public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer) { }
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer) { }
        #endregion
        #region ISerializable Members
        protected SerializableDictionary(SerializationInfo info, StreamingContext context)
        {
            int itemCount = info.GetInt32("itemsCount");
            for (int i = 0; i < itemCount; i++)
            {
                KeyValuePair<TKey, TVal> kvp = (KeyValuePair<TKey, TVal>)info.GetValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", i), typeof(KeyValuePair<TKey, TVal>));
                Add(kvp.Key, kvp.Value);
            }
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("itemsCount", Count);
            int itemIdx = 0; foreach (var kvp in this)
            {
                info.AddValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TVal>));
                itemIdx++;
            }
        }
        #endregion
        #region IXmlSerializable Members
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (var kvp in this)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                KeySerializer.Serialize(writer, kvp.Key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                ValueSerializer.Serialize(writer, kvp.Value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }
            // Move past container
            if (reader.NodeType == XmlNodeType.Element && !reader.Read())
                throw new XmlException("Error in De serialization of SerializableDictionary");
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                TKey key = (TKey)KeySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                TVal value = (TVal)ValueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadEndElement();
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
        XmlSchema IXmlSerializable.GetSchema() => null;
        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public new TVal this[TKey key]
        {
            get => base[key];

            set
            {
                if (ContainsKey(key) && base[key].Equals(value)) return;

                if (CollectionChanged != null)
                {
                    KeyValuePair<TKey, TVal> oldItem = new(key, base[key]);
                    KeyValuePair<TKey, TVal> newItem = new(key, value);
                    base[key] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key.ToString()));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, this.ToList().IndexOf(newItem)));
                }
                else
                {
                    base[key] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key.ToString()));
                }
            }
        }
    }
}
