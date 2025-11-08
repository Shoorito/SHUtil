//////////////////////////////////////////////////////////////////////////
//
// AutoSingleton
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

namespace SHUtil
{
    public interface ISingleton
    {
        void DIsposeSingleton();
    }

    public class AutoSingleton<T> : ISingleton where T : class, new()
    {
        private static T sInstance = null;

        //----------------------------------------------------------------------------------
        public static T Instance
        {
            get
            {
                if (sInstance == null)
                {
                    sInstance = System.Activator.CreateInstance<T>();
                }

                return sInstance;
            }
        }

        //----------------------------------------------------------------------------------
        public static void ClearInstance()
        {
            if (sInstance != null)
            {
                sInstance = null;
            }
        }

        //----------------------------------------------------------------------------------
        public AutoSingleton()
        {
            AutoSingletonManager.Instance.CachedSingletonList.Add(this);
        }

        //----------------------------------------------------------------------------------
        public virtual void DIsposeSingleton()
        {
            AutoSingleton<T>.ClearInstance();
        }
    }

    //----------------------------------------------------------------------------------
    public class AutoSingletonManager : Singleton<AutoSingletonManager>
    {
        public List<ISingleton> CachedSingletonList { get; private set; } = new List<ISingleton>();

        //----------------------------------------------------------------------------------
        public AutoSingletonManager()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            Dispose();
        }

        //----------------------------------------------------------------------------------
        public void Dispose()
        {
            CachedSingletonList.ForEach(a => a.DIsposeSingleton());
        }
    }
}
