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
using System.Security.Cryptography;
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

        //----------------------------------------------------------------------------------
        public static ParseType ParseSingleNode<ParseType>(XmlNode node, string name) where ParseType : class, ISingleNodeClass, new()
        {
            if (node == null)
                return default(ParseType);

            XmlNode selected_node = node.SelectSingleNode(name);
            if (selected_node == null)
                return default(ParseType);

            ParseType pt = Activator.CreateInstance<ParseType>();
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
                if (selector.HasAttribute(attr_name) == true)
                {
                    string str_value = ParseAttribute<string>(selector, attr_name, "");
                    if (string.IsNullOrEmpty(str_value) == false || ignore_empty == false)
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
            StringBuilder builder = new StringBuilder();
            StringWriter writer = new StringWriter(builder);

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            XmlSerializer encoder = new XmlSerializer(typeof(T));
            encoder.Serialize(writer, data, ns);
            writer.Close();

            return builder.ToString();
        }

        //----------------------------------------------------------------------------------
        public static T DecodeXml<T>(string data)
        {
            XmlSerializer decode = new XmlSerializer(typeof(T));
            MemoryStream read_stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            T type = (T)decode.Deserialize(read_stream);
            read_stream.Close();

            return type;
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlDocToFile(string file_path, XmlDocument doc)
        {
            if (string.IsNullOrEmpty(file_path) == true || doc == null)
                return false;

            string dir_path = Path.GetDirectoryName(file_path);
            if (Directory.Exists(dir_path) == false)
                Directory.CreateDirectory(dir_path);

            doc.Save(file_path);

            return true;
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlDocToEncryptFile(string file_path, XmlDocument doc, string key)
        {
            FileStream file_stream = GetFileStream(file_path);
            if (file_stream == null)
                return false;

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);

            CryptoStream crypto_stream = new CryptoStream(file_stream, des.CreateEncryptor(), CryptoStreamMode.Write);
            if (crypto_stream == null)
            {
                file_stream.Close();
                return false;
            }

            StreamWriter stream_writer = new StreamWriter(crypto_stream, Encoding.UTF8);
            if (stream_writer == null)
            {
                crypto_stream.Close();
                file_stream.Close();
            }

            crypto_stream.Close();
            file_stream.Close();

            doc.Save(stream_writer);
            stream_writer.Close();
            return true;
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlToFile(string file_path, string xml)
        {
            FileStream file_stream = GetFileStream(file_path);
            if (file_stream == null)
                return false;

            StreamWriter stream_writer = new StreamWriter(file_stream, Encoding.UTF8);
            if (stream_writer == null)
            {
                file_stream.Close();
                return false;
            }

            file_stream.Close();

            stream_writer.Write(xml);
            stream_writer.Close();
            return true;
        }

        //----------------------------------------------------------------------------------
        public static bool SaveXmlToEncryptFile(string file_path, string xml, string key)
        {
            FileStream file_stream = GetFileStream(file_path);
            if (file_stream == null)
                return false;

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);

            CryptoStream crypto_stream = new CryptoStream(file_stream, des.CreateEncryptor(), CryptoStreamMode.Write);
            if (crypto_stream == null)
            {
                file_stream.Close();
                return false;
            }

            StreamWriter stream_writer = new StreamWriter(crypto_stream, Encoding.UTF8);
            if (stream_writer == null)
            {
                file_stream.Close();
                crypto_stream.Close();
                return false;
            }

            file_stream.Close();
            crypto_stream.Close();

            stream_writer.Write(xml);
            stream_writer.Close();

            return true;
        }

        //----------------------------------------------------------------------------------
        private static FileStream GetFileStream(string file_path)
        {
            string dir_name = Path.GetDirectoryName(file_path);
            if (Directory.Exists(dir_name) == false)
                Directory.CreateDirectory(dir_name);

            if (File.Exists(file_path) == false)
                return null;

            FileStream file_stream = null;
            try
            {
                file_stream = File.Open(file_path, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            catch (IOException io_exception)
            {
                if (file_stream != null)
                    file_stream.Close();

                try
                {
                    file_stream = File.Open(file_path, FileMode.Create, FileAccess.Write, FileShare.Read);
                }
                catch (System.Exception e)
                {
                    if (file_stream != null)
                        file_stream.Close();

                    return null;
                }
            }

            return file_stream;
        }

        //----------------------------------------------------------------------------------
        public static XmlDocument LoadXmlFromFile(string file_path)
        {
            if (File.Exists(file_path) == false)
            {
                SHLog.LogError($"[ERROR] XmlDocument Load Fail: {file_path}, please check if the XML file exists at the file path...");
                return null;
            }

            StreamReader stream_reader = new StreamReader(file_path, Encoding.UTF8);
            if (stream_reader == null)
                return null;

            XmlDocument doc = new XmlDocument();
            doc.Load(stream_reader);
            stream_reader.Close();
            return doc;
        }

        //----------------------------------------------------------------------------------
        public static string LoadXmlStrFromFile(string file_path)
        {
            if (File.Exists(file_path) == false)
            {
                SHLog.LogError($"[ERROR] XmlDocument Load Fail: {file_path}, please check if the XML file exists at the file path...");
                return null;
            }

            StreamReader stream_reader = new StreamReader(file_path, Encoding.UTF8);
            if (stream_reader == null)
                return null;

            string xml_str = stream_reader.ReadToEnd();
            stream_reader.Close();
            return xml_str;
        }

        //----------------------------------------------------------------------------------
        public static string LoadXmlStrFromEncryptFile(string file_path, string key)
        {
            FileStream file_stream = GetFileStream(file_path);
            if (file_stream == null)
                return "";

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);

            CryptoStream crypto_stream = new CryptoStream(file_stream, des.CreateDecryptor(), CryptoStreamMode.Read);
            if (crypto_stream == null)
            {
                file_stream.Close();
                return "";
            }

            StreamReader stream_reader = new StreamReader(crypto_stream, Encoding.UTF8);
            if (stream_reader == null)
                return "";

            string xml_str = stream_reader.ReadToEnd();

            file_stream.Close();
            stream_reader.Close();
            crypto_stream.Close();

            return xml_str;
        }

        //----------------------------------------------------------------------------------
        public static string ConvertXmlTextPreDefined(object value, bool also_predefined, bool process_trim)
        {
            string convert_text = value.ToString();
            if (also_predefined == true)
            {
                convert_text = convert_text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;");
                convert_text = convert_text.Replace("_x000D_", "\\n");
            }
            else
            {
                convert_text = convert_text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t");
            }

            if (process_trim == true)
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
                .Replace("\\n", "\n")
                .Replace("\\n", "\r")
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
                XmlDocument doc = new XmlDocument();
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
            if (string.IsNullOrEmpty(xml_str) == true)
                return null;

            try
            {
                XmlDocument doc = new XmlDocument();
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
            if (converter != null && converter.IsValid(attr_value) == true)
            {
                return (T)(converter.ConvertFromString(attr_value));
            }

            return default(T);
        }

        //----------------------------------------------------------------------------------
        public static XmlNode GetNode(XmlDocument doc, string find_node_name)
        {
            if (doc == null || doc.DocumentElement == null || string.IsNullOrEmpty(find_node_name) == true)
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
