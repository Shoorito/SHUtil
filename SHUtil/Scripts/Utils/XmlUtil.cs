//////////////////////////////////////////////////////////////////////////
//
// XmlUtil
// 
// Created by Shoori.
//
// Copyright 2025 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SHUtil
{
    public interface ISingleNodeClass
    {
        void _Load(XmlNode node);
    }

    public static class XmlUtil
    {
        public static bool DEBUG_BINARY_LOG = false;

        public const string DEFAULT_XML_DATA_ROOT = "DataList";
        public const string DEFAULT_XML_DATA_ROW = "Row";

        //----------------------------------------------------------------------------------
        public static ParseType ParseSingleNode<ParseType>(XmlNode node, string name) where ParseType : class, ISingleNodeClass, new()
        {
            if (node == null)
                return default(ParseType);

            XmlNode selected_node = node.SelectSingleNode(name);
            if (selected_node == null)
                return default(ParseType);

            var pt = new ParseType();
            pt._Load(selected_node);

            return pt;
        }

        //----------------------------------------------------------------------------------
        public static string ParseInnerText(XmlNode node, string name)
        {
            XmlNode child_node = node.SelectSingleNode(name);
            if (child_node != null)
                return child_node.InnerText;

            return "";
        }

        //----------------------------------------------------------------------------------
        public static ParseType ParseAttribute<ParseType>(XmlSelector selector, string name, ParseType default_value)
        {
            if (selector == null)
            {
                return default_value;
            }
            else
            {
                if (selector.m_XMLNode != null)
                    return ParseAttribute<ParseType>(selector.m_XMLNode, name, default_value);
                else if (selector.m_XMLReader != null)
                    return ParseAttribute<ParseType>(selector.m_XMLReader, name, default_value);
                else if (selector.m_XMLBinaryCurrNode != null)
                    return ParseAttribute<ParseType>(selector.m_XMLBinaryCurrNode, name, default_value);

                return default_value;
            }
        }

        //----------------------------------------------------------------------------------
        public static ParseType ParseAttribute<ParseType>(XmlBinary.Node binary, string name, ParseType default_value)
        {
            if (binary == null)
                return default_value;

            return StringUtil.SafeParse<ParseType>(binary.GetAttribute(name), default_value);
        }

        //----------------------------------------------------------------------------------
        public static ParseType ParseAttribute<ParseType>(XmlReader reader, string name, ParseType default_value)
        {
            if (reader == null)
                return default_value;

            return StringUtil.SafeParse<ParseType>(reader.GetAttribute(name), default_value);
        }

        //----------------------------------------------------------------------------------
        public static ParseType ParseAttribute<ParseType>(XmlNode node, string name, ParseType default_value)
        {
            if (node == null)
                return default_value;

            XmlAttribute attr = node.Attributes[name];
            if (attr == null)
                return default_value;

            return StringUtil.SafeParse<ParseType>(attr.Value, default_value);
        }

        //----------------------------------------------------------------------------------
        public static XmlNode AddNode(XmlNode parent, string name)
        {
            if (parent == null || parent.OwnerDocument == null)
                return null;

            XmlNode new_node = parent.OwnerDocument.CreateElement(name);
            parent.AppendChild(new_node);

            return new_node;
        }

        //----------------------------------------------------------------------------------
        public static void AddAttribute(XmlNode node, string name, object value)
        {
            if (node == null || node.OwnerDocument == null)
                return;

            XmlAttribute attr = node.OwnerDocument.CreateAttribute(name);
            attr.Value = value.ToString();
            node.Attributes.Append(attr);
        }

        //----------------------------------------------------------------------------------
        public static bool ContainAttribute(XmlNode node, string name)
        {
            if (node == null)
                return false;

            return node.Attributes[name] != null;
        }

        //----------------------------------------------------------------------------------
        public static bool ContainAttribute(XmlBinary.Node node, string name)
        {
            return node.HasAttribute(name);
        }

        //----------------------------------------------------------------------------------
        public static bool ContainAttribute(XmlReader reader, string name)
        {
            if (reader == null)
                return false;

            return reader.GetAttribute(name) != null;
        }

        //----------------------------------------------------------------------------------
        public static List<T> ParseAttributeToList<T>(XmlNode node, string name, params char[] separator)
        {
            if (node == null)
                return null;

            return StringUtil.SafeParseToList<T>(ParseAttribute<string>(node, name, ""), separator);
        }

        //----------------------------------------------------------------------------------
        public static List<T> ParseAttributeToList<T>(XmlBinary.Node binary, string name, params char[] separator)
        {
            if (binary == null)
                return null;

            return StringUtil.SafeParseToList<T>(ParseAttribute<string>(binary, name, ""), separator);
        }

        //----------------------------------------------------------------------------------
        public static List<T> ParseAttributeToList<T>(XmlReader reader, string name, params char[] separator)
        {
            if (reader == null)
                return null;

            return StringUtil.SafeParseToList<T>(ParseAttribute<string>(reader, name, ""), separator);
        }

        //----------------------------------------------------------------------------------
        public static List<ParseType> ParseAttributeToList<ParseType>(XmlSelector selector, string name, params char[] separator)
        {
            if (selector == null)
                return null;

            if (selector.m_XMLNode != null)
                return ParseAttributeToList<ParseType>(selector.m_XMLNode, name, separator);
            else if (selector.m_XMLReader != null)
                return ParseAttributeToList<ParseType>(selector.m_XMLReader, name, separator);
            else if (selector.m_XMLBinaryCurrNode != null)
                return ParseAttributeToList<ParseType>(selector.m_XMLBinaryCurrNode, name, separator);

            return null;
        }

        //----------------------------------------------------------------------------------
        public static List<ParseType> ParseAttributeMultiple<ParseType>(XmlSelector selector, string name, int begin_idx, ParseType default_value, bool ignore_empty)
        {
            if (selector == null)
                return null;

            List<ParseType> list = null;
            int attr_count = selector.AttributeCount;
            for (int i = begin_idx; i <= attr_count; i++)
            {
                string attr_name = $"{name}_{i}";
                if (selector.HasAttribute(attr_name))
                {
                    string str_value = ParseAttribute<string>(selector, attr_name, "");
                    if (!string.IsNullOrEmpty(str_value) || !ignore_empty)
                    {
                        if (list == null)
                            list = new List<ParseType>();

                        list.Add(StringUtil.SafeParse<ParseType>(str_value, default_value));
                    }
                }
            }

            return list;
        }

        //----------------------------------------------------------------------------------
        public static string EncodeXml<T>(T data)
        {
            var builder = new StringBuilder();
            var writer = new StringWriter(builder);

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var encoder = new XmlSerializer(typeof(T));
            encoder.Serialize(writer, data, ns);
            writer.Close();

            return builder.ToString();
        }

        //----------------------------------------------------------------------------------
        public static T DecodeXml<T>(string data)
        {
            var decoder = new XmlSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                return (T)decoder.Deserialize(ms);
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlDocToFile(string file_path, XmlDocument doc)
        {
            if (string.IsNullOrEmpty(file_path) || doc == null)
                return false;

            string dir_path = Path.GetDirectoryName(file_path);
            if (!string.IsNullOrEmpty(dir_path) && !Directory.Exists(dir_path))
                Directory.CreateDirectory(dir_path);

            doc.Save(file_path);
            return true;
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlToFile(string file_path, string xml)
        {
            var fileStream = GetFileStream(file_path);
            if (fileStream == null)
                return false;

            using (fileStream)
            using (var writer = new StreamWriter(fileStream, new UTF8Encoding(false)))
                writer.Write(xml);

            return true;
        }

        //----------------------------------------------------------------------------------
        private static FileStream GetFileStream(string file_path)
        {
            string dir_name = Path.GetDirectoryName(file_path);
            if (!string.IsNullOrEmpty(dir_name) && !Directory.Exists(dir_name))
                Directory.CreateDirectory(dir_name);

            try
            {
                return new FileStream(file_path, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            catch
            {
                return null;
            }
        }

        //----------------------------------------------------------------------------------
        public static XmlDocument LoadXmlFromFile(string file_path)
        {
            if (!File.Exists(file_path))
            {
                SHLog.LogError($"[ERROR] XmlDocument Load Fail: {file_path}");
                return null;
            }

            using (var reader = new StreamReader(file_path, Encoding.UTF8))
            {
                var doc = new XmlDocument();
                doc.Load(reader);
                return doc;
            }
        }

        //----------------------------------------------------------------------------------
        public static string LoadXmlStrFromFile(string file_path)
        {
            if (!File.Exists(file_path))
            {
                SHLog.LogError($"[ERROR] XmlDocument Load Fail: {file_path}");
                return null;
            }

            using (var reader = new StreamReader(file_path, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        //----------------------------------------------------------------------------------
        public static string ConvertXmlTextPreDefined(object value, bool also_predefined, bool process_trim)
        {
            string convert_text = value.ToString();
            if (also_predefined)
            {
                convert_text = convert_text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;");
                convert_text = convert_text.Replace("_x000D_", "\\n");
            }
            else
            {
                convert_text = convert_text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t");
            }

            if (process_trim)
                convert_text = convert_text.Trim();

            int end_idx = convert_text.Length;
            for (int i = end_idx - 2; i >= 0; i -= 2)
            {
                string check_last_linefeed = convert_text.Substring(i, 2);
                if (check_last_linefeed != "\\n")
                    break;

                end_idx = i;
            }

            return convert_text.Substring(0, end_idx);
        }

        //----------------------------------------------------------------------------------
        public static string ReverseConvertXmlPreDefined(string value)
        {
            return value.Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&gt;", ">")
                .Replace("&lt;", "<")
                .Replace("&amp;", "&")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }

        //----------------------------------------------------------------------------------
        public static XmlNode FindNode(string find_name, string find_value, XmlNodeList node_list)
        {
            foreach (object obj in node_list)
            {
                XmlNode node = (XmlNode)obj;
                string value = ParseAttribute<string>(node, find_name, "");
                if (value == find_value)
                    return node;
            }

            return null;
        }

        //----------------------------------------------------------------------------------
        public static XmlDocument SafeLoad(byte[] byte_data, Action<string> error_callback = null)
        {
            if (byte_data == null)
                return null;

            try
            {
                var doc = new XmlDocument();
                doc.Load(new MemoryStream(byte_data));
                return doc;
            }
            catch (Exception e)
            {
                if (error_callback != null)
                    error_callback(e.ToString());
            }

            return null;
        }

        //----------------------------------------------------------------------------------
        public static XmlDocument SafeLoad(string xml_str, Action<string> error_callback = null)
        {
            if (string.IsNullOrEmpty(xml_str))
                return null;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml_str);
                return doc;
            }
            catch (Exception e)
            {
                if (error_callback != null)
                    error_callback(e.ToString());
            }

            return null;
        }

        //----------------------------------------------------------------------------------
        public static T GetNodeAttributeValue<T>(XmlNode node, string find_attr_name)
        {
            XmlAttribute attr = node.Attributes[find_attr_name];
            if (attr == null)
                return default(T);

            string attr_value = attr.Value;
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.IsValid(attr_value))
            {
                return (T)(converter.ConvertFromString(attr_value));
            }

            return default(T);
        }

        //----------------------------------------------------------------------------------
        public static XmlNode GetNode(XmlDocument doc, string find_node_name)
        {
            if (doc == null || doc.DocumentElement == null || string.IsNullOrEmpty(find_node_name))
                return null;

            XmlNodeList find_nodes = doc.GetElementsByTagName(find_node_name);
            if (find_nodes == null || find_nodes.Count <= 0)
                return null;

            return find_nodes[0];
        }

        //----------------------------------------------------------------------------------
        public static bool ExistsNode(XmlDocument doc, string nodeName)
        {
            return GetNode(doc, nodeName) != null;
        }

        //----------------------------------------------------------------------------------
        public static bool SetNodeValue(XmlDocument doc, string nodeName, string nodeValue)
        {
            var getNode = GetNode(doc, nodeName);
            if (getNode == null)
                return false;

            getNode.InnerText = nodeValue;
            return true;
        }
    }
}
