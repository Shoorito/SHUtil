//////////////////////////////////////////////////////////////////////////
//
// I18NMultiTextBase
// 
// Created by Shoori.
//
// Copyright 2024-2025 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;
using System.Collections.Generic;

namespace SHUtil.I18N
{
    public abstract class I18NMultiTextBase<T>
    {
        public class I18NTextInfo
        {
            public RegionInfo region_info;
            public Dictionary<string, string> dic_values = new Dictionary<string, string>();
        }

        public Dictionary<string, I18NTextInfo> LanguageTexts => mLanguageTexts;
        protected Dictionary<string, I18NTextInfo> mLanguageTexts = new Dictionary<string, I18NTextInfo>();

        //----------------------------------------------------------------------------------
        public virtual void ClearTexts()
        {
            foreach (var regionInfo in mLanguageTexts)
            {
                regionInfo.Value.dic_values.Clear();
            }

            mLanguageTexts.Clear();
        }

        //----------------------------------------------------------------------------------
        protected void AddText(string regionCode, Dictionary<string, string> dicData)
        {
            if (string.IsNullOrEmpty(regionCode) || dicData == null || dicData.Count <= 0)
                return;

            if (mLanguageTexts.TryGetValue(regionCode, out var regionData) == false)
            {
                RegionInfo regionInfo = null;
                try
                {
                    regionInfo = new RegionInfo(regionCode);
                }
                catch (Exception e)
                {
                    SHLog.LogError(e.ToString());
                    return;
                }

                regionData = new I18NTextInfo();
                regionData.region_info = regionInfo;
                mLanguageTexts.Add(regionCode, regionData);
            }

            foreach (var kvpData in dicData)
            {
                if (regionData.dic_values.ContainsKey(kvpData.Key) == false)
                    regionData.dic_values.Add(kvpData.Key, kvpData.Value);
            }
        }

        //----------------------------------------------------------------------------------
        public string GetLanguageText(string regionCode, string textId)
        {
            if (mLanguageTexts.TryGetValue(regionCode, out var regionData) == false)
                return textId;

            if (regionData.dic_values.TryGetValue(textId, out var getText))
                return getText;

            return textId;
        }

        //----------------------------------------------------------------------------------
        public I18NTextInfo GetLanguageInfo(string regionCode)
        {
            if (mLanguageTexts.TryGetValue(regionCode, out var regionData))
                return regionData;

            return null;
        }

        //----------------------------------------------------------------------------------
        public bool ContainRegion(string regionCode)
        {
            return mLanguageTexts.ContainsKey(regionCode);
        }

        //----------------------------------------------------------------------------------
        public bool ContainText(string textId, string checkRegion = "")
        {
            foreach (var kvpRegion in mLanguageTexts)
            {
                if (string.IsNullOrEmpty(checkRegion) == false && checkRegion != kvpRegion.Value.region_info.TwoLetterISORegionName)
                    continue;

                if (kvpRegion.Value.dic_values.ContainsKey(textId) == false)
                    continue;

                return true;
            }

            return false;
        }
    }
}
