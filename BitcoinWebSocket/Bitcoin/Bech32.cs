using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Bech32 (Bitcoin Address Format) Encoder
    ///     - converts byte arrays from bitcoin scripts into bech32 strings
    ///     - As described in: https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki
    /// </summary>
    public class Bech32
    {
        // Bech32 charset/digits
        private const string Digits = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        // Used by polymod checksum generation
        private static readonly uint[] Generator = { 0x3b6a57b2U, 0x26508e6dU, 0x1ea119faU, 0x3d4233ddU, 0x2a1462b3U };

        /// <summary>
        ///     Encodes a bech32 address
        /// </summary>
        /// <param name="witnessVersion">the witness version (only v0 exists so far)</param>
        /// <param name="witnessScriptData">byte array representing address from bitcoin OP_0 (segwit) script</param>
        /// <param name="mainnet">generate mainnet address, otherwise testnet address</param>
        /// <returns>bech32 encoded string with checksum and header</returns>
        public static string EncodeWithHeaderAndChecksum(byte witnessVersion, byte[] witnessScriptData, bool mainnet = true)
        {
            // header is bc for mainnet, or tb for testnet
            var header = mainnet ? "bc" : "tb";
            // convert byte array from 8bits to 5
            var base5 = ConvertBits(witnessScriptData, 8, 5, true);

            // generate checksum from witnessVersion concat base5 array
            var checksumBytes = CreateChecksum(header, ArrayTools.ConcatArrays(new[] { witnessVersion }, base5));
            // append the checksum to the base5 array
            var combined = base5.Concat(checksumBytes).ToArray();

            // convert byte array to bech32 string
            var encoded = BytesToBech32String(combined);

            // return address format - Header+Separator(1)+WitnessVersion+EncodedData
            return header + "1" + Digits[witnessVersion] + encoded;
        }

        /// <summary>
        ///     Internal function that computes bech32 checksum
        /// </summary>
        /// <param name="values">base5 byte array of the WitnessVersion+Address</param>
        /// <returns>bech32 checksum</returns>
        private static uint PolyMod(IEnumerable<byte> values)
        {
            uint chk = 1;
            foreach (var value in values)
            {
                var top = chk >> 25;
                chk = value ^ ((chk & 0x1ffffff) << 5);
                chk = Enumerable.Range(0, 5).Aggregate(chk, (current, i) => current ^ (((top >> i) & 1) == 1 ? Generator[i] : 0));
            }
            return chk;
        }

        /// <summary>
        ///     Expands the Human-Readable-Part of the string into values for checksum computation
        /// </summary>
        /// <param name="hrp">human-readable-part of string (e.g. bc prefix)</param>
        /// <returns></returns>
        private static IEnumerable<byte> HrpExpand(string hrp)
        {
            var ret = new byte[hrp.Length * 2 + 1];
            var len = hrp.Length;
            for (var i = 0; i < len; i++)
            {
                ret[i] = (byte)(hrp[i] >> 5);
            }
            ret[len] = 0;
            for (var i = 0; i < len; i++)
            {
                ret[len + 1 + i] = (byte)(hrp[i] & 31);
            }

            return ret;
        }

        /// <summary>
        ///     Creates the checksum for a bech32 format bitcoin address
        /// </summary>
        /// <param name="hrp">human-readable-part (e.g. bc)</param>
        /// <param name="data">base5 byte array of witness version + address</param>
        /// <returns>bech32 checksum bytes</returns>
        private static IEnumerable<byte> CreateChecksum(string hrp, IEnumerable<byte> data)
        {
            var values = HrpExpand(hrp).Concat(data).ToArray();
            values = values.Concat(new byte[6]).ToArray();

            // Create PolyMod checksum and flip LSB of result
            var checksum = PolyMod(values) ^ 1;

            for (var i = 0; i < 6; i++)
                // expand from 4 bytes to 6 and chop off the MSB
                yield return (byte)(checksum >> (5 * (5 - i)) & 0x1f);
        }

        /// <summary>
        ///     Converts an array of bytes from FROM bits to TO bits
        /// </summary>
        /// <param name="data">byte array in FROM bits</param>
        /// <param name="from">word size in source array</param>
        /// <param name="to">word size for destination array</param>
        /// <param name="strictMode">leaves prefix 0 bits</param>
        /// <returns>byte array in TO bits</returns>
        private static byte[] ConvertBits(IReadOnlyCollection<byte> data, int from, int to, bool strictMode = false)
        {
            var d = data.Count * from / (double)to;
            var length = strictMode ? (int)Math.Floor(d) : (int)Math.Ceiling(d);
            var mask = (1 << to) - 1;
            var result = new byte[length];
            var index = 0;
            var accumulator = 0;
            var bits = 0;
            foreach (var value in data)
            {
                accumulator = (accumulator << from) | value;
                bits += from;
                while (bits >= to)
                {
                    bits -= to;
                    result[index] = (byte)((accumulator >> bits) & mask);
                    ++index;
                }
            }

            if (strictMode) return result;
            if (bits <= 0) return result;

            result[index] = (byte)((accumulator << (to - bits)) & mask);
            ++index;

            return result;
        }

        /// <summary>
        ///     Converts byte array into bech32 string
        /// </summary>
        /// <param name="input">byte array of the address</param>
        /// <returns>bech32 string</returns>
        private static string BytesToBech32String(IEnumerable<byte> input)
        {
            var ret = string.Empty;
            foreach (var byteC in input)
            {
                if ((byteC & 0xe0) != 0)
                    return null;
                ret += Digits[byteC];
            }

            return ret;
        }
    }
}