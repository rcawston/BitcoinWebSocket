using System;
using BitcoinWebSocket.Util;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Base58 (Bitcoin Address Format) Encoder
    ///     - converts byte arrays from bitcoin scripts into base58 strings
    /// </summary>
    public class Base58
    {
        // bitcoin addresses use 4 checksum bytes
        public const int CheckSumBytes = 4; 
        // Base58 charset/digits
        private const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        ///     Converts byte array to base58 string
        /// </summary>
        /// <param name="data">byte array from bitcoin script representing an address</param>
        /// <returns>base58 string</returns>
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
                result = '1' + result;

            return result;
        }

        /// <summary>
        ///     Converts byte array to base58 string w/ checksum
        /// </summary>
        /// <param name="data">byte array from bitcoin script representing an address</param>
        /// <returns>base58 string with checksum</returns>
        public static string EncodeWithCheckSum(byte[] data)
        {
            // add checksum to the byte array and encode as base58
            return Encode(AddCheckSum(data));
        }

        /// <summary>
        ///     Appends truncated double sha256 checksum to a byte array
        /// </summary>
        /// <param name="data">byte array from bitcoin script representing an address</param>
        /// <returns>byte array with appended checksum</returns>
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