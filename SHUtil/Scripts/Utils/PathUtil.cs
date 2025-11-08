//////////////////////////////////////////////////////////////////////////
//
// PathUtil
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SHUtil
{
    public static class PathUtil
    {
        /// <summary>
        /// 입력한 경로가 올바른 형태인지 체크합니다.
        /// </summary>
        /// <param name="path">검사할 경로</param>
        /// <param name="checkExist">true이면 경로가 실제로 존재하는지도 확인합니다.</param>
        //----------------------------------------------------------------------------------
        public static bool IsValidPath(string path, bool checkExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c) || char.IsControl(c)))
                return false;

            try
            {
                string rootPath = Path.GetPathRoot(path);
                if (string.IsNullOrWhiteSpace(rootPath))
                    return false;

                string fullPath = Path.GetFullPath(path);
                if (string.IsNullOrWhiteSpace(fullPath))
                    return false;

                if (checkExist && !File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return false;
            }
            catch (Exception e)
            {
                SHLog.LogError(e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 입력한 Web Url의 경로가 올바른 경로 형태인지 체크합니다
        /// </summary>
        /// <param name="url"></param>
        //----------------------------------------------------------------------------------
        public static bool IsValidWebURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var result))
                return false;

            if (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps)
                return false;

            if (string.IsNullOrWhiteSpace(result.Host))
                return false;

            return true;
        }

        /// <summary>
        /// 입력한 Web Url의 경로에 파일이 존재하는지 비동기로 체크합니다
        /// </summary>
        /// <param name="url"></param>
        //----------------------------------------------------------------------------------
        public static async Task<bool> CheckUrlExistsAsync(string url, float timeOutSecond)
        {
            if (!IsValidWebURL(url))
                return false;

            try
            {
                var req = WebRequest.CreateHttp(url);
                req.Timeout = Convert.ToInt32(1000.0f * timeOutSecond);
                req.Method = "HEAD";

                using (var response = (HttpWebResponse)(await req.GetResponseAsync()))
                {
                    var resultCode = response.StatusCode;
                    if (resultCode >= HttpStatusCode.OK && resultCode < HttpStatusCode.BadRequest)
                        return true;

                    return false;
                }
            }
            catch (WebException webException)
            {
                if (webException.Response is HttpWebResponse errorResponse)
                {
                    SHLog.LogError($"[HTTP URL STATUS ERROR] StatusCode: {errorResponse.StatusCode}({(int)errorResponse.StatusCode})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                SHLog.LogError(ex.ToString());
            }

            return false;
        }
    }
}
