///////////////////////////////////////////////////////////////////////////////////////
//
//        Project:  BlueLogic.SDelta
//    Description: 
//         Status:  FINAL
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 8. Mai 2007 Matthias Gruber ALPHA
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MatthiasToolbox.Delta.Delta;

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// Specialized XML Seriliation Provider for binary deltas
    /// </summary>
    /// <typeparam name="THash">type of the hash</typeparam>
    /// <typeparam name="TCheck">type of the checksum</typeparam>
    public class XmlSerializationProvider<THash, TCheck> : SerializationProvider<THash, TCheck>
    {
        #region classvars
        private XmlWriter writer;
        private XmlReader reader;

        private XmlReaderSettings setReader;
        private XmlWriterSettings setWriter;

        private XmlSerializer mySerializer;
        #endregion
        #region constructor
        
        /// <summary>
        /// initialize the XmlSerializationProvider
        /// </summary>
        public XmlSerializationProvider()
        {
            setWriter = new XmlWriterSettings();
            setWriter.IndentChars = "  ";
            setWriter.CloseOutput = false;
            setWriter.Indent = true;
            
            setReader = new XmlReaderSettings();
            setReader.IgnoreComments = true;
            setReader.CloseInput = false;
            setReader.IgnoreProcessingInstructions = true;
            setReader.IgnoreWhitespace = true;
            mySerializer = new XmlSerializer(typeof(byte[]));
        }
        
        #endregion
        #region writer
        
        /// <summary>
        /// Write BlockedHashset to XML
        /// </summary>
        /// <param name="hashset">hashset to save</param>
        /// <param name="file">file to save to</param>
        /// <returns>true on success</returns>
        public override bool WriteBlockedHashSet(BlockedHashset<THash, TCheck> hashset, Stream file)
        {
            if (!file.CanWrite) return false;

            try
            {
                writer = XmlWriter.Create(file, setWriter);

                writer.WriteStartElement("BlockedHashset");

                writer.WriteAttributeString("blocksize", hashset.BlockSize.ToString());
                writer.WriteAttributeString("checksumname", hashset.CheckName);
                writer.WriteAttributeString("hashname", hashset.HashName);

                writer.WriteStartElement("checksums");

                foreach (DictionaryEntry e in hashset.ChecksumPointers)
                {
                    TCheck key = (TCheck)e.Key;

                    List<long> addresses = (List<long>)e.Value; // checksumPointers[key];
                    writer.WriteElementString(hashset.CheckName, key.ToString());

                    writer.WriteStartElement("addresses");

                    foreach (long address in addresses)
                    {
                        writer.WriteElementString("address", address.ToString());
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement("hashes");

                foreach (KeyValuePair<long, THash> pair in hashset.HashList)
                {
                    writer.WriteElementString(hashset.HashName, pair.Value.ToString());
                    writer.WriteElementString("address", pair.Key.ToString());
                }
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        
        /// <summary>
        /// write binary delta to XML
        /// </summary>
        /// <param name="delta">the delta to save</param>
        /// <param name="file">the file to save to</param>
        /// <returns>true on success</returns>
        public override bool WriteDelta(Delta<BinaryCommand> delta, Stream file)
        {
            if (!file.CanWrite) return false;

            try
            {
                writer = XmlWriter.Create(file, setWriter);

                writer.WriteStartElement("Delta");

                 writer.WriteAttributeString("digestname", delta.DigestName);
                 writer.WriteAttributeString("hashname", delta.HashName);
                 writer.WriteAttributeString("checksumname", delta.CheckName);
                 writer.WriteAttributeString("blocksize", delta.BlockSize.ToString());

                 writer.WriteStartElement("commands");
                 foreach (BinaryCommand cmd in delta.Commands)
                 {
                     if(cmd.CommandType == CommandType.Copy)
                     {
                         writer.WriteStartElement("copy");
                          writer.WriteElementString("from", cmd.StartAddress.ToString());
                          writer.WriteElementString("to", cmd.EndAddress.ToString());
                          writer.WriteElementString("repeat", cmd.Repeat.ToString());
                         writer.WriteEndElement();
                     }
                     else   // insert
                     {
                         writer.WriteStartElement("insert");
                          mySerializer.Serialize(writer, cmd.Data);
                         writer.WriteEndElement();
                     }
                 }
                 writer.WriteEndElement();

                 writer.WriteElementString("digest", delta.Digest);

                writer.WriteEndElement();

                writer.Flush();
                writer.Close();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        
        #endregion
        #region reader

        /// <summary>
        /// Read BlockedHashset from XML
        /// </summary>
        /// <param name="hashset">hashset to populate</param>
        /// <param name="file">file to read from</param>
        /// <returns>true on success</returns>
        public override bool ReadBlockedHashSet(ref BlockedHashset<THash, TCheck> hashset, Stream file)
        {
            if (!file.CanRead) return false;

            try
            {
                reader = XmlReader.Create(file, setReader);

                bool check = false;
                bool adr = false;
                String tmpKey = "";
                List<long> tmpAdr = new List<long>();
                String dummyAdr = "";

                reader.Read();
                reader.Read();
                reader.Read();

                hashset.BlockSize = int.Parse(reader.GetAttribute("blocksize"));
                hashset.CheckName = reader.GetAttribute("checksumname");
                hashset.HashName = reader.GetAttribute("hashname");

                reader.Read();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == hashset.CheckName)
                        {
                            check = true;
                        }
                        else if (reader.Name == "address")
                        {
                            adr = true;
                            tmpAdr = new List<long>();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        if (adr)
                        {
                            adr = false;
                            tmpAdr.Add(long.Parse(reader.Value));
                        }
                        else if (check)
                        {
                            check = false;
                            tmpKey = reader.Value;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "addresses")
                        {
                            if (!String.IsNullOrEmpty(tmpKey)) hashset.ChecksumPointers.Add(tmpKey, tmpAdr);
                        }
                        else if (reader.Name == "checksums")
                        {
                            break;
                        }
                    }
                }

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == hashset.HashName)
                        {
                            check = true;
                        }
                        else if (reader.Name == "address")
                        {
                            adr = true;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        if (adr)
                        {
                            adr = false;
                            dummyAdr = reader.Value;
                        }
                        else if (check)
                        {
                            check = false;
                            tmpKey = reader.Value;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "address")
                        {
                            bool bb = false;
                            if (!String.IsNullOrEmpty(dummyAdr)) hashset.HashList.Add(long.Parse(dummyAdr), Converter.Convert<String, THash>(tmpKey, default(THash), null, ref bb));
                        }
                        else if (reader.Name == "hashes")
                        {
                            break;
                        }
                    }
                }
                reader.Close();

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Read delta from XML
        /// </summary>
        /// <param name="delta">delta to populate</param>
        /// <param name="file">file to read from</param>
        /// <returns>true on success</returns>
        public override bool ReadDelta(ref Delta<BinaryCommand> delta, Stream file)
        {
            if (!file.CanRead) return false;

            try
            {
                reader = XmlReader.Create(file, setReader);
                
                reader.Read();
                reader.Read();
                
                delta.DigestName = reader.GetAttribute("digestname");
                delta.HashName = reader.GetAttribute("hashname");
                delta.CheckName = reader.GetAttribute("checksumname");
                delta.BlockSize = int.Parse(reader.GetAttribute("blocksize"));
                
                while(reader.Read())
                {
                    if(reader.NodeType == XmlNodeType.Element)
                    {
                        if(reader.Name == "copy")
                        {
                            reader.Read();
                            reader.Read();
                            // from:
                            long a1 = int.Parse(reader.Value);
                            reader.Read();
                            
                            reader.Read();
                            reader.Read();
                            // to:
                            long a2 = int.Parse(reader.Value);
                            reader.Read();

                            reader.Read();
                            reader.Read();
                            // repeat:
                            int a3 = int.Parse(reader.Value);
                            reader.Read();
                            
                            BinaryCommand bct = new BinaryCommand(CommandType.Copy, null, a1, a2, a3);
                            delta.Commands.Add(bct);
                        }
                        else if(reader.Name == "insert")   // insert
                        {
                            reader.Read();
                            byte[] b = (byte[])mySerializer.Deserialize(reader);
                            BinaryCommand bct = new BinaryCommand(CommandType.Insert, b, 0, 0, 0);
                            delta.Commands.Add(bct);
                        }
                        else if (reader.Name == "digest")
                        {
                            reader.Read();
                            delta.Digest = reader.Value;
                            break;
                        }
                    }
                   
                }
                
                reader.Close();

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        
        #endregion
    } // class
} // namespace
