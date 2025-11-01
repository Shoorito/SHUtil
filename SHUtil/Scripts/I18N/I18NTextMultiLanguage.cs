//////////////////////////////////////////////////////////////////////////
//
// I18NTextMultiLanguage
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
using System.Globalization;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using SHUtil.Config;


namespace SHUtil.I18N
{
    public class I18NTextMultiLanguage : I18NMultiTextBase<I18NTextMultiLanguage>
    {
        public override string ReloadDataID => "I18NText";
        protected string DEFAULT_REGION => "KR";
        protected string DEFAULT_TEXT_GROUP => "GameText";
        public string PrefixSearchTable { get; set; } = "TBL";

        //----------------------------------------------------------------------------------
        public I18NTextMultiLanguage()
        {
            RegistServer();
        }

        //----------------------------------------------------------------------------------
        public override string ReloadData()
        {
            ClearTexts();

            string base_path = GlobalConfig.Instance.GlobalI18NPath;
            if (string.IsNullOrEmpty(StaticPath) == false)
                base_path = StaticPath;

            if (Directory.Exists(base_path) == false)
                return null;

            string[] i18n_files = Directory.GetFiles(base_path, $"{PrefixSearchTable}*.xml", SearchOption.AllDirectories);
            if (i18n_files == null || i18n_files.Length <= 0)
                return null;

            int file_count = i18n_files.Length;
            for (int i = 0; i < file_count; i++)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(i18n_files[i]);
                LoadData(doc);
            }

            return base_path;
        }

        //----------------------------------------------------------------------------------
        private void LoadData(XmlDocument doc)
        {
            if (doc == null)
                return;

            XmlNode root_node = doc.SelectSingleNode("DataList");
            if (root_node == null || root_node.ChildNodes.Count <= 0)
                return;

            string region_code = root_node.Attributes["language"].Value;

            XmlNodeList tbl_root_node_list = root_node.ChildNodes;
            foreach (XmlNode tbl_root_node in tbl_root_node_list)
            {
                if (tbl_root_node.ChildNodes.Count <= 0)
                    continue;

                XmlNodeList tbl_node_list = tbl_root_node.ChildNodes;
                foreach (XmlNode tbl_node in tbl_node_list)
                {
                    if (tbl_node.Attributes.Count <= 0)
                        continue;

                    string data_id = tbl_node.Attributes["ID"].Value;
                    if (string.IsNullOrEmpty(data_id) == true)
                        continue;

                    Dictionary<string, string> kvp_values = new Dictionary<string, string>();
                    XmlAttributeCollection attr_list = tbl_node.Attributes;
                    foreach (XmlAttribute attr in attr_list)
                    {
                        if (attr.Name == "ID")
                            continue;

                        kvp_values.Add(attr.Name, attr.Value);
                    }

                    AddText(region_code, tbl_root_node.Name, data_id, kvp_values);
                }
            }
        }

        //----------------------------------------------------------------------------------
        public string GetText(string text_id, string value_type)
        {
            return GetText(DEFAULT_TEXT_GROUP, text_id, value_type);
        }

        //----------------------------------------------------------------------------------
        public string GetText(string text_group, string text_id, string value_type)
        {
            return GetText(DEFAULT_REGION, text_group, text_id, value_type);
        }

        //----------------------------------------------------------------------------------
        public string GetText(string region_code, string text_group, string text_id, string value_type)
        {
            if (string.IsNullOrEmpty(region_code) == true || string.IsNullOrEmpty(text_group) == true || string.IsNullOrEmpty(text_id) == true || string.IsNullOrEmpty(value_type) == true)
                return text_id;

            try
            {
                RegionInfo region_info = new RegionInfo(region_code);
            }
            catch (Exception e)
            {
                return text_group;
            }

            return GetLanguageText(region_code, text_group, text_id, value_type);
        }

        //----------------------------------------------------------------------------------
        public string GetDefaultText(string text_id)
        {
            return GetDefaultText(DEFAULT_TEXT_GROUP, text_id);
        }

        //----------------------------------------------------------------------------------
        public string GetDefaultText(string text_group, string text_id)
        {
            return GetDefaultText(DEFAULT_REGION, text_group, text_id);
        }

        //----------------------------------------------------------------------------------
        public string GetDefaultText(string region_code, string text_group, string text_id)
        {
            if (string.IsNullOrEmpty(region_code) == true || string.IsNullOrEmpty(text_group) == true || string.IsNullOrEmpty(text_id) == true)
                return "";

            try
            {
                RegionInfo region_info = new RegionInfo(region_code);
            }
            catch (Exception e)
            {
                return "";
            }

            return GetDefaultLanguageText(region_code, text_group, text_id);
        }
    }
}
