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

using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SHUtil
{
    public static class StringUtil
    {
        //----------------------------------------------------------------------------------
        public static ParseType SafeParse<ParseType>(string text, ParseType default_value)
        {
            return (ParseType)SafeParse(text, typeof(ParseType), default_value);
        }

        //----------------------------------------------------------------------------------
        public static object SafeParse(string text, System.Type parse_type, object default_value)
        {
            if (string.IsNullOrEmpty(text) == true)
                return default_value;

            try
            {
                if (parse_type.IsEnum == true)
                {
                    if (text.Contains("|") == true)
                        return ParseEnumFlag(text, parse_type);

                    return System.Enum.Parse(parse_type, text, true);
                }
                else
                {
                    if (parse_type == typeof(System.Version))
                        return System.Version.Parse(text);

                    return System.Convert.ChangeType(text, parse_type);
                }
            }
            catch (System.Exception e)
            {
                // TODO: Add Log
            }

            return default_value;
        }

        //----------------------------------------------------------------------------------
        public static T ParseEnumFlag<T>(string value)
        {
            return (T)ParseEnumFlag(value, typeof(T));
        }

        //----------------------------------------------------------------------------------
        public static object ParseEnumFlag(string value, System.Type parse_type)
        {
            System.TypeCode type_code = System.Type.GetTypeCode(parse_type);
            List<object> split_obj_list = value.Split('|').Select(s => System.Enum.Parse(parse_type, s)).ToList<object>();
            int split_obj_count = split_obj_list.Count;

            int flag_value = 0;
            byte flag_value_byte = 0;
            long flag_value_long = 0L;
            for (int i = 0; i < split_obj_count; i++)
            {
                if (type_code != System.TypeCode.Byte)
                {
                    if (type_code != System.TypeCode.Int64)
                        flag_value |= (int)split_obj_list[i];
                    else
                        flag_value_long |= (long)split_obj_list[i];
                }
                else
                {
                    flag_value_byte |= (byte)split_obj_list[i];
                }
            }

            if (type_code == System.TypeCode.Byte)
                return flag_value_byte;
            else if (type_code != System.TypeCode.Int64)
                return flag_value;
            else
                return flag_value_long;
        }

        //----------------------------------------------------------------------------------
        public static List<T> SafeParseToList<T>(string parse_value, params char[] separator)
        {
            if (string.IsNullOrEmpty(parse_value) == true)
                return null;

            try
            {
                if (typeof(T).IsEnum == true)
                    return parse_value.Split(separator).Select(s => (T)System.Enum.Parse(typeof(T), s)).ToList<T>();

                return parse_value.Split(separator).Select(s => (T)System.Convert.ChangeType(s, typeof(T))).ToList<T>();

            }
            catch (System.Exception e)
            {
                // TODO: Add Log
            }

            return null;
        }

        //----------------------------------------------------------------------------------
        public static string CompressedBase64String(string xml_str, bool use_clzf)
        {
            string ret_string = "";
            if (use_clzf == true)
            {
                try
                {
                    byte[] text_bytes = Encoding.UTF8.GetBytes(xml_str);
                    byte[] compressed = CLZF.Compress(text_bytes);
                    return System.Convert.ToBase64String(compressed);

                }
                catch (System.Exception e)
                {
                    // TODO: Add Log
                    return ret_string;
                }
            }

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream();

                byte[] buffer = Encoding.UTF8.GetBytes(xml_str);
                GZipStream gzip = new GZipStream(stream, CompressionMode.Compress, true);
                if (gzip != null)
                    gzip.Write(buffer, 0, buffer.Length);

                stream.Position = 0L;

                byte[] compressed_data = new byte[stream.Length];
                stream.Read(compressed_data, 0, compressed_data.Length);
                ret_string = System.Convert.ToBase64String(compressed_data);
            }
            catch (System.Exception e)
            {
                // TODO: Add Log
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            return ret_string;
        }

        //----------------------------------------------------------------------------------
        public static List<string> StringSplit(string in_str, int len)
        {
            List<string> list = new List<string>();
            int loop_count = 1;
            int in_str_len = in_str.Length;
            if (in_str_len >= len)
                loop_count = in_str_len / len + 1;

            int offset = 0;
            int size = System.Math.Min(in_str_len, 25000);
            for (int i = 0; i < loop_count; i++)
            {
                list.Add(in_str.Substring(offset, size));
                offset += size;
                size = System.Math.Min(in_str_len - offset, len);
            }

            return list;
        }

        //----------------------------------------------------------------------------------
        public static bool ContainsSpecialOrWildcard(string text, char[] ignoreChars = null)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                return false;

            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) == false || ch == '_')
                    return true;

                if (ignoreChars != null && ignoreChars.Contains(ch))
                    return true;
            }

            return false;
        }
    }
}
