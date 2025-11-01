//////////////////////////////////////////////////////////////////////////
//
// DataReloadBase
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

using System.Net;
using System.IO;
using SHUtil.Config;

namespace SHUtil.Data
{
    public abstract class DataReloadBase
    {
        public abstract string ReloadDataID { get; }
        public abstract string ReloadData();

        public string StaticPath { get; set; } = "";
        public string FileExtension { get; set; } = ".xml";

        //----------------------------------------------------------------------------------
        public bool LoadWithURL(out string load_url)
        {
            load_url = "";

            if (string.IsNullOrEmpty(StaticPath) == false)
            {
                bool is_success = CheckFileExist(StaticPath);
                if (is_success == true)
                    load_url = StaticPath;

                return is_success;
            }

            string base_file_path = GlobalConfig.Instance.GlobalDataPath;
            if (string.IsNullOrEmpty(base_file_path) == true)
                return false;

            string file_path = Path.Combine(base_file_path, $"{ReloadDataID}{FileExtension}");
            if (CheckFileExist(file_path) == true)
            {
                load_url = file_path;
                return true;
            }

            return false;
        }

        //----------------------------------------------------------------------------------
        protected bool CheckFileExist(string url)
        {
            bool is_exist = false;
            if (url.StartsWith("http") == true)
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Timeout = 1000;
                request.Method = "HEAD";

                HttpWebResponse response = null;
                try
                {
                    is_exist = true;
                    response = (HttpWebResponse)request.GetResponse();
                    if (response != null)
                        response.Close();

                    return is_exist;
                }
                catch (System.Exception e)
                {
                    if (response != null)
                        response.Close();

                    return false;
                }
            }

            if (File.Exists(url) == true)
                is_exist = true;

            return is_exist;
        }
    }
}
