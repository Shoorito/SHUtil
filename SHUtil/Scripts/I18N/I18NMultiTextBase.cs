//////////////////////////////////////////////////////////////////////////
//
// I18NMultiTextBase
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
using System.Collections.Generic;
using System.Linq;
using SHUtil.Data;

namespace SHUtil.I18N
{
    public abstract class I18NMultiTextBase<T> : DataReloadSingleton<T> where T : DataReloadBase, new()
    {
        public class I18NTextInfo
        {
            public RegionInfo region_info;
            public Dictionary<string, string> dic_values = new Dictionary<string, string>();
        }

        protected Dictionary<string, Dictionary<string, Dictionary<string, I18NTextInfo>>> mLanguageTexts = new Dictionary<string, Dictionary<string, Dictionary<string, I18NTextInfo>>>();

        //----------------------------------------------------------------------------------
        public int CountAll()
        {
            return mLanguageTexts.Values.Sum(a => a.Values.Count);
        }

        //----------------------------------------------------------------------------------
        public virtual void ClearTexts()
        {
            mLanguageTexts.Clear();
        }

        //----------------------------------------------------------------------------------
        protected void AddText(string region_code, string text_group, string text_id, Dictionary<string, string> kvp_values)
        {
            if (string.IsNullOrEmpty(region_code) == true || string.IsNullOrEmpty(text_group) == true || kvp_values == null || kvp_values.Count <= 0)
                return;

            RegionInfo region_info = null;
            try
            {
                region_info = new RegionInfo(region_code);
            }
            catch (Exception e)
            {
                return;
            }

            Dictionary<string, Dictionary<string, I18NTextInfo>> dic_group = null;
            if (mLanguageTexts.ContainsKey(region_code) == false)
            {
                dic_group = new Dictionary<string, Dictionary<string, I18NTextInfo>>();
                mLanguageTexts.Add(region_code, dic_group);
            }
            else
            {
                dic_group = mLanguageTexts[region_code];
            }

            Dictionary<string, I18NTextInfo> dic_text_group = null;
            if (dic_group.ContainsKey(text_group) == false)
            {
                dic_text_group = new Dictionary<string, I18NTextInfo>();
                dic_group.Add(text_group, dic_text_group);
            }
            else
            {
                dic_text_group = dic_group[text_group];
            }

            if (dic_text_group.ContainsKey(text_id) == true)
                return;

            I18NTextInfo value_info = new I18NTextInfo();
            dic_text_group.Add(text_id, value_info);

            value_info.region_info = region_info;
            value_info.dic_values = kvp_values;
        }

        //----------------------------------------------------------------------------------
        public string GetLanguageText(string region_code, string group_name, string text_id, string value_type)
        {
            if (mLanguageTexts.ContainsKey(region_code) == false)
                return "";

            Dictionary<string, Dictionary<string, I18NTextInfo>> dic_groups = mLanguageTexts[region_code];
            if (dic_groups.ContainsKey(group_name) == false)
                return "";

            Dictionary<string, I18NTextInfo> dic_texts = dic_groups[group_name];
            if (dic_texts.ContainsKey(text_id) == false)
                return "";

            I18NTextInfo text_info = dic_texts[text_id];
            if (text_info == null || text_info.dic_values.ContainsKey(value_type) == false)
                return "";

            return text_info.dic_values[value_type];
        }

        //----------------------------------------------------------------------------------
        public string GetDefaultLanguageText(string region_code, string group_name, string text_id)
        {
            if (mLanguageTexts.ContainsKey(region_code) == false)
                return "";

            Dictionary<string, Dictionary<string, I18NTextInfo>> dic_groups = mLanguageTexts[region_code];
            if (dic_groups.ContainsKey(group_name) == false)
                return "";

            Dictionary<string, I18NTextInfo> dic_texts = dic_groups[group_name];
            if (dic_texts.ContainsKey(text_id) == false)
                return "";

            I18NTextInfo text_info = dic_texts[text_id];
            if (text_info == null)
                return "";

            Dictionary<string, string> dic_values = text_info.dic_values;
            foreach (KeyValuePair<string, string> info in dic_values)
            {
                return info.Value;
            }

            return "";
        }

        //----------------------------------------------------------------------------------
        public I18NTextInfo GetLanguageInfo(string region_code, string group_name, string text_id)
        {
            if (mLanguageTexts.ContainsKey(region_code) == false)
                return null;

            Dictionary<string, Dictionary<string, I18NTextInfo>> dic_groups = mLanguageTexts[region_code];
            if (dic_groups.ContainsKey(group_name) == false)
                return null;

            Dictionary<string, I18NTextInfo> dic_texts = dic_groups[group_name];
            if (dic_texts.ContainsKey(text_id) == false)
                return null;

            return dic_texts[text_id];
        }

        //----------------------------------------------------------------------------------
        public bool ContainGroupText(string region_code, string group_name)
        {
            if (mLanguageTexts.ContainsKey(region_code) == false)
                return false;

            return mLanguageTexts[region_code].ContainsKey(group_name);
        }

        //----------------------------------------------------------------------------------
        public bool ContainRegion(string region_code)
        {
            return mLanguageTexts.ContainsKey(region_code);
        }

        //----------------------------------------------------------------------------------
        public bool ContainText(string region_code, string group_name, string text_id)
        {
            if (ContainRegion(region_code) == false)
                return false;

            if (ContainGroupText(region_code, group_name) == false)
                return false;

            return mLanguageTexts[region_code][group_name].ContainsKey(text_id);
        }

        //----------------------------------------------------------------------------------
        public Dictionary<string, string> GetTextAllLangauges(string group_name, string text_id, string value_type)
        {
            if (string.IsNullOrEmpty(group_name) == true)
                return null;

            Dictionary<string, string> results = null;
            foreach (var region_kvp in mLanguageTexts) // var 정말 쓰기 싫지만... 너무 길어진다...
            {
                Dictionary<string, Dictionary<string, I18NTextInfo>> dic_group = region_kvp.Value;
                if (dic_group == null || dic_group.ContainsKey(group_name) == false)
                    continue;

                Dictionary<string, I18NTextInfo> dic_id = dic_group[group_name];
                if (dic_id == null || dic_id.ContainsKey(text_id) == false)
                    continue;

                I18NTextInfo text_info = dic_id[text_id];
                if (text_info == null || text_info.dic_values.ContainsKey(value_type) == false)
                    continue;

                if (results == null)
                    results = new Dictionary<string, string>();

                results.Add(region_kvp.Key, text_info.dic_values[value_type]);
            }

            return results;
        }
    }
}
