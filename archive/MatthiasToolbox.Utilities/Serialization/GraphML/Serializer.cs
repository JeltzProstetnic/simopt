using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Utilities.Serialization.GraphML
{
    public static class Serializer
    {
        public static Container LoadFromFile(string file)
        {
            return LoadFromFile(new FileInfo(file));
        }

        public static Container LoadFromFile(FileInfo file)
        {
            Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();

            bool inGraph = false;
            bool inNode = false;
            Vertex tmpNode = null;
            Edge tmpEdge = null;

            StreamReader sr = file.OpenText();
            if (sr.EndOfStream) return null;

            Container result = new Container();
            result.Graph = new Graph();
            result.Graph.EdgeList = new List<Edge>();
            result.Graph.VertexList = new List<Vertex>();

            while (!sr.EndOfStream)
            {
                string tmpLine = sr.ReadLine().Trim();

                if (tmpLine.StartsWith("<?") || tmpLine.StartsWith("<!--") || tmpLine.StartsWith("<key") || tmpLine.StartsWith("</key") || tmpLine.StartsWith("<default/>")) continue;
                if (tmpLine.StartsWith("<graph")) inGraph = true;
                if (inGraph && tmpLine.StartsWith("<node"))
                {
                    inNode = true;
                    tmpNode = new Vertex();
                    tmpNode.ID = tmpLine.getAttribute("id");
                    tmpNode.Name = tmpNode.ID;
                    tmpNode.Geometry = new Geometry();
                }
                if (inNode && tmpLine.StartsWith("<y:Geometry"))
                {
                    tmpNode.Geometry.Height = double.Parse(tmpLine.getAttribute("height"));
                    tmpNode.Geometry.Width = double.Parse(tmpLine.getAttribute("width"));
                    tmpNode.Geometry.X = double.Parse(tmpLine.getAttribute("x"));
                    tmpNode.Geometry.Y = double.Parse(tmpLine.getAttribute("y"));
                }
                else if (inNode && tmpLine.StartsWith("<y:Fill"))
                {
                    tmpNode.Fill = ColorTranslator.FromHtml(tmpLine.getAttribute("color"));
                }
                else if (inNode && tmpLine.StartsWith("<y:NodeLabel"))
                {
                    if (!isTagComplete(tmpLine)) tmpLine += sr.ReadLine().Trim();
                    tmpNode.Label = tmpLine.getTagContent();
                }
                else if (inNode && tmpLine.StartsWith("</node"))
                {
                    inNode = false;
                    result.Graph.VertexList.Add(tmpNode);
                    vertices[tmpNode.ID] = tmpNode;
                }

                if (inGraph && tmpLine.StartsWith("<edge"))
                {
                    tmpEdge = new Edge();
                    tmpEdge.ID = tmpLine.getAttribute("id");
                    tmpEdge.Name = tmpEdge.ID;
                    string srcID = tmpLine.getAttribute("source");
                    string trgID = tmpLine.getAttribute("target");
                    tmpEdge.Vertex1 = vertices[srcID];
                    tmpEdge.Vertex2 = vertices[trgID];
                    tmpEdge.Start = (tmpEdge.Vertex1 as Vertex).Geometry.Center;
                    tmpEdge.End = (tmpEdge.Vertex2 as Vertex).Geometry.Position;
                    result.Graph.EdgeList.Add(tmpEdge);
                }
            }
            sr.Close();

            return result;
        }

        private static string getTagContent(this string text)
        {
            int posStart = text.IndexOf(">");
            int posEnd = text.IndexOf('<', posStart + 1);
            return text.Substring(posStart + 1, posEnd - (posStart + 1));
        }

        private static bool isTagComplete(this string text)
        {
            int posStart = text.IndexOf(">");
            int posEnd = text.IndexOf('<', posStart + 1);
            return posEnd > posStart;
        }

        private static string getAttribute(this string text, string attributeName)
        {
            int posStart = text.IndexOf(attributeName + "=");
            int posEnd = text.IndexOf('\"', posStart + attributeName.Length + 2);
            return text.Substring(posStart + attributeName.Length + 2, posEnd - (posStart + attributeName.Length + 2));
        }

        ///// <summary>
        ///// Method to convert a custom Object to XML string
        ///// </summary>
        ///// <param name="pObject">Object that is to be serialized to XML</param>
        ///// <returns>XML string</returns>
        //public static String Serialize(GraphMLFile pObject)
        //{
        //    try
        //    {
        //        String XmlizedString = null;
        //        MemoryStream memoryStream = new MemoryStream();
        //        XmlSerializer xs = new XmlSerializer(typeof(GraphMLFile));
        //        XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

        //        xs.Serialize(xmlTextWriter, pObject);
        //        memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
        //        XmlizedString = memoryStream.ToArray().UTF8ToString();
        //        return XmlizedString;
        //    }
        //    catch (Exception e)
        //    {
        //        System.Console.WriteLine(e);
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// Method to reconstruct an Object from XML string
        ///// </summary>
        ///// <param name="pXmlizedString"></param>
        ///// <returns></returns>
        //public static GraphMLFile Deserialize(String pXmlizedString)
        //{
        //    XmlSerializer xs = new XmlSerializer(typeof(GraphMLFile));
        //    MemoryStream memoryStream = new MemoryStream(pXmlizedString.ToUTF8ByteArray());
        //    XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

        //    return (GraphMLFile)xs.Deserialize(memoryStream);
        //}

        //public static void Serialize(string file, GraphMLFile graph)
        //{
        //    Stream stream = File.Open(file, FileMode.Create);
        //    BinaryFormatter bFormatter = new BinaryFormatter();
        //    bFormatter.Serialize(stream, graph);
        //    stream.Close();
        //}

        //public static GraphMLFile Deserialize(string file)
        //{
        //    GraphMLFile graph;
        //    Stream stream = File.Open(file, FileMode.Open);
        //    BinaryFormatter bFormatter = new BinaryFormatter();
        //    graph = (GraphMLFile)bFormatter.Deserialize(stream);
        //    stream.Close();
        //    return graph;
        //}
    }
}