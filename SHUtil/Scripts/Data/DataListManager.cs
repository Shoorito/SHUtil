//////////////////////////////////////////////////////////////////////////
//
// DataListManager
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

namespace SHUtil.Data
{
    public class DataListManager : AutoSingleton<DataListManager>
    {
        protected Dictionary<Type, ITableOwnerBase> mRegisteredTables = new Dictionary<Type, ITableOwnerBase>();
        protected string mDataRootPath;

        //----------------------------------------------------------------------------------
        public void Init(string dataRootPath)
        {
            if (!PathUtil.IsValidPath(dataRootPath))
                return;

            mDataRootPath = dataRootPath;
        }

        //----------------------------------------------------------------------------------
        public void Load<T>(TableOwnerBase<T> table, string fileExtension, bool loadAsync = false, string encryptPassword = "") where T : TableInfoBase, new()
        {
            if (table == null || !PathUtil.IsValidPath(mDataRootPath))
                return;

            string loadPath = Path.Combine(mDataRootPath, $"{table.FileName}.{fileExtension}");
            if (loadAsync)
                table.LoadDataAsync(loadPath, encryptPassword);
            else
                table.LoadData(loadPath, encryptPassword);

            if (!mRegisteredTables.ContainsKey(table.GetType()))
                mRegisteredTables.Add(table.GetType(), table);
        }

        //----------------------------------------------------------------------------------
        public T Get<T>() where T : class, ITableOwnerBase, new()
        {
            if (mRegisteredTables.TryGetValue(typeof(T), out var result))
                return result as T;

            return default;
        }

        //----------------------------------------------------------------------------------
        public override void DisposeSingleton()
        {
            foreach (var table in mRegisteredTables.Values)
                table.Dispose();

            mRegisteredTables.Clear();
            base.DisposeSingleton();
        }

        //----------------------------------------------------------------------------------
        public List<string> GetDataIDList()
        {
            return mRegisteredTables.Values.Select(a => a.FileName).ToList();
        }
    }
}
