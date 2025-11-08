using System;
using System.Collections.Generic;
using System.Globalization;

namespace SHUtil.I18N
{
    public abstract class I18NMultiTextBase
    {
        public class I18NTextInfo
        {
            public RegionInfo Region { get; }
            public Dictionary<string, string> Texts { get; } = new Dictionary<string, string>();

            public I18NTextInfo(RegionInfo region)
            {
                Region = region;
            }
        }

        public IReadOnlyDictionary<string, I18NTextInfo> LanguageTexts => mLanguageTexts;
        protected Dictionary<string, I18NTextInfo> mLanguageTexts = new Dictionary<string, I18NTextInfo>();

        //----------------------------------------------------------------------------------
        public virtual void ClearTexts()
        {
            foreach (var info in mLanguageTexts.Values)
                info.Texts.Clear();

            mLanguageTexts.Clear();
        }

        //----------------------------------------------------------------------------------
        protected void AddText(string regionCode, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(regionCode) || data == null || data.Count == 0)
                return;

            if (!mLanguageTexts.TryGetValue(regionCode, out var regionData))
            {
                RegionInfo regionInfo;
                try
                {
                    regionInfo = new RegionInfo(regionCode);
                }
                catch (Exception e)
                {
                    SHLog.LogError(e.ToString());
                    return;
                }

                regionData = new I18NTextInfo(regionInfo);
                mLanguageTexts.Add(regionCode, regionData);
            }

            foreach (var kv in data)
            {
                if (!regionData.Texts.ContainsKey(kv.Key))
                    regionData.Texts.Add(kv.Key, kv.Value);
            }
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// regionCode 지역의 textId 텍스트를 반환합니다. 찾지 못하면 textId를 그대로 반환합니다.
        /// </summary>
        public string GetLanguageText(string regionCode, string textId)
        {
            if (!mLanguageTexts.TryGetValue(regionCode, out var regionData))
                return textId;

            return regionData.Texts.TryGetValue(textId, out var text) ? text : textId;
        }

        //----------------------------------------------------------------------------------
        public I18NTextInfo GetLanguageInfo(string regionCode)
        {
            mLanguageTexts.TryGetValue(regionCode, out var regionData);
            return regionData;
        }

        //----------------------------------------------------------------------------------
        public bool ContainRegion(string regionCode) => mLanguageTexts.ContainsKey(regionCode);

        //----------------------------------------------------------------------------------
        public bool ContainText(string textId, string checkRegion = "")
        {
            foreach (var kv in mLanguageTexts)
            {
                if (!string.IsNullOrEmpty(checkRegion) && checkRegion != kv.Value.Region.TwoLetterISORegionName)
                    continue;

                if (kv.Value.Texts.ContainsKey(textId))
                    return true;
            }

            return false;
        }
    }
}
