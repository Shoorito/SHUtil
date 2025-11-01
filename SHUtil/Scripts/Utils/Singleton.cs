//////////////////////////////////////////////////////////////////////////
//
// Singleton
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

namespace SHUtil
{
    public class Singleton<T> where T : class, new()
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
    }
}
