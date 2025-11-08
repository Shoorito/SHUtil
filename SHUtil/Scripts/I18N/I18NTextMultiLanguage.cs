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
    public class I18NTextMultiLanguage : I18NMultiTextBase
    {
        public event Action<string, string, bool> OnLoadedCallback;

        protected string mKeyName;
        protected string mValueName;
        protected string mDefaultRegion;
        protected bool mIsInited;
        protected XMLTableDataTypeInfo mCustomXMLDataInfo;

        //----------------------------------------------------------------------------------
        public I18NTextMultiLanguage() { }

        /// <summary>키/값 컬럼명과 기본 지역 코드를 지정해 바로 초기화합니다.</summary>
        public I18NTextMultiLanguage(string keyName, string valueName, string defaultRegion = "KR")
        {
            Init(keyName, valueName, defaultRegion);
        }

        //----------------------------------------------------------------------------------
        public virtual void Init(string keyName, string valueName, string defaultRegion = "KR")
        {
            mKeyName       = keyName;
            mValueName     = valueName;
            mDefaultRegion = string.IsNullOrWhiteSpace(defaultRegion) ? "KR" : defaultRegion;
            mIsInited      = true;
        }

        //----------------------------------------------------------------------------------
        public virtual void Dispose() => ClearTexts();

        //----------------------------------------------------------------------------------
        public virtual void SetCustomXmlTableDataInfo(XMLTableDataTypeInfo info)
        {
            mCustomXMLDataInfo = info;
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// 파일에서 지역화 데이터를 비동기로 로드합니다.
        /// 완료 여부는 OnLoadedCallback 이벤트로 수신합니다.
        /// </summary>
        public virtual void LoadDataAsync(string filePath, eTableDataType tableDataType, string regionCode = "KR",
            bool isBinary = false, bool isEncrypt = false, string encryptPassword = "")
        {
            if (!mIsInited)
            {
                SHLog.LogError("[I18NText] Init을 먼저 호출해야 합니다.");
                return;
            }

            if (!PathUtil.IsValidPath(filePath))
                return;

            Task.Run(() => InternalLoadDataAsync(filePath, tableDataType, regionCode, isBinary, isEncrypt, encryptPassword));
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// 파일에서 지역화 데이터를 비동기로 로드하고 완료를 await 할 수 있는 Task를 반환합니다.
        /// </summary>
        public virtual Task LoadAsync(string filePath, eTableDataType tableDataType, string regionCode = "KR",
            bool isBinary = false, bool isEncrypt = false, string encryptPassword = "")
        {
            if (!mIsInited)
            {
                SHLog.LogError("[I18NText] Init을 먼저 호출해야 합니다.");
                return Task.CompletedTask;
            }

            if (!PathUtil.IsValidPath(filePath))
                return Task.CompletedTask;

            return InternalLoadDataAsync(filePath, tableDataType, regionCode, isBinary, isEncrypt, encryptPassword);
        }

        //----------------------------------------------------------------------------------
        protected virtual async Task InternalLoadDataAsync(string filePath, eTableDataType tableDataType,
            string regionCode, bool isBinary, bool isEncrypt, string encryptPassword)
        {
            var rawData = await File.ReadAllBytesAsync(filePath);
            if (rawData == null || rawData.Length == 0)
                return;

            var serializedData = rawData;

            if (isEncrypt && !string.IsNullOrEmpty(encryptPassword))
            {
                serializedData = await Task.Run(() => FileUtil.DecryptWithBytes(serializedData, encryptPassword));
                if (serializedData == null || serializedData.Length == 0)
                    return;
            }

            if (isBinary)
            {
                serializedData = await Task.Run(() => CLZF.Decompress(serializedData));
                if (serializedData == null || serializedData.Length == 0)
                    return;
            }

            var dicDataList = new Dictionary<string, string>();

            switch (tableDataType)
            {
                case eTableDataType.XML:
                {
                    var tblDataTypeInfo = mCustomXMLDataInfo ?? TableDataTypeInfoUtil.DefaultXMLInfo;
                    var newDoc = new XmlDocument();
                    using (var ms = new MemoryStream(serializedData))
                        await Task.Run(() => newDoc.Load(ms));

                    var rootNode = newDoc.SelectSingleNode($".//{tblDataTypeInfo.DataRootName}");
                    if (rootNode == null)
                        break;

                    var rowList = rootNode.SelectNodes($"//{tblDataTypeInfo.DataRowName}");
                    if (rowList == null || rowList.Count == 0)
                        break;

                    foreach (XmlNode rowData in rowList)
                    {
                        if (rowData.Attributes.Count < 2)
                            continue;

                        var dataKey = (!string.IsNullOrEmpty(mKeyName) ? rowData.Attributes[mKeyName]?.Value : null)
                                      ?? rowData.Attributes[0]?.Value;

                        if (string.IsNullOrEmpty(dataKey) || dicDataList.ContainsKey(dataKey))
                            continue;

                        var dataValue = (!string.IsNullOrEmpty(mValueName) ? rowData.Attributes[mValueName]?.Value : null)
                                        ?? rowData.Attributes[1]?.Value;

                        if (!string.IsNullOrEmpty(dataValue))
                            dicDataList.Add(dataKey, dataValue);
                    }
                    break;
                }

                case eTableDataType.Json:
                {
                    using (var ms = new MemoryStream(serializedData))
                    {
                        var dataList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(ms);
                        if (dataList == null || dataList.Count == 0)
                            break;

                        foreach (var dicData in dataList)
                        {
                            if (dicData.Count < 2)
                                continue;

                            var dataKey = (!string.IsNullOrEmpty(mKeyName) && dicData.TryGetValue(mKeyName, out var k) ? k : null)
                                          ?? dicData.Keys.FirstOrDefault();

                            if (string.IsNullOrEmpty(dataKey) || dicDataList.ContainsKey(dataKey))
                                continue;

                            var dataValue = (!string.IsNullOrEmpty(mValueName) && dicData.TryGetValue(mValueName, out var v) ? v : null)
                                            ?? dicData.Values.Skip(1).FirstOrDefault();

                            if (!string.IsNullOrEmpty(dataValue))
                                dicDataList.Add(dataKey, dataValue);
                        }
                    }
                    break;
                }

                default:
                    SHLog.LogError("[I18NText] 지원하지 않는 데이터 타입입니다.");
                    return;
            }

            if (dicDataList.Count == 0)
            {
                OnLoadedCallback?.Invoke(filePath, regionCode, false);
                return;
            }

            AddText(regionCode, dicDataList);
            OnLoadedCallback?.Invoke(filePath, regionCode, true);
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// textId에 해당하는 텍스트를 반환합니다.
        /// regionCode를 생략하면 Init에서 지정한 기본 지역 코드를 사용합니다.
        /// 찾지 못하면 textId를 그대로 반환합니다.
        /// </summary>
        public string GetText(string textId, string regionCode = "")
            => GetLanguageText(string.IsNullOrWhiteSpace(regionCode) ? mDefaultRegion : regionCode, textId);
    }
}
