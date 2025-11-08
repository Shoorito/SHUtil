//////////////////////////////////////////////////////////////////////////
//
// CLZF
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

namespace SHUtil
{
    public static class CLZF
    {
        private static readonly long[] HashTable = new long[HSIZE];

        private const uint HLOG = 14U;
        private const uint HSIZE = 16384U;
        private const uint MAX_LIT = 32U;
        private const uint MAX_OFF = 8192U;
        private const uint MAX_REF = 264U;

        //----------------------------------------------------------------------------------
        public static byte[] Compress(byte[] input_bytes)
        {
            int output_byte_count_guess = input_bytes.Length * 2;
            byte[] tmp_buffer = new byte[output_byte_count_guess];

            int byte_count = LZFCompress(input_bytes, ref tmp_buffer);
            while (byte_count == 0)
            {
                output_byte_count_guess *= 2;
                tmp_buffer = new byte[output_byte_count_guess];
                byte_count = LZFCompress(input_bytes, ref tmp_buffer);
            }

            byte[] output_bytes = new byte[byte_count];
            Buffer.BlockCopy(tmp_buffer, 0, output_bytes, 0, byte_count);
            return output_bytes;
        }

        //----------------------------------------------------------------------------------
        public static byte[] Decompress(byte[] input_bytes)
        {
            int output_byte_count_guess = input_bytes.Length * 2;
            byte[] tmp_buffer = new byte[output_byte_count_guess];

            int byte_count = LZFDecompress(input_bytes, ref tmp_buffer);
            while (byte_count == 0)
            {
                output_byte_count_guess *= 2;
                tmp_buffer = new byte[output_byte_count_guess];
                byte_count = LZFDecompress(input_bytes, ref tmp_buffer);
            }

            byte[] output_bytes = new byte[byte_count];
            Buffer.BlockCopy(tmp_buffer, 0, output_bytes, 0, byte_count);
            return output_bytes;
        }

        //----------------------------------------------------------------------------------
        public static int LZFCompress(byte[] input, ref byte[] output)
        {
            int input_len = input.Length;
            int output_len = output.Length;

            Array.Clear(HashTable, 0, (int)HSIZE);

            uint input_idx = 0U;
            uint output_idx = 0U;
            uint h_val = (uint)((int)input[(int)input_idx] << 8 | (int)input[(int)(input_idx + 1U)]);

            int lit = 0;
            bool now_compress = true;
            while (now_compress == true)
            {
                if ((ulong)input_idx < (ulong)((long)(input_len - 2)))
                {
                    h_val = (h_val << 8 | (uint)input[(int)(input_idx + 2U)]);

                    long hslot = (long)((ulong)(h_val ^ h_val << 5) >> (int)(24U - HLOG - h_val * 5U) & HSIZE - 1U);
                    long refer = HashTable[(int)(checked((IntPtr)hslot))];
                    HashTable[(int)(checked((IntPtr)hslot))] = (long)((ulong)input_idx);

                    long offset = (long)((ulong)input_idx - (ulong)refer - 1UL);
                    if ((offset < (long)((ulong)MAX_OFF)) &&
                        ((ulong)(input_idx + 4U) < (ulong)((long)input_len)) &&
                        refer > 0L &&
                        input[(int)(checked((IntPtr)refer))] == input[(int)input_idx] &&
                        input[(int)(checked((IntPtr)(unchecked(refer + 1L))))] == input[(int)(input_idx + 1U)] &&
                        input[(int)(checked((IntPtr)(unchecked(refer + 2L))))] == input[(int)(input_idx + 2U)])
                    {
                        uint len = 2U;
                        uint max_len = (uint)input_len - input_idx - len;

                        max_len = max_len > MAX_REF ? MAX_REF : max_len;

                        ulong checker = (ulong)output_idx + (ulong)((long)lit) + 4UL;
                        if (checker >= (ulong)((long)output_len))
                        {
                            now_compress = false;
                            continue;
                        }

                        len += 1U;
                        while (len < max_len && input[(int)(checked((IntPtr)(unchecked(refer + (long)((ulong)len)))))] == input[(int)(input_idx + len)])
                        {
                            len += 1U;
                        }

                        if (lit != 0)
                        {
                            output[(int)output_idx] = (byte)(lit - 1);
                            output_idx += 1U;
                            lit = -lit;

                            output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                            output_idx += 1U;
                            lit += 1;
                            while (lit != 0)
                            {
                                output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                                output_idx += 1U;
                                lit += 1;
                            }
                        }

                        len -= 2U;
                        input_idx += 1U;
                        if (len < 7U)
                        {
                            output[(int)output_idx] = (byte)((offset >> 8) + (long)((ulong)((ulong)len << 5)));
                            output_idx += 1;
                        }
                        else
                        {
                            output[(int)output_idx] = (byte)((offset >> 8) + 224L);
                            output_idx += 1U;
                            output[(int)output_idx] = (byte)(len - 7U);
                            output_idx += 1U;
                        }

                        output[(int)output_idx] = (byte)offset;
                        output_idx += 1U;
                        input_idx += len - 1U;

                        h_val = (uint)((int)input[(int)input_idx] << 8 | (int)input[(int)(input_idx + 1U)]);
                        h_val = (h_val << 8 | (uint)input[(int)(input_idx + 2U)]);
                        HashTable[(int)((h_val ^ h_val << 5) >> (int)(24U - HLOG - h_val * 5U) & HSIZE - 1U)] = (long)((ulong)input_idx);

                        input_idx += 1U;
                        h_val = (h_val << 8 | (uint)input[(int)(input_idx + 2U)]);
                        HashTable[(int)((h_val ^ h_val << 5) >> (int)(24U - HLOG - h_val * 5U) & HSIZE - 1U)] = (long)((ulong)input_idx);

                        input_idx += 1U;
                        continue;
                    }
                }
                else if ((ulong)input_idx == (ulong)((long)input_len))
                {
                    if (lit != 0)
                    {
                        if ((ulong)output_idx + (ulong)((long)lit) + 1UL >= (ulong)((long)output_len))
                            return 0;

                        output[(int)output_idx] = (byte)(lit - 1);
                        output_idx += 1U;
                        lit = -lit;

                        output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                        output_idx += 1U;
                        lit += 1;
                        while (lit != 0)
                        {
                            output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                            output_idx += 1U;
                            lit += 1;
                        }
                    }

                    return (int)output_idx;
                }

                lit += 1;
                input_idx += 1U;
                if ((long)lit == (long)((ulong)MAX_LIT))
                {
                    if ((ulong)(output_idx + 1U + MAX_LIT) >= (ulong)((long)output_len))
                        return 0;

                    output[(int)output_idx] = (byte)(MAX_LIT - 1U);
                    output_idx += 1U;
                    lit = -lit;

                    output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                    output_idx += 1U;
                    lit += 1;
                    while (lit != 0)
                    {
                        output[(int)output_idx] = input[(int)(checked((IntPtr)(unchecked((ulong)input_idx + (ulong)((long)lit)))))];
                        output_idx += 1U;
                        lit += 1;
                    }
                }
            }

            return 0;
        }

        //----------------------------------------------------------------------------------
        public static int LZFDecompress(byte[] input, ref byte[] output)
        {
            int input_len = input.Length;
            int output_len = output.Length;
            uint input_idx = 0U;
            uint output_idx = 0U;

            bool now_decompress = true;
            while (now_decompress == true)
            {
                uint ctrl = (uint)input[(int)input_idx];
                input_idx += 1U;

                if (ctrl < 32U)
                {
                    ctrl += 1U;
                    if ((ulong)(output_idx + ctrl) > (ulong)((long)output_len))
                    {
                        now_decompress = false;
                        continue;
                    }

                    output[(int)output_idx] = input[(int)input_idx];
                    output_idx += 1U;
                    input_idx += 1U;
                    ctrl -= 1U;
                    while (ctrl != 0)
                    {
                        output[(int)output_idx] = input[(int)input_idx];
                        output_idx += 1U;
                        input_idx += 1U;
                        ctrl -= 1U;
                    }
                }
                else
                {
                    uint len = ctrl >> 5;
                    int refer = (int)(output_idx - ((ctrl & 31U) << 8) - 1U);
                    if (len == 7U)
                    {
                        len += (uint)input[(int)input_idx];
                        input_idx += 1U;
                    }

                    refer -= (int)input[(int)input_idx];
                    input_idx += 1U;

                    if ((ulong)(output_idx + len + 2U) > (ulong)((long)output_len))
                        return 0;

                    if (refer < 0)
                        return 0;

                    for (int i = 0; i < 3; i++)
                    {
                        output[(int)output_idx] = output[refer];
                        output_idx += 1U;
                        refer += 1;
                    }

                    len -= 1;
                    while (len != 0U)
                    {
                        output[(int)output_idx] = output[refer];
                        output_idx += 1U;
                        refer += 1;
                        len -= 1;
                    }
                }

                if ((ulong)input_idx >= (ulong)((long)input_len))
                    return (int)output_idx;
            }

            return 0;
        }
    }
}
