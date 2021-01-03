using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace MS.Extensions.Caching.ServiceFabric
{
    public static class PartitionKeyExtensions
    {
        private const uint Seed = 123456U;

        /// <summary>
        /// Converts text to an unsigned integer hash value usable as partition key.
        /// </summary>
        /// <remarks>
        /// MurmmurHash3 based hashing algorithm.
        /// </remarks>
        public static uint ToPartitionKey(this string text)
        {
            return Hash32(Encoding.UTF8.GetBytes(text), Seed);
        }

        /// <summary>
        /// Converts bytes to an unsigned integer hash value usable as partition key.
        /// </summary>
        /// <remarks>
        /// MurmmurHash3 based hashing algorithm.
        /// </remarks>
        public static uint ToPartitionKey(this ReadOnlySpan<byte> bytes)
        {
            return Hash32(bytes, Seed);
        }

        private static uint Hash32(ReadOnlySpan<byte> bytes, uint seed)
        {
            var length = bytes.Length;
            var h1 = seed;
            var remainder = length & 3;
            var position = length - remainder;
            for (var start = 0; start < position; start += 4)
                h1 = (uint)((int)RotateLeft(h1 ^ RotateLeft(BitConverter.ToUInt32(bytes.Slice(start, 4)) * 3432918353U, 15) * 461845907U, 13) * 5 - 430675100);

            if (remainder > 0)
            {
                uint num = 0;
                switch (remainder)
                {
                    case 1:
                        num ^= (uint)bytes[position];
                        break;
                    case 2:
                        num ^= (uint)bytes[position + 1] << 8;
                        goto case 1;
                    case 3:
                        num ^= (uint)bytes[position + 2] << 16;
                        goto case 2;
                }

                h1 ^= RotateLeft(num * 3432918353U, 15) * 461845907U;
            }

            h1 = FMix(h1 ^ (uint)length);

            return h1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte r)
        {
            return x << (int)r | x >> 32 - (int)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FMix(uint h)
        {
            h = (uint)(((int)h ^ (int)(h >> 16)) * -2048144789);
            h = (uint)(((int)h ^ (int)(h >> 13)) * -1028477387);
            return h ^ h >> 16;
        }
    }
}