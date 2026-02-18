///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: 
//    Status:      RELEASE
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 08. Mai 2007 Matthias Gruber original version
//     Freitag, 11. Mai 2007 Matthias Gruber added constructor(int) and constructor()
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using MatthiasToolbox.Passwords.Utilities;
using Hashtable = System.Collections.Hashtable;

namespace MatthiasToolbox.Passwords
{
    /// <summary>
    /// A generic, serializable wrapper for System.Collections.Hashtable, which represents a collection of key/value pairs that are organized based on the hash code of the key.
    /// </summary>
    /// <typeparam name="TValue">type of the values</typeparam>
    [Serializable]
    public class Hashtable<TValue> : Hashtable, IXmlSerializable
    {
        #region IXmlSerializable members

        /// <summary>
        /// a schema is not retrievable in this verion
        /// </summary>
        /// <returns>null</returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// add data from a XmlReader
        /// </summary>
        /// <param name="reader">System.Xml.XmlReader containing data which was serialized by this class</param>
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();
            if (wasEmpty) return;
            if(reader.NodeType == XmlNodeType.None) return;

            while (reader.NodeType != XmlNodeType.None && reader.NodeType != XmlNodeType.EndElement)
            {
                
                    reader.ReadStartElement("item");
                    reader.ReadStartElement("key");

                    String key = (String)keySerializer.Deserialize(reader);

                    reader.ReadEndElement();
                    reader.ReadStartElement("value");

                    TValue value = (TValue)valueSerializer.Deserialize(reader);

                    reader.ReadEndElement();

                    this.Add(Crypto.DecryptSecureString(Crypto.DecryptRSA(key, Program.passUser)), value);
                    
                    reader.ReadEndElement();
                    reader.ReadEndElement();
                
                    // reader.MoveToContent();
                
            }
            
                // try{reader.ReadEndElement();}
           
        }

        /// <summary>
        /// write contents to xml
        /// </summary>
        /// <param name="writer">a System.Xml.XmlWriter</param>
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (String key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");

                keySerializer.Serialize(writer, Crypto.EncryptRSA(key, Program.passUser));

                writer.WriteEndElement();
                writer.WriteStartElement("value");

                TValue value = (TValue)this[key];

                valueSerializer.Serialize(writer, value);

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        #endregion
        #region constructor

        /// <summary>
        /// constructs a new Hashtable with a specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">
        /// an initial capacity for this instance
        /// </param>
        public Hashtable(int initialCapacity) : base(initialCapacity) { }

        /// <summary>
        /// constructs a new Hashtable with the default initial capacity
        /// </summary>
        public Hashtable() : base() { }

        #endregion
        #region add

        /// <summary>
        /// add a key value pair to the hashtable
        /// </summary>
        /// <param name="key">
        /// key
        /// </param>
        /// <param name="value">
        /// value
        /// </param>
        public void Add(String key, TValue value)
        {
            base.Add(key, value);
        }

        #endregion
        // todo: overload more non generic hashtable functions as needed
    } // class
} // namespace
