//////////////////////////////////////////////////////////////////////////
//
// DataListManager
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

using System.Collections.Generic;
using System.Linq;

namespace SHUtil.Data
{
    public class DataListManager : Singleton<DataListManager>
    {
        private Dictionary<string, delegate_DataLoadHandler> mDataLoadHandlers = new Dictionary<string, delegate_DataLoadHandler>();
        private Dictionary<string, delegate_GetVersionHandler> mGetVersionHandlers = new Dictionary<string, delegate_GetVersionHandler>();
        private Dictionary<string, delegate_GetServerData> mServerDataHandlers = new Dictionary<string, delegate_GetServerData>();
        private Dictionary<string, delegate_UseXmlBinary> mUseXmlBinaryHandlers = new Dictionary<string, delegate_UseXmlBinary>();

        public delegate void delegate_DataLoadHandler(string xml_str_or_url, bool is_url, byte[] bytes, string filepath, bool is_binary, string bin_encrypt_key);
        public delegate int delegate_GetVersionHandler();
        public delegate bool delegate_GetServerData(ref string server_data);
        public delegate bool delegate_UseXmlBinary();

        //----------------------------------------------------------------------------------
        public List<string> GetDataIDList()
        {
            return mDataLoadHandlers.Keys.ToList<string>();
        }

        //----------------------------------------------------------------------------------
        public List<string> GetServerDataIDList()
        {
            return mServerDataHandlers.Keys.ToList<string>();
        }

        //----------------------------------------------------------------------------------
        public void AddHandler(string data_id,
            delegate_DataLoadHandler load_handler,
            delegate_GetVersionHandler version_handler,
            delegate_GetServerData server_data_handler,
            delegate_UseXmlBinary use_xml_handler)
        {
            if (mDataLoadHandlers.ContainsKey(data_id) == true)
                mDataLoadHandlers.Remove(data_id);

            if (load_handler != null)
                mDataLoadHandlers.Add(data_id, load_handler);

            if (mGetVersionHandlers.ContainsKey(data_id) == true)
                mGetVersionHandlers.Remove(data_id);

            if (version_handler != null)
                mGetVersionHandlers.Add(data_id, version_handler);

            if (mServerDataHandlers.ContainsKey(data_id) == true)
                mServerDataHandlers.Remove(data_id);

            if (server_data_handler != null)
                mServerDataHandlers.Add(data_id, server_data_handler);

            if (mUseXmlBinaryHandlers.ContainsKey(data_id) == true)
                mUseXmlBinaryHandlers.Remove(data_id);

            if (use_xml_handler != null)
                mUseXmlBinaryHandlers.Add(data_id, use_xml_handler);
        }

        //----------------------------------------------------------------------------------
        public bool Load(string data_id, byte[] bytes, string file_path, bool is_binary, string bin_encrypt_key)
        {
            if (mDataLoadHandlers.ContainsKey(data_id) == true)
            {
                mDataLoadHandlers[data_id]("", false, bytes, file_path, is_binary, bin_encrypt_key);
                return true;
            }

            return false;
        }

        //----------------------------------------------------------------------------------
        public bool Load(string data_id, string xml_str)
        {
            if (mDataLoadHandlers.ContainsKey(data_id) == true)
            {
                mDataLoadHandlers[data_id](xml_str, false, null, "", false, "");
                return true;
            }

            return false;
        }

        //----------------------------------------------------------------------------------
        public int GetVersion(string data_id)
        {
            if (mGetVersionHandlers.ContainsKey(data_id) == true)
                return mGetVersionHandlers[data_id]();

            return -1;
        }

        //----------------------------------------------------------------------------------
        public bool GetServerData(string data_id, ref string server_data)
        {
            return mServerDataHandlers.ContainsKey(data_id) == true && mServerDataHandlers[data_id](ref server_data);
        }

        //----------------------------------------------------------------------------------
        public bool UseXmlBinary(string data_id)
        {
            return mUseXmlBinaryHandlers.ContainsKey(data_id) == true && mUseXmlBinaryHandlers[data_id]();
        }
    }
}
