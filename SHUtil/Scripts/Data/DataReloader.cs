//////////////////////////////////////////////////////////////////////////
//
// DataReloader
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
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SHUtil.Data
{
    public interface IExecuteAfterFirstReloadData
    {
        bool IsExecutedAfterFirstReload { get; set; }
        void ExecuteAfterFirstReload();
    }

    public class DataReloader : Singleton<DataReloader>
    {
        public class ReloadInfo
        {
            public string data_id = "";
            public int reload_count = 0;
            public string last_loaded_path = "";
            public ReloadDelegate reload_handler = null;
        }

        public string ReloadSaveFileName
        {
            get
            {
                return mReloadSaveFileName;
            }
            set
            {
                mReloadSaveFileName = value;
            }
        }

        public bool ReloadDataSave { get; set; } = true;
        public string ReloadSaveNameSuffix { get; set; } = "";
        public List<ReloadInfo> ReloadInfoList => mReloadInfoList;

        private string mReloadSaveFileName = "_reload_save";
        private List<ReloadInfo> mReloadInfoList = new List<ReloadInfo>();
        private List<IExecuteAfterFirstReloadData> mExecuteAfterFirstReloadList = new List<IExecuteAfterFirstReloadData>();

        public delegate string ReloadDelegate();

        private const string RELOAD_DATA_ROOT_NODE = "ReloadDataInfo";

        //----------------------------------------------------------------------------------
        public void AddReload(string data_id, ReloadDelegate handler)
        {
            ReloadInfo info = mReloadInfoList.Find(a => a.data_id == data_id);
            if (info != null)
                return;

            info = new ReloadInfo();
            info.data_id = data_id;
            info.reload_handler = handler;
            mReloadInfoList.Add(info);
        }

        //----------------------------------------------------------------------------------
        public void AddExecuteAfterFirstReload(IExecuteAfterFirstReloadData check)
        {
            if (mExecuteAfterFirstReloadList.Contains(check) == false)
                mExecuteAfterFirstReloadList.Add(check);
        }

        //----------------------------------------------------------------------------------
        private void ExecuteAfterFirstReload()
        {
            if (mExecuteAfterFirstReloadList.Count > 0)
            {
                foreach (IExecuteAfterFirstReloadData data in this.mExecuteAfterFirstReloadList)
                {
                    data.IsExecutedAfterFirstReload = true;
                    data.ExecuteAfterFirstReload();
                }

                mExecuteAfterFirstReloadList.Clear();
            }
        }

        //----------------------------------------------------------------------------------
        public string ReloadData(List<string> id_list)
        {
            List<string> data_id_list = id_list;
            if (data_id_list == null)
                data_id_list = mReloadInfoList.Select(a => a.data_id).ToList();

            StringBuilder response_builder = new StringBuilder("ReloadData:");
            if (data_id_list != null)
            {
                using (List<string>.Enumerator data_id_enumerator = data_id_list.GetEnumerator())
                {
                    while (data_id_enumerator.MoveNext() == true)
                    {
                        string id = data_id_enumerator.Current;
                        ReloadInfo info = mReloadInfoList.Find(a => a.data_id == id);
                        if (info != null && info.reload_handler != null)
                        {
                            info.reload_count += 1;
                            info.last_loaded_path = info.reload_handler();
                            if (string.IsNullOrEmpty(info.last_loaded_path) == true)
                                response_builder.Append($"\n[{id}:NOT]");
                            else
                                response_builder.Append($"\n[{id}:{info.last_loaded_path}:OK]");
                        }
                        else
                        {
                            response_builder.Append($"\n[{id}:IGNORE]");
                        }
                    }
                }
            }

            if (ReloadDataSave == true)
                SaveReloadDataInfo();

            ExecuteAfterFirstReload();

            return response_builder.ToString();
        }

        //----------------------------------------------------------------------------------
        public ReloadInfo GetInfo(string id)
        {
            return mReloadInfoList.Find(a => a.data_id == id);
        }

        //----------------------------------------------------------------------------------
        public void SaveReloadDataInfo()
        {
            if (ReloadDataSave == false)
                return;

            XmlDocument doc = new XmlDocument();
            XmlNode root_node = doc.AppendChild(doc.CreateElement(RELOAD_DATA_ROOT_NODE));
            foreach (ReloadInfo info in mReloadInfoList)
            {
                XmlNode data_node = root_node.AppendChild(root_node.OwnerDocument.CreateElement("Data"));
                XmlUtil.AddAttribute(data_node, "id", info.data_id);
            }

            doc.Save($"{mReloadSaveFileName}_{ReloadSaveNameSuffix}.xml");
        }

        //----------------------------------------------------------------------------------
        public void LoadFromSavedFile()
        {
            if (ReloadDataSave == false)
                return;

            try
            {
                string file_name = $"{mReloadSaveFileName}_{ReloadSaveNameSuffix}.xml";
                if (File.Exists(file_name) == true)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file_name);

                    XmlNode root_node = doc.SelectSingleNode(RELOAD_DATA_ROOT_NODE);
                    if (root_node != null)
                    {
                        XmlNodeList node_list = root_node.ChildNodes;
                        foreach (object node in node_list)
                        {
                            XmlNode child_node = (XmlNode)node;
                            string id = XmlUtil.ParseAttribute<string>(child_node, "id", "");
                            if (string.IsNullOrEmpty(id) == true)
                                continue;

                            ReloadInfo info = mReloadInfoList.Find(a => a.data_id == id);
                            if (info != null && info.reload_handler != null)
                                info.reload_handler();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: Add Log
            }
        }
    }
}
