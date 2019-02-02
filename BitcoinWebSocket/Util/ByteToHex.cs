using System;
using System.Runtime.InteropServices;

namespace BitcoinWebSocket.Util
{
    /// <summary>
    ///     (Probably over-) optimized ByteToHex conversion script
    ///     - doesn't get faster then this unsafe code!
    /// </summary>
    public unsafe class ByteToHex
    {
        private static readonly uint[] Lookup32Unsafe = CreateLookup32Unsafe();

        private static readonly uint* Lookup32UnsafeP =
            (uint*)GCHandle.Alloc(Lookup32Unsafe, GCHandleType.Pinned).AddrOfPinnedObject();

        /// <summary>
        ///     Create a table of hex pairs from 0 to 255
        /// </summary>
        /// <returns>lookup table array</returns>
        private static uint[] CreateLookup32Unsafe()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = s[0] + ((uint)s[1] << 16);
                else
                    result[i] = s[1] + ((uint)s[0] << 16);
            }

            return result;
        }

        /// <summary>
        ///     Use lookup table to convert Byte Array to hex string
        /// </summary>
        /// <param name="bytes">input array to convert</param>
        /// <returns>hex conversion of the byte array</returns>
        public static string ByteArrayToHex(byte[] bytes)
        {
            var lookupP = Lookup32UnsafeP;
            var result = new char[bytes.Length * 2];
            fixed (byte* bytesP = bytes)
            fixed (char* resultP = result)
            {
                var resultP2 = (uint*)resultP;
                for (var i = 0; i < bytes.Length; i++) resultP2[i] = lookupP[bytesP[i]];
            }

            return new string(result);
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
