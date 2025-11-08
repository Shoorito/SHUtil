//////////////////////////////////////////////////////////////////////////
//
// TableOwnerBase
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace SHUtil.Table
{
    public enum eTableDataType
    {
        XML,
        Json,
    }

    public interface ITableOwnerBase
    {
        public string FileName { get; }

        public void Init();
        public void Dispose();
        public void LoadData(string filePath, string encryptPassword = "");
        public void LoadData(byte[] rawData);
        public void LoadDataAsync(string filePath, string encryptPassword = "");
        public void LoadDataAsync(byte[] rawData);
    }

    //----------------------------------------------------------------------------------
    public abstract class TableOwnerBase<ValueType> : ITableOwnerBase where ValueType : TableInfoBase, new()
    {
        public abstract string FileName { get; } // 데이터 파일 명입니다
        public abstract bool IsStringKey { get; } // 데이터를 읽을 시 키 타입이 문자열인지, 정수인지 정합니다
        public abstract bool CheckSameKey { get; } // 데이터를 읽을 시 동일 Key 허용 여부를 정합니다, 해당 옵션이 False 일 경우 AppendData로서 처리됩니다
        public abstract eTableDataType TableDataType { get; } // 읽을 데이터 파일 타입을 정의합니다
        public abstract ITableDataTypeInfo TableDataTypeInfo { get; } // 테이블 데이터 타입에 맞는 Setting 클래스를 정의합니다
        public virtual bool IsEncrypted => false; // 읽어들인 파일이 암호화 되어 있는지 체크합니다, 기본 값: False
        public virtual bool IsCompressed => false; // 읽어들인 파일이 압축 되어 있는지 체크합니다, 기본 값: False
        public virtual bool IsInvalid { get; protected set; } = false; // 로드에 실패했을 경우 해당 값을 True로 지정합니다

        public int Count => IsStringKey ? mInfosForStr.Count : mInfosForInt.Count;

        protected List<ValueType> mInfos = new List<ValueType>();
        protected Dictionary<int, ValueType> mInfosForInt = new Dictionary<int, ValueType>();
        protected Dictionary<string, ValueType> mInfosForStr = new Dictionary<string, ValueType>();
        protected string mEncryptPassword;

        public bool LoadCompleted => mLoadCompleted;
        protected bool mLoadCompleted = false;

        //----------------------------------------------------------------------------------
        public virtual void Init()
        {
        }

        //----------------------------------------------------------------------------------
        public virtual void Dispose()
        {
        }

        //----------------------------------------------------------------------------------
        public virtual void LoadData(string filePath, string encryptPassword = "")
        {
            if (mLoadCompleted)
                return;

            if (PathUtil.IsValidPath(filePath, true) == false)
            {
                IsInvalid = true;
                return;
            }

            mEncryptPassword = encryptPassword;

            var rawData = File.ReadAllBytes(filePath);
            if (rawData == null || rawData.Length <= 0)
            {
                IsInvalid = true;
                return;
            }

            LoadData(rawData);
        }

        //----------------------------------------------------------------------------------
        public virtual void LoadData(byte[] rawData)
        {
            if (mLoadCompleted)
                return;

            if (rawData == null || rawData.Length <= 0)
            {
                IsInvalid = true;
                return;
            }

            var bytes = rawData;
            if (IsEncrypted && mEncryptPassword.Length > 0)
            {
                try
                {
                    bytes = FileUtil.DecryptWithBytes(rawData, mEncryptPassword);
                }
                catch
                {
                    IsInvalid = true;
                    return;
                }

                if (bytes == null || bytes.Length <= 0)
                {
                    IsInvalid = true;
                    return;
                }
            }

            if (IsCompressed)
            {
                try
                {
                    bytes = CLZF.Decompress(bytes);
                }
                catch
                {
                    IsInvalid = true;
                    return;
                }
            }

            Preload();

            switch (TableDataType)
            {
                case eTableDataType.XML:
                    LoadDataInternalWithXML(bytes);
                    break;

                case eTableDataType.Json:
                    LoadDataInternalWithJson(bytes);
                    break;

                default:
                    IsInvalid = true;
                    return;
            }

            if (IsInvalid == false)
                Postload();

            mLoadCompleted = true;
        }

        //----------------------------------------------------------------------------------
        private void LoadDataInternalWithXML(byte[] rawData)
        {
            if (rawData == null || rawData.Length <= 0)
            {
                IsInvalid = true;
                return;
            }

            var tblDataTypeInfo = TableDataTypeInfo as XMLTableDataTypeInfo;
            if (tblDataTypeInfo == null)
                tblDataTypeInfo = TableDataTypeInfoUtil.DefaultXMLInfo;

            var newDoc = new XmlDocument();
            using (var ms = new MemoryStream(rawData))
            {
                newDoc.Load(ms);
            }

            var rootNode = newDoc.SelectSingleNode($".//{tblDataTypeInfo.DataRootName}");
            if (rootNode == null)
            {
                IsInvalid = true;
                return;
            }

            var rowList = rootNode.SelectNodes($"//{tblDataTypeInfo.DataRowName}");
            if (rowList == null || rowList.Count <= 0)
                return;

            int rowIdx = 0;
            foreach (XmlNode rowNode in rowList)
            {
                if (rowNode.Attributes.Count < 1)
                    continue;

                var strKey = rowNode.Attributes[0].Value;
                if (strKey == null)
                    continue;

                SetInfoData(rowIdx, strKey, rowNode);
                rowIdx++;
            }
        }

        //----------------------------------------------------------------------------------
        private void LoadDataInternalWithJson(byte[] rawData)
        {
            if (rawData == null || rawData.Length <= 0)
            {
                IsInvalid = true;
                return;
            }

            using (var ms = new MemoryStream(rawData))
            {
                var dataList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(ms);
                if (dataList == null || dataList.Count <= 0)
                    return;

                int rowIdx = 0;
                foreach (var dicData in dataList)
                {
                    if (dicData.Count < 1)
                        continue;

                    var strKey = dicData.FirstOrDefault().Value;
                    if (strKey == null)
                        continue;

                    SetInfoData(rowIdx, strKey, dicData);
                    rowIdx++;
                }
            }
        }

        //----------------------------------------------------------------------------------
        private void SetInfoData(int rowIdx, string rawKey, object value)
        {
            if (string.IsNullOrEmpty(rawKey) || string.IsNullOrWhiteSpace(rawKey))
                return;

            int intKey = 0;
            if (IsStringKey == false)
            {
                try
                {
                    intKey = Convert.ToInt32(rawKey);
                }
                catch
                {
                    return;
                }

                if (mInfosForInt.TryGetValue(intKey, out var cachedValue))
                {
                    if (CheckSameKey == false)
                        cachedValue.LoadAppend(value);

                    return;
                }
            }
            else
            {
                if (mInfosForStr.TryGetValue(rawKey, out var cachedValue))
                {
                    if (CheckSameKey == false)
                        cachedValue.LoadAppend(value);

                    return;
                }
            }

            var valueData = Activator.CreateInstance<ValueType>();
            if (IsStringKey)
                valueData.InitById(rowIdx, rawKey);
            else
                valueData.InitByIdn(rowIdx, intKey);

            valueData.Load(value);

            if (IsStringKey)
                mInfosForStr.Add(rawKey, valueData);
            else
                mInfosForInt.Add(intKey, valueData);

            mInfos.Add(valueData);
        }

        public void LoadDataAsync(string filePath, string encryptPassword = "")
        {
            Task.Run(() => InternalLoadDataAsync(filePath, encryptPassword));
        }

        public void LoadDataAsync(byte[] rawData)
        {
            Task.Run(() => InternalLoadDataAsync(rawData));
        }

        //----------------------------------------------------------------------------------
        protected async virtual Task InternalLoadDataAsync(string filePath, string encryptPassword = "")
        {
            if (mLoadCompleted)
                return;

            bool isValidPath = await Task.Run(() => PathUtil.IsValidPath(filePath, true));
            if (isValidPath == false)
            {
                IsInvalid = true;
                return;
            }

            mEncryptPassword = encryptPassword;

            try
            {
                var rawData = await File.ReadAllBytesAsync(filePath);
                if (rawData == null || rawData.Length <= 0)
                {
                    IsInvalid = true;
                    return;
                }

                await InternalLoadDataAsync(rawData);
            }
            catch (Exception ex)
            {
                SHLog.LogError(ex.ToString());
                IsInvalid = true;
                return;
            }
        }

        //----------------------------------------------------------------------------------
        protected async virtual Task InternalLoadDataAsync(byte[] rawData)
        {
            if (mLoadCompleted)
                return;

            if (rawData == null || rawData.Length <= 0)
            {
                IsInvalid = true;
                return;
            }

            var bytes = rawData;
            if (IsEncrypted && mEncryptPassword.Length > 0)
            {
                try
                {
                    bytes = await Task.Run(() => FileUtil.DecryptWithBytes(rawData, mEncryptPassword));
                }
                catch
                {
                    IsInvalid = true;
                    return;
                }

                if (bytes == null || bytes.Length <= 0)
                {
                    IsInvalid = true;
                    return;
                }
            }

            if (IsCompressed)
            {
                try
                {
                    bytes = await Task.Run(() => CLZF.Decompress(bytes));
                }
                catch
                {
                    IsInvalid = true;
                    return;
                }
            }

            await Task.Run(Preload);

            switch (TableDataType)
            {
                case eTableDataType.XML:
                    await Task.Run(() => LoadDataInternalWithXML(bytes));
                    break;

                case eTableDataType.Json:
                    await Task.Run(() => LoadDataInternalWithJson(bytes));
                    break;

                default:
                    IsInvalid = true;
                    return;
            }

            if (IsInvalid == false)
                await Task.Run(Postload);

            mLoadCompleted = true;
        }

        //----------------------------------------------------------------------------------
        protected virtual void Preload()
        {
        }

        //----------------------------------------------------------------------------------
        protected virtual void Postload()
        {
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByIntKey(int intKey)
        {
            if (IsStringKey)
                return default;

            if (mInfosForInt.TryGetValue(intKey, out var info) == false)
                return default;

            return info;
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByStrKey(string strKey)
        {
            if (IsStringKey == false)
                return default;

            if (mInfosForStr.TryGetValue(strKey, out var info) == false)
                return default;

            return info;
        }

        //----------------------------------------------------------------------------------
        public ValueType GetInfoByIndex(int idx)
        {
            if (idx < 0 || idx >= mInfos.Count)
                return default;

            return mInfos[idx];
        }

        //----------------------------------------------------------------------------------
        public bool ContainsIntKey(int intKey)
        {
            return mInfosForInt.ContainsKey(intKey);
        }

        //----------------------------------------------------------------------------------
        public bool ContainsStrKey(string strKey)
        {
            return mInfosForStr.ContainsKey(strKey);
        }
    }
}
