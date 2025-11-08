//////////////////////////////////////////////////////////////////////////
//
// XmlBinary
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

using System.Text;
using System.Xml;
using System.IO;
using System.Collections.Generic;

namespace SHUtil
{
    public class XmlBinary
    {
        public class Node
        {
            private Dictionary<short, string> mFieldDic;
            private Dictionary<string, string> mKeyValueAttributes;

            public string Identity { get; private set; }
            public int AttributeCount => mKeyValueAttributes.Count;
            public Dictionary<short, string> FieldDic => mFieldDic;

            //----------------------------------------------------------------------------------
            public Node(string identity, BinaryReader reader, Dictionary<short, string> row_field_dic, int row_idx = 0)
            {
                Identity = identity;
                mKeyValueAttributes = new Dictionary<string, string>();
                mFieldDic = row_field_dic;

                string bin_identity = reader.ReadString();
                if (bin_identity != Identity)
                    return;

                int attr_count = reader.ReadInt32();
                for (int i = 0; i < attr_count; i++)
                {
                    string key = "";
                    string value = "";

                    if (Identity == "Row")
                    {
                        short key_idx = reader.ReadInt16();
                        if (row_field_dic != null && row_field_dic.TryGetValue(key_idx, out var fieldName))
                            key = fieldName;

                        value = reader.ReadString();
                    }
                    else if (Identity == "DataList")
                    {
                        key = reader.ReadString();
                        value = reader.ReadString();
                    }

                    if (!string.IsNullOrEmpty(key) && !mKeyValueAttributes.ContainsKey(key))
                        mKeyValueAttributes.Add(key, value);
                }

                if (Identity == "DataList")
                {
                    mFieldDic = new Dictionary<short, string>();

                    int field_dic_count = reader.ReadInt32();
                    for (int i = 0; i < field_dic_count; i++)
                    {
                        short field_key = reader.ReadInt16();
                        string field_value = reader.ReadString();
                        if (field_key > 0 && !string.IsNullOrEmpty(field_value) && !mFieldDic.ContainsKey(field_key))
                            mFieldDic.Add(field_key, field_value);
                    }
                }
            }

            //----------------------------------------------------------------------------------
            public string GetAttribute(string key)
            {
                if (mKeyValueAttributes.TryGetValue(key, out var ret))
                    return ret;

                return "";
            }

            //----------------------------------------------------------------------------------
            public bool HasAttribute(string key)
            {
                return mKeyValueAttributes.ContainsKey(key);
            }

            //----------------------------------------------------------------------------------
            public string DebugInfo()
            {
                var sb = new StringBuilder();
                sb.AppendLine(Identity);

                foreach (KeyValuePair<string, string> kvp in mKeyValueAttributes)
                {
                    sb.AppendFormat($"[{kvp.Key}:{kvp.Value}]");
                }

                return sb.ToString();
            }
        }

        public Node Header { get; private set; }
        public List<Node> RowList { get; private set; }

        public static string CurrentFilePath = "";

        //----------------------------------------------------------------------------------
        public static void WriteXML(BinaryWriter writer, XmlDocument doc, Dictionary<short, string> row_field_dic)
        {
            XmlNode header_node = doc.SelectSingleNode("DataList");

            writer.Write("DataList");

            int header_node_attr_count = header_node.Attributes.Count;
            writer.Write(header_node_attr_count);

            for (int i = 0; i < header_node_attr_count; i++)
            {
                XmlAttribute attr = header_node.Attributes[i];
                writer.Write(attr.Name);
                writer.Write(attr.Value);
            }

            writer.Write(row_field_dic.Count);

            foreach (KeyValuePair<short, string> kvp in row_field_dic)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            XmlNodeList row_node_list = header_node.SelectNodes("Row");
            int row_count = row_node_list.Count;
            writer.Write(row_count);

            foreach (object obj in row_node_list)
            {
                writer.Write("Row");

                XmlNode child = (XmlNode)obj;
                int attr_count = child.Attributes.Count;
                writer.Write(attr_count);

                for (int i = 0; i < attr_count; i++)
                {
                    XmlAttribute attr = child.Attributes[i];
                    short key_idx = 0;
                    string key_value = "";
                    foreach (KeyValuePair<short, string> kvp in row_field_dic)
                    {
                        if (kvp.Value == attr.Name)
                        {
                            key_idx = kvp.Key;
                            key_value = attr.Value;
                            break;
                        }
                    }

                    writer.Write(key_idx);
                    writer.Write(key_value);
                }
            }
        }

        //----------------------------------------------------------------------------------
        public XmlBinary(byte[] bytes, string file_path, bool header_only, bool is_encrypted, string encrypt_key)
        {
            CurrentFilePath = file_path;

            byte[] sourceBytes = bytes ?? (string.IsNullOrEmpty(file_path) ? null : File.ReadAllBytes(file_path));
            if (sourceBytes == null)
                return;

            if (is_encrypted && !string.IsNullOrEmpty(encrypt_key))
            {
                sourceBytes = FileUtil.DecryptWithBytes(sourceBytes, encrypt_key);
                if (sourceBytes == null)
                    return;
            }

            using (var br = new BinaryReader(new MemoryStream(sourceBytes)))
                Load(br, header_only);
        }

        //----------------------------------------------------------------------------------
        private void Load(BinaryReader reader, bool header_only)
        {
            Header = new Node("DataList", reader, null);
            int row = reader.ReadInt32();
            if (!header_only)
            {
                Dictionary<short, string> row_field_dic = Header.FieldDic;
                RowList = new List<Node>();
                for (int i = 0; i < row; i++)
                {
                    RowList.Add(new Node("Row", reader, row_field_dic, i));
                }
            }
        }

        //----------------------------------------------------------------------------------
        public string DebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header.DebugInfo());
            foreach (Node row in RowList)
            {
                sb.AppendLine(row.DebugInfo());
            }

            return sb.ToString();
        }
    }
}
