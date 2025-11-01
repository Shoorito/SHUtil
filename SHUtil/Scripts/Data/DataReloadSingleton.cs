//////////////////////////////////////////////////////////////////////////
//
// DataReloadSingleton
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

namespace SHUtil.Data
{
    public abstract class DataReloadSingleton<T> : DataReloadBase where T : DataReloadBase, new()
    {
        private static T sInstance = null;
        private static object sLock = new object();

        //----------------------------------------------------------------------------------
        public static T Instance
        {
            get
            {
                if (sInstance == null)
                {
                    object obj_lock = sLock;
                    lock (obj_lock)
                    {
                        MakeInstance();
                    }
                }

                return sInstance;
            }
        }

        //----------------------------------------------------------------------------------
        public static void MakeInstance()
        {
            if (sInstance == null)
                sInstance = Activator.CreateInstance<T>();
        }

        //----------------------------------------------------------------------------------
        public static void ClearInstance()
        {
            if (sInstance != null)
                sInstance = null;
        }

        //----------------------------------------------------------------------------------
        public virtual void RegistServer()
        {
            Singleton<DataReloader>.Instance.AddReload(ReloadDataID, new DataReloader.ReloadDelegate(ReloadHandler));
        }

        //----------------------------------------------------------------------------------
        private static string ReloadHandler()
        {
            string static_path = sInstance.StaticPath;
            string file_ext = sInstance.FileExtension;

            ClearInstance();
            MakeInstance();

            sInstance.StaticPath = static_path;
            sInstance.FileExtension = file_ext;

            return sInstance.ReloadData();
        }
    }
}