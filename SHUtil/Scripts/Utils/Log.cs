//////////////////////////////////////////////////////////////////////////
//
// Log
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
    public static class SHLog
    {
        public static LogDelegate s_Log = null;
        public static LogDelegate s_LogError = null;
        public static LogDelegate s_LogWarning = null;

        public const string LOG_PATH = "_SHLog";

        public delegate void LogDelegate(string strLog);

        //----------------------------------------------------------------------------------
        public static void Log(string message)
        {
            if (s_Log == null)
                return;

            s_Log(message);
        }

        //----------------------------------------------------------------------------------
        public static void LogWarning(string message)
        {
            if (s_LogWarning == null)
                return;

            s_LogWarning(message);
        }

        //----------------------------------------------------------------------------------
        public static void LogError(string message)
        {
            if (s_LogError == null)
                return;

            s_LogError(message);
        }
    }
}
