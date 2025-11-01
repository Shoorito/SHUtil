//////////////////////////////////////////////////////////////////////////
//
// TableBaseManager
// 
// Created by Shoori.
//
// Copyright 2024 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////


using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using SHUtil.Data;

namespace SHUtil.Table
{

    //----------------------------------------------------------------------------------
    public abstract class TableBaseManager<ValueType, ManagerType> : DataReloadSingleton<ManagerType> where ValueType : TableInfoBase, new() where ManagerType : DataReloadBase, new()
    {
        public override string ReloadDataID
        {
            get
            {
                return DATA_ID;
            }
        }

        public abstract string DATA_ID { get; }
        public abstract bool CheckSameIDN { get; }
        public abstract bool CheckSameID { get; }

        public List<ValueType> Infos
        {
            get
            {
                return m_Infos;
            }
        }

        public int Count
        {
            get
            {
                return m_Infos.Count;
            }
        }

        protected List<ValueType> m_Infos = new List<ValueType>();

        public int Version { get; private set; }
        public DateTime CreatedTime { get; private set; }
        public DateTime LastLoadTime { get; private set; }
        public bool UseXMLReader { get; private set; }
        public bool EnableServerPatch { get; private set; }
        public bool IsServerRegist { get; private set; }
        public bool UseXMLBinary { get; private set; }

        public string DATA_XML_ID = "";

        protected Dictionary<int, TableInfoBase> m_InfosByIntKey = new Dictionary<int, TableInfoBase>();
        protected Dictionary<string, TableInfoBase> m_InfosByStrKey = new Dictionary<string, TableInfoBase>();
        protected string m_Base64CompressedString = "";

        //----------------------------------------------------------------------------------
        protected abstract void ParsingRow(XmlSelector row_node);

        //----------------------------------------------------------------------------------
        public TableBaseManager()
        {
            EnableServerPatch = false;
            UseXMLReader = false;
            IsServerRegist = false;

            Singleton<DataListManager>.Instance.AddHandler(ReloadDataID,
                new DataListManager.delegate_DataLoadHandler(SetLoadDataInternal),
                new DataListManager.delegate_GetVersionHandler(GetVersion),
                new DataListManager.delegate_GetServerData(GetServerPatchData),
                new DataListManager.delegate_UseXmlBinary(() => UseXMLBinary));

            RegistServer();
        }

        //----------------------------------------------------------------------------------
        public void RegistClient(bool enable_server_patch, bool use_xml_reader, bool use_xml_bin)
        {
            EnableServerPatch = enable_server_patch;
            UseXMLReader = use_xml_reader;
            UseXMLBinary = use_xml_bin;
            IsServerRegist = false;
        }

        //----------------------------------------------------------------------------------
        public override void RegistServer()
        {
            base.RegistServer();
            IsServerRegist = true;
        }

        //----------------------------------------------------------------------------------
        public override string ReloadData()
        {
            string load_url;
            if (LoadWithURL(out load_url) == true)
            {
                SetLoadDataInternal(load_url, true, null, "", false, "");
                return load_url;
            }

            return "";
        }

        //----------------------------------------------------------------------------------
        protected virtual void PreLoadData(XmlSelector node)
        {
        }

        //----------------------------------------------------------------------------------
        protected virtual void PostLoadData(XmlSelector node)
        {
        }

        //----------------------------------------------------------------------------------
        public virtual string DebugInfo()
        {
            return "";
        }

        //----------------------------------------------------------------------------------
        protected ValueType CreateIDNBaseParse(XmlSelector row_node)
        {
            int idn = XmlUtil.ParseAttribute<int>(row_node, "IDN", 0);
            string id = XmlUtil.ParseAttribute<string>(row_node, "ID", "");
            if (idn == 0 || (CheckSameID == true && string.IsNullOrEmpty(id) == true))
            {
                SHLog.LogError($"{DATA_ID} invalid IDN:{idn} ID:{id}");
                return default(ValueType);
            }

            ValueType exist_id_value = GetInfoByStrKey(id);
            if (exist_id_value != null)
            {
                if (CheckSameID == true)
                {
                    SHLog.LogError($"{DATA_ID} already exist ID (IDN:{idn} ID:{id})");
                    return default(ValueType);
                }

                exist_id_value.LoadAppend(row_node);
                return exist_id_value;
            }

            ValueType exist_idn_value = GetInfoByIntKey(idn);
            if (exist_idn_value != null)
            {
                if (CheckSameIDN == true)
                {
                    SHLog.LogError($"{DATA_ID} already exist IDN (IDN:{idn} ID:{id})");
                    return default(ValueType);
                }

                exist_idn_value.LoadAppend(row_node);
                return exist_idn_value;
            }

            exist_idn_value = Activator.CreateInstance<ValueType>();
            exist_idn_value.Init(idn, id);
            exist_idn_value.Load(row_node);

            AddInfo(exist_idn_value, idn, id);

            return exist_idn_value;
        }

        //----------------------------------------------------------------------------------
        private int GetVersion()
        {
            return Version;
        }

        //----------------------------------------------------------------------------------
        private bool GetServerPatchData(ref string server_data)
        {
            server_data = m_Base64CompressedString;
            return EnableServerPatch;
        }

        //----------------------------------------------------------------------------------
        private void SetLoadDataInternal(string xml_str_or_url, bool is_url, byte[] bytes, string file_path, bool is_binary, string bin_encrypt_key)
        {
            m_Infos.Clear();
            m_InfosByIntKey.Clear();
            m_InfosByStrKey.Clear();

            Stream input_stream = null;
            XmlDocument doc = null;
            XmlSelector selector = new XmlSelector();
            if (is_binary == true && UseXMLBinary == true)
            {
                XmlBinary xml_bin = new XmlBinary(bytes, file_path, false, string.IsNullOrEmpty(bin_encrypt_key) == false, bin_encrypt_key);
                selector.m_XMLBinary = xml_bin;
                selector.m_XMLBinaryCurrNode = xml_bin.Header;
            }
            else
            {
                if (UseXMLReader == true)
                {
                    try
                    {
                        XmlReader reader = null;
                        if (bytes != null)
                        {
                            input_stream = new MemoryStream(bytes);
                            reader = XmlReader.Create(input_stream);
                        }
                        else if (is_url == true)
                        {
                            reader = XmlReader.Create(xml_str_or_url);
                        }
                        else
                        {
                            reader = XmlReader.Create(new StringReader(xml_str_or_url));
                        }

                        selector.m_XMLReader = reader;
                        while (reader.Read() && (reader.NodeType != XmlNodeType.Element || reader.Name.Equals("DataList") == false))
                        {
                            // Do nothing...
                        }

                        LoadDataInternal(xml_str_or_url, is_url, bytes, file_path, is_binary, bin_encrypt_key, selector, input_stream, doc);
                        return;
                    }
                    catch (Exception e)
                    {
                        // TODO: Add Log
                        throw e;
                    }
                }

                try
                {
                    doc = new XmlDocument();
                    if (bytes != null)
                    {
                        input_stream = new MemoryStream(bytes);
                        doc.Load(input_stream);
                    }
                    else if (is_url == true)
                    {
                        doc.Load(xml_str_or_url);
                    }
                    else
                    {
                        doc.LoadXml(xml_str_or_url);
                    }

                    selector.m_XMLNode = doc.SelectSingleNode("DataList");
                }
                catch (Exception e)
                {
                    // TODO: Add Log
                    throw e;
                }
            }

            LoadDataInternal(xml_str_or_url, is_url, bytes, file_path, is_binary, bin_encrypt_key, selector, input_stream, doc);
        }

        //----------------------------------------------------------------------------------
        private void LoadDataInternal(string xml_str_or_url, bool is_url, byte[] bytes, string file_path, bool is_binary, string bin_encrypt_key, XmlSelector selector, Stream stream, XmlDocument doc)
        {
            Version = XmlUtil.ParseAttribute<int>(selector, "version", 0);
            DATA_XML_ID = XmlUtil.ParseAttribute<string>(selector, "data_id", "");
            CreatedTime = XmlUtil.ParseAttribute<DateTime>(selector, "created_time", DateTime.Now);
            LastLoadTime = DateTime.Now;

            if (DATA_XML_ID != DATA_ID)
                throw new Exception(string.Format("Data id({0} != {1}) Not matched!", this.DATA_ID, this.DATA_XML_ID));

            PreLoadData(selector);

            if (is_binary == true && UseXMLBinary == true)
            {
                List<XmlBinary.Node> row_list = selector.m_XMLBinary.RowList;
                int row_count = row_list.Count;
                for (int i = 0; i < row_count; i++)
                {
                    selector.m_XMLBinaryCurrNode = row_list[i];
                    ParsingRow(selector);
                }
            }
            else if (UseXMLReader == true)
            {
                while (selector.m_XMLReader.Read() == true)
                {
                    if (selector.m_XMLReader.NodeType == XmlNodeType.Element && selector.m_XMLReader.Name.Equals("Row"))
                        ParsingRow(selector);
                }
            }
            else
            {
                XmlNode root_node = selector.m_XMLNode;
                foreach (object obj in root_node.ChildNodes)
                {
                    XmlNode info_node = (XmlNode)obj;
                    if (info_node.NodeType != XmlNodeType.Comment)
                    {
                        selector.m_XMLNode = info_node;
                        ParsingRow(selector);
                    }
                }
            }

            PostLoadData(selector);

            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }

            if (doc != null && this.IsServerRegist)
            {
                string xml_text = "";
                StringWriter string_writer = new StringWriter();
                if (string_writer != null)
                {
                    XmlWriter xml_writer = XmlWriter.Create(string_writer);
                    if (xml_writer != null)
                    {
                        doc.WriteTo(xml_writer);
                        xml_writer.Flush();
                        xml_text = string_writer.GetStringBuilder().ToString();
                    }
                }

                if (string.IsNullOrEmpty(xml_text) == false)
                    m_Base64CompressedString = StringUtil.CompressedBase64String(xml_text, true);
            }
        }

        //----------------------------------------------------------------------------------
        protected void AddInfo(TableInfoBase info, int int_key, string str_key)
        {
            try
            {
                m_InfosByIntKey.Add(int_key, info);
                if (string.IsNullOrEmpty(str_key) == false)
                {
                    m_InfosByStrKey.Add(str_key, info);
                }

                m_Infos.Add((ValueType)((object)info));
            }
            catch (Exception e)
            {
                SHLog.LogError(e.Message);
            }
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByIntKey(int int_key)
        {
            TableInfoBase info;
            if (m_InfosByIntKey.TryGetValue(int_key, out info) == false)
                return default(ValueType);

            return (ValueType)((object)info);
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByStrKey(string str_key)
        {
            TableInfoBase info;
            if (m_InfosByStrKey.TryGetValue(str_key, out info) == false)
                return default(ValueType);

            return (ValueType)((object)info);
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByIndex(int idx)
        {
            if (idx >= 0 && idx < m_Infos.Count)
                return m_Infos[idx];

            return default(ValueType);
        }

        //----------------------------------------------------------------------------------
        public bool ContainsWithIndex(int idx)
        {
            return idx < m_Infos.Count;
        }

        //----------------------------------------------------------------------------------
        public bool ContainsIntKey(int int_key)
        {
            return m_InfosByIntKey.ContainsKey(int_key);
        }

        //----------------------------------------------------------------------------------
        public bool ContainsStrKey(string str_key)
        {
            return m_InfosByStrKey.ContainsKey(str_key);
        }
    }
}
