//////////////////////////////////////////////////////////////////////////
//
// GlobalConfig
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

using System.Xml;
using System.IO;

namespace SHUtil.Config
{
    public class GlobalConfig
    {
        public string GlobalDataPath => mGlobalTablePath;
        public string GlobalConfigPath => mGlobalConfigPath;
        public string GlobalI18NPath => mGlobalI18NPath;

        private string mGlobalConfigPath = "";
        private string mGlobalTablePath = "";
        private string mGlobalI18NPath = "";

        public static GlobalConfig Instance
        {
            get
            {
                if (sInstance == null)
                {
                    sInstance = new GlobalConfig();
                    sInstance.Init();
                }

                return sInstance;
            }
        }

        private static GlobalConfig sInstance = null;

        public const string FILE_NAME = "global_config.xml";
        public const string DEFAULT_ROOT_PATH = "DATA";
        public const string DEFAULT_TABLE_ROOT_PATH = "Table";
        public const string DEFAULT_TABLE_I18N_ROOT_PATH = "Table/I18NText";
        public const string DEFAULT_CONFIG_ROOT_PATH = "Config";

        //----------------------------------------------------------------------------------
        public void Init()
        {
            mGlobalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_ROOT_PATH, DEFAULT_CONFIG_ROOT_PATH, FILE_NAME);
            Load();
        }

        //----------------------------------------------------------------------------------
        public void Load(bool forced_reset = false)
        {
            bool is_dirty = false;
            XmlDocument config_doc = null;
            if (File.Exists(mGlobalConfigPath) == false)
            {
                string dir_config = Path.GetDirectoryName(mGlobalConfigPath);
                if (Directory.Exists(dir_config) == false)
                    Directory.CreateDirectory(dir_config);

                is_dirty = true;
                config_doc = new XmlDocument();
                XmlDeclaration xml_declaration = config_doc.CreateXmlDeclaration("1.0", "utf-8", null);
                config_doc.AppendChild(xml_declaration);

                XmlNode root_node = config_doc.CreateElement("GlobalConfig");
                config_doc.AppendChild(root_node);

                string dir_cur = Directory.GetCurrentDirectory();
                mGlobalTablePath = Path.Combine(dir_cur, DEFAULT_ROOT_PATH, DEFAULT_TABLE_ROOT_PATH);

                XmlNode new_data_path_node = XmlUtil.AddNode(root_node, "DataPath");
                XmlUtil.AddAttribute(new_data_path_node, "path", mGlobalTablePath);

                XmlNode new_i18n_text_path_node = XmlUtil.AddNode(root_node, "I18NPath");
                mGlobalI18NPath = Path.Combine(dir_cur, DEFAULT_ROOT_PATH, DEFAULT_TABLE_I18N_ROOT_PATH);
                XmlUtil.AddAttribute(new_i18n_text_path_node, "path", mGlobalI18NPath);
            }
            else
            {
                config_doc = XmlUtil.LoadXmlFromFile(mGlobalConfigPath);

                XmlNodeList data_path_node = config_doc.GetElementsByTagName("DataPath");
                if (data_path_node.Count > 0)
                {
                    XmlAttributeCollection attrs = data_path_node[0].Attributes;
                    if (attrs != null && attrs.Count > 0)
                    {
                        mGlobalTablePath = attrs["path"].Value;
                        if (string.IsNullOrEmpty(mGlobalTablePath) == true)
                        {
                            is_dirty = true;
                            mGlobalTablePath = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_ROOT_PATH, DEFAULT_TABLE_ROOT_PATH);
                            attrs["path"].Value = mGlobalTablePath;
                        }
                    }
                }

                XmlNodeList i18n_path_node = config_doc.GetElementsByTagName("I18NPath");
                if (data_path_node.Count > 0)
                {
                    XmlAttributeCollection attrs = data_path_node[0].Attributes;
                    if (attrs != null && attrs.Count > 0)
                        mGlobalI18NPath = attrs["path"].Value;

                    if (string.IsNullOrEmpty(mGlobalI18NPath) == true)
                    {
                        is_dirty = true;
                        mGlobalI18NPath = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_ROOT_PATH, DEFAULT_TABLE_I18N_ROOT_PATH);
                        attrs["path"].Value = mGlobalI18NPath;
                    }
                }
            }

            if (is_dirty == true)
                XmlUtil.SaveXmlDocToFile(mGlobalConfigPath, config_doc);
        }
    }
}
