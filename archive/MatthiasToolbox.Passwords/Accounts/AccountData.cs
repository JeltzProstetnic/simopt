using System;
using System.Security;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    /// <summary>
    /// Account Data
    /// </summary>
    public class AccountData : IXmlSerializable
    {
        #region classvars

        private SecureString accountName;
        private String[] names = new String[3];
        private SecureString[] fields = new SecureString[3];
        private SecureString comments;

        #endregion
        #region properties

        #region SecureString
        
        public SecureString AccountNameS
        {
            get { return accountName; }
            set { accountName = value; }
        }

        public SecureString Field1S
        {
            get { return fields[0]; }
            set { fields[0] = value; }
        }

        public SecureString Field2S
        {
            get { return fields[1]; }
            set { fields[1] = value; }
        }

        public SecureString Field3S
        {
            get { return fields[2]; }
            set { fields[2] = value; }
        }
        
        public SecureString CommentsS
        {
            get { return comments; }
            set { comments = value; }
        }

        #endregion
        #region String
        
        internal String AccountName
        {
            get { return Crypto.DecryptSecureString(accountName); }
            set { accountName = Crypto.EncryptSecureString(value); }
        }

        internal String Field1
        {
            get { return Crypto.DecryptSecureString(fields[0]); }
            set { fields[0] = Crypto.EncryptSecureString(value); }
        }

        internal String Field2
        {
            get { return Crypto.DecryptSecureString(fields[1]); }
            set { fields[1] = Crypto.EncryptSecureString(value); }
        }

        internal String Field3
        {
            get { return Crypto.DecryptSecureString(fields[2]); }
            set { fields[2] = Crypto.EncryptSecureString(value); }
        }

        internal String Comments
        {
            get { return Crypto.DecryptSecureString(comments); }
            set { comments = Crypto.EncryptSecureString(value); }
        }

        #endregion
        #region Encrypted

        private String AccountNameR
        {
            get { return Crypto.EncryptRSA(AccountNameS, Program.passUser); }
            set { AccountNameS = Crypto.DecryptRSA(value, Program.passUser); }
        }

        private String Field1R
        {
            get { return Crypto.EncryptRSA(Field1S, Program.passUser); }
            set { Field1S = Crypto.DecryptRSA(value, Program.passUser); }
        }

        private String Field2R
        {
            get { return Crypto.EncryptRSA(Field2S, Program.passUser); }
            set { Field2S = Crypto.DecryptRSA(value, Program.passUser); }
        }

        private String Field3R
        {
            get { return Crypto.EncryptRSA(Field3S, Program.passUser); }
            set { Field3S = Crypto.DecryptRSA(value, Program.passUser); }
        }

        private String CommentsR
        {
            get { return Crypto.EncryptRSA(CommentsS, Program.passUser); }
            set { CommentsS = Crypto.DecryptRSA(value, Program.passUser); }
        }

        #endregion
        
        #endregion
        #region constructor

        public AccountData()
        {
            accountName = new SecureString();
            fields[0] = new SecureString();
            fields[1] = new SecureString();
            fields[2] = new SecureString();
            comments = new SecureString();

            names[0] = "Username";
            names[1] = "Password_1";
            names[2] = "Password_2";
        }

        #endregion
        #region IXmlSerializable Implementation
        
        ///<summary>
        ///This property is reserved, apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"></see> to the class instead. 
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Xml.Schema.XmlSchema"></see> that describes the XML representation of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"></see> method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"></see> method.
        ///</returns>
        ///
        public XmlSchema GetSchema()
        {
            // throw new NotImplementedException();
            return null;
        }

        ///<summary>
        ///Generates an object from its XML representation.
        ///</summary>
        ///
        ///<param name="reader">The <see cref="T:System.Xml.XmlReader"></see> stream from which the object is deserialized. </param>
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(String));
            
            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();
            
            if (wasEmpty) return;
            
            if (reader.NodeType != XmlNodeType.None && reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("f1");
                AccountNameR = (String)serializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("f2");
                Field1R = (String)serializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("f3");
                Field2R = (String)serializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("f4");
                Field3R = (String)serializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("f5");
                CommentsR = (String)serializer.Deserialize(reader);
                reader.ReadEndElement();
            }
        }

        ///<summary>
        ///Converts an object into its XML representation.
        ///</summary>
        ///
        ///<param name="writer">The <see cref="T:System.Xml.XmlWriter"></see> stream to which the object is serialized. </param>
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(String));
            
            writer.WriteStartElement("f1");
            serializer.Serialize(writer, AccountNameR);
            writer.WriteEndElement();

            writer.WriteStartElement("f2");
            serializer.Serialize(writer, Field1R);
            writer.WriteEndElement();
            writer.WriteStartElement("f3");
            serializer.Serialize(writer, Field2R);
            writer.WriteEndElement();
            writer.WriteStartElement("f4");
            serializer.Serialize(writer, Field3R);
            writer.WriteEndElement();
            writer.WriteStartElement("f5");
            serializer.Serialize(writer, CommentsR);
            writer.WriteEndElement();
            
        }

        #endregion
    } // class
} // namepace
