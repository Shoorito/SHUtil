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
    /// <summary>
    /// AutoSingletonManager에 자동 등록되는 싱글톤 인터페이스입니다.
    /// </summary>
    public interface ISingleton
    {
        void DisposeSingleton();
    }

    /// <summary>
    /// 프로세스 종료 시 자동으로 DisposeSingleton이 호출되는 스레드 안전 싱글톤입니다.
    /// </summary>
    public class AutoSingleton<T> : ISingleton where T : class, new()
    {
        private static volatile T sInstance;
        private static readonly object sLock = new object();

        //----------------------------------------------------------------------------------
        public static T Instance
        {
            get
            {
                if (sInstance != null)
                    return sInstance;

                lock (sLock)
                {
                    if (sInstance == null)
                        sInstance = new T();
                }

                return sInstance;
            }
        }

        //----------------------------------------------------------------------------------
        internal static void ClearInstance()
        {
            lock (sLock)
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
        public virtual void DisposeSingleton()
        {
            ClearInstance();
        }
    }

    //----------------------------------------------------------------------------------
    /// <summary>
    /// 등록된 모든 AutoSingleton 인스턴스를 관리하고 프로세스 종료 시 정리합니다.
    /// </summary>
    public class AutoSingletonManager : Singleton<AutoSingletonManager>
    {
        public List<ISingleton> CachedSingletonList { get; private set; } = new List<ISingleton>();

        //----------------------------------------------------------------------------------
        public AutoSingletonManager()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        //----------------------------------------------------------------------------------
        private void OnProcessExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            Dispose();
        }

        //----------------------------------------------------------------------------------
        public void Dispose()
        {
            CachedSingletonList.ForEach(s => s.DisposeSingleton());
            CachedSingletonList.Clear();
        }
    }
}
