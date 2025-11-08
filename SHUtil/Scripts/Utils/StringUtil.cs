//////////////////////////////////////////////////////////////////////////
//
// StringUtil
// 
// Created by Shoori.
//
// Copyright 2025 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SHUtil
{
    /// <summary>
    /// 문자열 파싱, 압축, 분할 등 문자열 관련 편의 기능을 제공합니다.
    /// </summary>
    public static class StringUtil
    {
        /// <summary>
        /// 문자열을 지정한 타입으로 안전하게 변환합니다. 실패 시 기본값을 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static ParseType SafeParse<ParseType>(string text, ParseType defaultValue)
        {
            return (ParseType)SafeParse(text, typeof(ParseType), defaultValue);
        }

        //----------------------------------------------------------------------------------
        public static object SafeParse(string text, Type parseType, object defaultValue)
        {
            if (string.IsNullOrEmpty(text))
                return defaultValue;

            try
            {
                if (parseType.IsEnum)
                {
                    if (text.Contains("|"))
                        return ParseEnumFlag(text, parseType);

                    return Enum.Parse(parseType, text, ignoreCase: true);
                }

                if (parseType == typeof(Version))
                    return Version.Parse(text);

                return Convert.ChangeType(text, parseType);
            }
            catch
            {
                return defaultValue;
            }
        }

        //----------------------------------------------------------------------------------
        public static T ParseEnumFlag<T>(string value)
        {
            return (T)ParseEnumFlag(value, typeof(T));
        }

        //----------------------------------------------------------------------------------
        public static object ParseEnumFlag(string value, Type parseType)
        {
            var typeCode = Type.GetTypeCode(parseType);
            var parts    = value.Split('|').Select(s => Enum.Parse(parseType, s.Trim(), true)).ToList();

            int  flagInt  = 0;
            long flagLong = 0L;
            byte flagByte = 0;

            foreach (var part in parts)
            {
                switch (typeCode)
                {
                    case TypeCode.Byte:  flagByte |= (byte)part; break;
                    case TypeCode.Int64: flagLong |= (long)part; break;
                    default:             flagInt  |= (int)part;  break;
                }
            }

            switch (typeCode)
            {
                case TypeCode.Byte:  return (object)flagByte;
                case TypeCode.Int64: return (object)flagLong;
                default:             return (object)flagInt;
            }
        }

        /// <summary>
        /// 구분자로 분리된 문자열을 List로 변환합니다. 실패 시 null을 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static List<T> SafeParseToList<T>(string parseValue, params char[] separator)
        {
            if (string.IsNullOrEmpty(parseValue))
                return null;

            try
            {
                if (typeof(T).IsEnum)
                    return parseValue.Split(separator).Select(s => (T)Enum.Parse(typeof(T), s.Trim(), true)).ToList();

                return parseValue.Split(separator).Select(s => (T)Convert.ChangeType(s.Trim(), typeof(T))).ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 문자열을 LZF 또는 GZip으로 압축 후 Base64 문자열로 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static string CompressToBase64(string text, bool useClzf)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                var inputBytes = Encoding.UTF8.GetBytes(text);
                byte[] compressed;

                if (useClzf)
                {
                    compressed = CLZF.Compress(inputBytes);
                }
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                            gzip.Write(inputBytes, 0, inputBytes.Length);

                        compressed = ms.ToArray();
                    }
                }

                return Convert.ToBase64String(compressed);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>문자열을 지정한 길이 단위로 분할합니다.</summary>
        //----------------------------------------------------------------------------------
        public static List<string> StringSplit(string str, int chunkSize)
        {
            var list = new List<string>();

            if (string.IsNullOrEmpty(str) || chunkSize <= 0)
                return list;

            for (int offset = 0; offset < str.Length; offset += chunkSize)
            {
                int size = Math.Min(chunkSize, str.Length - offset);
                list.Add(str.Substring(offset, size));
            }

            return list;
        }

        /// <summary>
        /// 문자열에 영문자·숫자·언더스코어 외의 문자가 포함되어 있으면 true를 반환합니다.
        /// ignoreChars에 포함된 문자는 특수 문자로 처리하지 않습니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static bool ContainsSpecialOrWildcard(string text, char[] ignoreChars = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    continue;

                if (ignoreChars != null && Array.IndexOf(ignoreChars, ch) >= 0)
                    continue;

                return true;
            }

            return false;
        }
    }
}
