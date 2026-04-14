//////////////////////////////////////////////////////////////////////////
//
// FileUtil
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
using System.Security.Cryptography;
using System.Text;

namespace SHUtil
{
    /// <summary>
    /// 파일 암호화/복호화 및 텍스트 디코딩 기능을 제공합니다.
    /// 암호화 포맷: [SALT:16][NONCE:12][TAG:16][CIPHERTEXT:N]
    /// </summary>
    public static class FileUtil
    {
        private const int SALT_SIZE = 16;
        private const int NONCE_SIZE = 12;   // AES-GCM 표준 nonce 크기
        private const int TAG_SIZE = 16;   // AES-GCM 인증 태그 크기
        private const int KEY_SIZE = 32;   // AES-256
        private const int PBKDF2_ITERATIONS = 600_000;

        private static readonly HashAlgorithmName PBKDF2_HASH = HashAlgorithmName.SHA256;

        /// <summary>
        /// 파일을 AES-256-GCM으로 암호화합니다.
        /// </summary>
        /// <param name="srcFilePath">원본 파일 경로</param>
        /// <param name="dstFilePath">암호화된 파일 저장 경로</param>
        /// <param name="password">암호화에 사용할 비밀번호</param>
        /// <returns>성공 시 true</returns>
        //----------------------------------------------------------------------------------
        public static bool Encrypt(string srcFilePath, string dstFilePath, string password)
        {
            if (!PathUtil.IsValidPath(srcFilePath) ||
                !PathUtil.IsValidPath(dstFilePath) ||
                string.IsNullOrWhiteSpace(password))
                return false;

            var salt = new byte[SALT_SIZE];
            var nonce = new byte[NONCE_SIZE];
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(nonce);

            var key = DeriveKey(password, salt);
            var plain = File.ReadAllBytes(srcFilePath);
            var cipher = new byte[plain.Length];
            var tag = new byte[TAG_SIZE];

#if NET8_0_OR_GREATER
            using (var aesGcm = new AesGcm(key, TAG_SIZE))
            {
                aesGcm.Encrypt(nonce, plain, cipher, tag);
            }
#else
            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, plain, cipher, tag);
            }
#endif
            var dstDir = Path.GetDirectoryName(dstFilePath);
            if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            using (var fs = new FileStream(dstFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(salt, 0, salt.Length);
                fs.Write(nonce, 0, nonce.Length);
                fs.Write(tag, 0, tag.Length);
                fs.Write(cipher, 0, cipher.Length);
            }

            return true;
        }

        /// <summary>
        /// 바이트 배열을 복호화합니다. 인증 실패 또는 잘못된 입력 시 null을 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static byte[] DecryptWithBytes(byte[] allBytes, string password)
        {
            if (allBytes == null || string.IsNullOrWhiteSpace(password))
                return null;

            int headerSize = SALT_SIZE + NONCE_SIZE + TAG_SIZE;
            if (allBytes.Length <= headerSize)
                return null;

            var salt = allBytes[..SALT_SIZE];
            var nonce = allBytes[SALT_SIZE..(SALT_SIZE + NONCE_SIZE)];
            var tag = allBytes[(SALT_SIZE + NONCE_SIZE)..(SALT_SIZE + NONCE_SIZE + TAG_SIZE)];
            var cipher = allBytes[(SALT_SIZE + NONCE_SIZE + TAG_SIZE)..];

            var key = DeriveKey(password, salt);
            var plain = new byte[cipher.Length];

            try
            {
#if NET8_0_OR_GREATER
                using (var aesGcm = new AesGcm(key, TAG_SIZE))
                {
                    aesGcm.Decrypt(nonce, cipher, tag, plain);
                }
#else
                using (var aesGcm = new AesGcm(key))
                {
                    aesGcm.Decrypt(nonce, cipher, tag, plain);
                }
#endif

                return plain;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        /// 파일을 복호화하여 바이트 배열로 반환합니다. 실패 시 null을 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static byte[] Decrypt(string filePath, string password)
        {
            if (!PathUtil.IsValidPath(filePath) || string.IsNullOrWhiteSpace(password))
                return null;

            return DecryptWithBytes(File.ReadAllBytes(filePath), password);
        }

        /// <summary>
        /// 암호화된 파일을 복호화 후 문자열로 변환합니다. BOM 기반 인코딩을 자동 감지합니다.
        /// 실패 시 null을 반환합니다.
        /// </summary>
        //----------------------------------------------------------------------------------
        public static string TryDecryptToString(string filePath, string password)
        {
            var plainBytes = Decrypt(filePath, password);
            if (plainBytes == null)
                return null;

            var fromBOM = TryDecodeWithBom(plainBytes);
            if (fromBOM != null)
                return fromBOM;

            Encoding[] encodings =
            {
                new UTF8Encoding(false, true),
                new UnicodeEncoding(false, false, true),
                new UnicodeEncoding(true,  false, true),
                new UTF32Encoding(false, false, true),
            };

            foreach (var enc in encodings)
            {
                try { return enc.GetString(plainBytes); }
                catch { /* 다음 인코딩 시도 */ }
            }

            return null;
        }

        //----------------------------------------------------------------------------------
        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, PBKDF2_HASH))
                return pbkdf2.GetBytes(KEY_SIZE);
        }

        //----------------------------------------------------------------------------------
        private static string TryDecodeWithBom(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            // UTF-8 BOM: EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                try { return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3); }
                catch { return null; }
            }

            // UTF-32 LE BOM: FF FE 00 00 (UTF-16 LE보다 먼저 체크해야 함)
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            {
                try { return new UTF32Encoding(false, true, true).GetString(bytes, 4, bytes.Length - 4); }
                catch { return null; }
            }

            // UTF-16 LE BOM: FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                try { return new UnicodeEncoding(false, true, true).GetString(bytes, 2, bytes.Length - 2); }
                catch { return null; }
            }

            // UTF-16 BE BOM: FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                try { return new UnicodeEncoding(true, true, true).GetString(bytes, 2, bytes.Length - 2); }
                catch { return null; }
            }

            // UTF-32 BE BOM: 00 00 FE FF
            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            {
                try { return new UTF32Encoding(true, true, true).GetString(bytes, 4, bytes.Length - 4); }
                catch { return null; }
            }

            return null;
        }
    }
}
