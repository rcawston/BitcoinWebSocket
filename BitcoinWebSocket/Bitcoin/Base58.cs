using System;
using BitcoinWebSocket.Util;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace BitcoinWebSocket.Bitcoin
{
    public class Base58
    {
        public const int CheckSumBytes = 4;
        private const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string Encode(byte[] data)
        {
            // Decode byte[] to BigInteger
            var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

            // Encode BigInteger to Base58 string
            var result = "";
            while (intData > 0)
            {
                var remainder = (int)(intData % 58);
                intData /= 58;
                result = Digits[remainder] + result;
            }

            // Append `1` for each leading 0 byte
            for (var i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }
            return result;
        }

        public static string EncodeWithCheckSum(byte[] data)
        {
            return Encode(AddCheckSum(data));
        }

        private static byte[] AddCheckSum(byte[] data)
        {
            SHA256 sha256 = new SHA256Managed();
            var hash1 = sha256.ComputeHash(data);
            var hash2 = sha256.ComputeHash(hash1);

            var checkSum = new byte[CheckSumBytes];
            Buffer.BlockCopy(hash2, 0, checkSum, 0, checkSum.Length);
            var dataWithCheckSum = ArrayTools.ConcatArrays(data, checkSum);
            return dataWithCheckSum;
        }
    }
}