//////////////////////////////////////////////////////////////////////////
//
// I18NTextMultiLanguage
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

using SHUtil.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace SHUtil.I18N
{
    public class I18NTextMultiLanguage : I18NMultiTextBase<I18NTextMultiLanguage>
    {
        public event Action<string, string, bool> OnLoadedCallback;

        protected string mKeyName;
        protected string mValueName;
        protected string mDefaultRegion;
        protected bool mIsInited = false;
        protected XMLTableDataTypeInfo mCustomXMLDataInfo = null;

        //----------------------------------------------------------------------------------
        public virtual void Init(string dataKeyName, string dataValueName, string defaultRegion = "")
        {
            mKeyName = dataKeyName;
            mValueName = dataValueName;
            mDefaultRegion = string.IsNullOrEmpty(defaultRegion) || string.IsNullOrWhiteSpace(defaultRegion) ? "KR" : defaultRegion;
            mIsInited = true;
        }

        //----------------------------------------------------------------------------------
        public virtual void Dispose()
        {
            ClearTexts();
        }

        //----------------------------------------------------------------------------------
        public virtual void SetCustomXmlTableDataInfo(XMLTableDataTypeInfo info)
        {
            mCustomXMLDataInfo = info;
        }

        //----------------------------------------------------------------------------------
        public virtual void LoadDataAsync(string filePath, eTableDataType tableDataType, string regionCode = "KR", bool isBinary = false, bool isEncrypt = false, string encryptPassword = "")
        {
            if (mIsInited == false)
            {
                SHLog.LogError("[I18NText] Please call the Init function to proceed with the initialization process...");
                return;
            }

            if (PathUtil.IsValidPath(filePath) == false)
                return;

            Task.Run(() => InternalLoadDataAsync(filePath, tableDataType, regionCode, isBinary, isEncrypt, encryptPassword));
        }

        //----------------------------------------------------------------------------------
        protected virtual async Task InternalLoadDataAsync(string filePath, eTableDataType tableDataType, string regionCode = "KR", bool isBinary = false, bool isEncrypt = false, string encryptPassword = "")
        {
            var rawData = await File.ReadAllBytesAsync(filePath);
            if (rawData == null)
                return;

            var serializedData = rawData;
            if (isEncrypt && encryptPassword.Length > 0)
            {
                serializedData = await Task.Run(() => FileUtil.DecryptWithBytes(serializedData, encryptPassword));
                if (serializedData.Length <= 0)
                    return;
            }

            if (isBinary)
            {
                serializedData = await Task.Run(() => CLZF.Decompress(serializedData));
                if (serializedData.Length <= 0)
                    return;
            }

            var dicDataList = new Dictionary<string, string>();
            switch (tableDataType)
            {
                case eTableDataType.XML:
                    var tblDataTypeInfo = mCustomXMLDataInfo != null ? mCustomXMLDataInfo : TableDataTypeInfoUtil.DefaultXMLInfo;
                    var newDoc = new XmlDocument();
                    using (var ms = new MemoryStream(serializedData))
                    {
                        await Task.Run(() => newDoc.Load(ms));
                    }

                    var rootNode = newDoc.SelectSingleNode($".//{tblDataTypeInfo.DataRootName}");
                    if (rootNode == null)
                        return;

                    var rowList = rootNode.SelectNodes($"//{tblDataTypeInfo.DataRowName}");
                    if (rowList == null || rowList.Count <= 0)
                        return;

                    foreach (XmlNode rowData in rowList)
                    {
                        if (rowData.Attributes.Count < 2)
                            continue;

                        string dataKey = string.Empty;
                        if (mKeyName.Length > 0)
                            dataKey = rowData.Attributes[mKeyName]?.Value;

                        if (string.IsNullOrEmpty(dataKey))
                            dataKey = rowData.Attributes[0]?.Value;

                        if (string.IsNullOrEmpty(dataKey) || dicDataList.ContainsKey(dataKey))
                            continue;

                        string dataValue = string.Empty;
                        if (mValueName.Length > 0)
                            dataValue = rowData.Attributes[mValueName]?.Value;

                        if (string.IsNullOrEmpty(dataValue))
                            dataValue = rowData.Attributes[1]?.Value;

                        if (string.IsNullOrEmpty(dataValue))
                            continue;

                        dicDataList.Add(dataKey, dataValue);
                    }
                    break;

                case eTableDataType.Json:
                    using (var ms = new MemoryStream(rawData))
                    {
                        var dataList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(ms);
                        if (dataList == null || dataList.Count <= 0)
                            return;

                        foreach (var dicData in dataList)
                        {
                            if (dicData.Count < 2)
                                continue;

                            string dataKey = string.Empty;
                            if (mKeyName.Length > 0 && dicData.TryGetValue(mKeyName, out var getDataKey))
                                dataKey = getDataKey;

                            if (string.IsNullOrEmpty(dataKey))
                                dataKey = dicData.FirstOrDefault().Key;

                            if (string.IsNullOrEmpty(dataKey) || dicDataList.ContainsKey(dataKey))
                                continue;

                            string dataValue = string.Empty;
                            if (mValueName.Length > 0 && dicData.TryGetValue(mValueName, out var getDataValue))
                                dataValue = getDataValue;

                            if (string.IsNullOrEmpty(dataValue))
                            {
                                int findCount = 0;
                                foreach (var kvpData in dicData)
                                {
                                    if (findCount == 1)
                                    {
                                        dataValue = kvpData.Value;
                                        break;
                                    }
                                    else
                                    {
                                        findCount++;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(dataValue))
                                continue;

                            dicDataList.Add(dataKey, dataValue);
                        }
                    }
                    break;

                default:
                    SHLog.LogError("[I18NText] UnSupported Table Data Type...");
                    return;
            }

            if (dicDataList.Count <= 0)
            {
                OnLoadedCallback?.Invoke(filePath, regionCode, false);
                return;
            }

            AddText(regionCode, dicDataList);
            OnLoadedCallback?.Invoke(filePath, regionCode, true);
        }

        //----------------------------------------------------------------------------------
        public string GetText(string textId, string regionCode = "")
        {
            return GetLanguageText(string.IsNullOrEmpty(regionCode) || string.IsNullOrWhiteSpace(regionCode) ? mDefaultRegion : regionCode, textId);
        }
    }
}
