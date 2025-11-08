using System;

namespace SHUtil
{
    /// <summary>
    /// LZF 알고리즘 기반 데이터 압축/해제 유틸리티입니다.
    /// </summary>
    public static class CLZF
    {
        private const int HLOG    = 16;
        private const int HSIZE   = 1 << HLOG;  // 65536
        private const int MAX_LIT = 1 << 5;     // 32
        private const int MAX_OFF = 1 << 13;    // 8192
        private const int MAX_REF = (1 << 8) + (1 << 3); // 264

        [ThreadStatic]
        private static long[] sHashTable;

        private static long[] HashTable
        {
            get
            {
                if (sHashTable == null)
                    sHashTable = new long[HSIZE];
                return sHashTable;
            }
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// 바이트 배열을 LZF 알고리즘으로 압축합니다.
        /// </summary>
        public static byte[] Compress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length == 0)
                return Array.Empty<byte>();

            byte[] output = new byte[Math.Max(input.Length * 2, 64)];
            int    length = LZFCompress(input, ref output);

            byte[] result = new byte[length];
            Buffer.BlockCopy(output, 0, result, 0, length);
            return result;
        }

        //----------------------------------------------------------------------------------
        /// <summary>
        /// LZF 압축된 바이트 배열을 원본으로 해제합니다.
        /// </summary>
        public static byte[] Decompress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length == 0)
                return Array.Empty<byte>();

            byte[] output = new byte[Math.Max(input.Length * 2, 64)];
            int    length = LZFDecompress(input, ref output);

            byte[] result = new byte[length];
            Buffer.BlockCopy(output, 0, result, 0, length);
            return result;
        }

        //----------------------------------------------------------------------------------
        private static int LZFCompress(byte[] input, ref byte[] output)
        {
            long[] hashTable = HashTable;
            Array.Clear(hashTable, 0, hashTable.Length);

            int iLen = input.Length;
            int oLen = output.Length;
            int ip   = 0;
            int op   = 0;
            int lit  = 0;

            for (;;)
            {
                if (ip < iLen - 2)
                {
                    int hval  = (input[ip] << 8) | input[ip + 1];
                    int hslot = (((hval ^ (hval << 5)) >> (3 * 8 - HLOG)) - input[ip + 2] * 5) & (HSIZE - 1);

                    int reference = (int)hashTable[hslot];
                    hashTable[hslot] = ip;

                    int off = ip - reference - 1;

                    if (off >= 0
                        && off < MAX_OFF
                        && ip + 4 < iLen
                        && reference + 4 < iLen
                        && input[reference]     == input[ip]
                        && input[reference + 1] == input[ip + 1]
                        && input[reference + 2] == input[ip + 2])
                    {
                        int maxLen = Math.Min(Math.Min(iLen - ip, iLen - reference), MAX_REF);
                        int len    = 3;

                        while (len < maxLen && input[reference + len] == input[ip + len])
                            len++;

                        // 대기 중인 리터럴 먼저 출력
                        if (lit != 0)
                        {
                            if (op + lit + 1 >= oLen)
                                oLen = GrowOutput(ref output, op + lit + 2);

                            output[op++] = (byte)(lit - 1);
                            Buffer.BlockCopy(input, ip - lit, output, op, lit);
                            op  += lit;
                            lit  = 0;
                        }

                        // 역참조 출력
                        len -= 2;

                        if (op + 3 >= oLen)
                            oLen = GrowOutput(ref output, op + 4);

                        if (len < 7)
                        {
                            output[op++] = (byte)((len << 5) | (off >> 8));
                        }
                        else
                        {
                            output[op++] = (byte)((7 << 5) | (off >> 8));
                            output[op++] = (byte)(len - 7);
                        }
                        output[op++] = (byte)off;

                        ip += len + 2;
                        continue;
                    }
                }
                else if (ip == iLen)
                {
                    break;
                }

                // 리터럴 누적
                lit++;
                ip++;

                if (lit == MAX_LIT)
                {
                    if (op + MAX_LIT + 1 >= oLen)
                        oLen = GrowOutput(ref output, op + MAX_LIT + 2);

                    output[op++] = (byte)(MAX_LIT - 1);
                    Buffer.BlockCopy(input, ip - lit, output, op, lit);
                    op  += lit;
                    lit  = 0;
                }
            }

            // 남은 리터럴 출력
            if (lit != 0)
            {
                if (op + lit + 1 >= oLen)
                    oLen = GrowOutput(ref output, op + lit + 2);

                output[op++] = (byte)(lit - 1);
                Buffer.BlockCopy(input, ip - lit, output, op, lit);
                op += lit;
            }

            return op;
        }

        //----------------------------------------------------------------------------------
        private static int LZFDecompress(byte[] input, ref byte[] output)
        {
            int iLen = input.Length;
            int oLen = output.Length;
            int ip   = 0;
            int op   = 0;

            while (ip < iLen)
            {
                int ctrl = input[ip++];

                if (ctrl < MAX_LIT)
                {
                    // 리터럴 블록
                    ctrl++;
                    if (op + ctrl >= oLen)
                        oLen = GrowOutput(ref output, op + ctrl + 1);

                    Buffer.BlockCopy(input, ip, output, op, ctrl);
                    ip += ctrl;
                    op += ctrl;
                }
                else
                {
                    // 역참조 블록
                    int len    = ctrl >> 5;
                    int refPos = op - ((ctrl & 0x1f) << 8) - 1;

                    if (len == 7)
                        len += input[ip++];

                    len    += 2;
                    refPos -= input[ip++];

                    if (refPos < 0)
                        throw new InvalidOperationException("CLZF 압축 해제 실패: 잘못된 참조 위치입니다.");

                    if (op + len >= oLen)
                        oLen = GrowOutput(ref output, op + len + 1);

                    do { output[op++] = output[refPos++]; } while (--len != 0);
                }
            }

            return op;
        }

        //----------------------------------------------------------------------------------
        private static int GrowOutput(ref byte[] output, int minSize)
        {
            int newSize = Math.Max(output.Length * 2, minSize + 64);
            Array.Resize(ref output, newSize);
            return newSize;
        }
    }
}
