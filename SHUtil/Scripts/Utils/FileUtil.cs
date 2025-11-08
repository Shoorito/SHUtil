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
    public static class FileUtil
    {
        private const int SALT_SIZE = 16;   // 128-bit
        private const int IV_SIZE = 16;     // AES Block Size (128-bit)
        private const int KEY_SIZE = 32;    // 256-bit
        private const int HMAC_SIZE = 32;
        private const int PBKDF2_ITER = 100000;

        //----------------------------------------------------------------------------------
        public static bool Encrypt(string srcFilePath, string dstFilePath, string password)
        {
            if (PathUtil.IsValidPath(srcFilePath) == false ||
                PathUtil.IsValidPath(dstFilePath) == false ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrWhiteSpace(password))
                return false;

            var iv = new byte[IV_SIZE];
            var salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            byte[] enc_key = null;
            byte[] mac_key = null;
            DeriveKeys(password, salt, out enc_key, out mac_key);

            var plain = File.ReadAllBytes(srcFilePath);
            var cipher = EncryptAesCbc(plain, enc_key, iv);
            var hmacInput = Concat(salt, iv, cipher);
            var hmac = ComputeHmacSha256(mac_key, hmacInput);

            string dstDirPath = Path.GetDirectoryName(dstFilePath);
            if (PathUtil.IsValidPath(dstDirPath) == false)
                Directory.CreateDirectory(dstDirPath);

            using (var fs = new FileStream(dstFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(salt, 0, salt.Length);
                fs.Write(iv, 0, iv.Length);
                fs.Write(cipher, 0, cipher.Length);
                fs.Write(hmac, 0, hmac.Length);
            }

            return true;
        }

        //----------------------------------------------------------------------------------
        public static byte[] DecryptWithBytes(byte[] allBytes, string password)
        {
            if (allBytes.Length < (SALT_SIZE + IV_SIZE + HMAC_SIZE))
                return null;

            int off = 0;
            var salt = new byte[SALT_SIZE];
            Buffer.BlockCopy(allBytes, off, salt, 0, SALT_SIZE);
            off += SALT_SIZE;

            var iv = new byte[IV_SIZE];
            Buffer.BlockCopy(allBytes, off, iv, 0, IV_SIZE);
            off += IV_SIZE;

            int cipherLen = allBytes.Length - off - HMAC_SIZE;
            if (cipherLen <= 0)
                return null;

            var cipher = new byte[cipherLen];
            Buffer.BlockCopy(allBytes, off, cipher, 0, cipherLen);
            off += cipherLen;

            var hmac = new byte[HMAC_SIZE];
            Buffer.BlockCopy(allBytes, off, hmac, 0, HMAC_SIZE);

            byte[] enc_key = null;
            byte[] mac_key = null;
            DeriveKeys(password, salt, out enc_key, out mac_key);

            var hmac_input = Concat(salt, iv, cipher);
            var computed = ComputeHmacSha256(mac_key, hmac_input);
            if (ConstantTimeNotEqual(hmac, computed))
                return null;

            var plain = DecryptAesCbc(cipher, enc_key, iv);
            return plain;
        }

        //----------------------------------------------------------------------------------
        public static byte[] Decrypt(string filePath, string password)
        {
            if (PathUtil.IsValidPath(filePath) == false ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrWhiteSpace(password))
                return null;

            var allBytes = File.ReadAllBytes(filePath);
            return DecryptWithBytes(allBytes, password);
        }

        //----------------------------------------------------------------------------------
        private static void DeriveKeys(string password, byte[] salt, out byte[] encKey, out byte[] macKey)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITER))
            {
                encKey = pbkdf2.GetBytes(KEY_SIZE);
                macKey = pbkdf2.GetBytes(KEY_SIZE);
            }
        }

        //----------------------------------------------------------------------------------
        private static byte[] EncryptAesCbc(byte[] plain, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var enc = aes.CreateEncryptor())
                    {
                        using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                        {
                            cs.Write(plain, 0, plain.Length);
                            cs.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------
        private static byte[] DecryptAesCbc(byte[] cipher, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var dec = aes.CreateDecryptor())
                    {
                        using (var cs = new CryptoStream(ms, dec, CryptoStreamMode.Write))
                        {
                            cs.Write(cipher, 0, cipher.Length);
                            cs.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------
        private static byte[] ComputeHmacSha256(byte[] key, byte[] data)
        {
            using (var h = new HMACSHA256(key))
            {
                return h.ComputeHash(data);
            }
        }

        //----------------------------------------------------------------------------------
        private static byte[] Concat(byte[] a, byte[] b, byte[] c)
        {
            var r = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, r, a.Length + b.Length, c.Length);

            return r;
        }

        //----------------------------------------------------------------------------------
        private static bool ConstantTimeNotEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return true;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff != 0;
        }

        //----------------------------------------------------------------------------------
        public static string TryDecryptToString(string filePath, string password)
        {
            // BOM 검사
            var plainBytes = Decrypt(filePath, password);
            string fromBOM = TryDecodeWithBom(plainBytes);
            if (fromBOM != null)
                return fromBOM;

            // UTF-8 시도(유효하지 않으면 DecoderFallbackException이 발생하게 설정)
            try
            {
                var utf8Str = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(plainBytes);
                return utf8Str;
            }
            catch
            {
                // 실패시 다음 시도
            }

            // UTF-16LE 시도
            try
            {
                var utf16le = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true).GetString(plainBytes);
                return utf16le;
            }
            catch
            {
                // 실패시 다음 시도
            }

            // UTF-16BE 시도
            try
            {
                var utf16be = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true).GetString(plainBytes);
                return utf16be;
            }
            catch
            {
                // 실패시 다음 시도
            }

            // UTF-32 시도
            try
            {
                var utf32 = new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true).GetString(plainBytes);
                return utf32;
            }
            catch
            {
                // 실패시 다음 시도
            }

            return null;
        }

        // BOM 기반 디코딩 시도: 성공하면 문자열, 실패하면 null
        //----------------------------------------------------------------------------------
        private static string TryDecodeWithBom(byte[] bytes)
        {
            if (bytes == null || bytes.Length <= 0)
                return string.Empty;

            // UTF-8 BOM EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                try
                {
                    return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
                }
                catch
                {
                    return null;
                }
            }

            // UTF-16 LE BOM FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                try
                {
                    return new UnicodeEncoding(false, true, true).GetString(bytes, 2, bytes.Length - 2);
                }
                catch
                {
                    return null;
                }
            }

            // UTF-16 BE BOM FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                try
                {
                    return new UnicodeEncoding(true, true, true).GetString(bytes, 2, bytes.Length - 2);
                }
                catch
                {
                    return null;
                }
            }

            // UTF-32 LE BOM FF FE 00 00
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            {
                try
                {
                    return new UTF32Encoding(false, true, true).GetString(bytes, 4, bytes.Length - 4);
                }
                catch
                {
                    return null;
                }
            }

            // UTF-32 BE BOM 00 00 FE FF
            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            {
                try
                {
                    return new UTF32Encoding(true, true, true).GetString(bytes, 4, bytes.Length - 4);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
