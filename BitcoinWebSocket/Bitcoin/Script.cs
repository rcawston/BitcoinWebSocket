using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a bitcoin script
    /// </summary>
    public class Script
    {
        // byte array of the script
        public byte[] ScriptBytes { get; }
        // ordered op codes of the script
        public List<OpCodeType> OpCodes { get; }
        // data in the script
        public List<byte[]> DataChunks { get; }

        /// <summary>
        ///     Constructor
        ///     - saves the specified raw script
        ///     - generates op_code list and retrieves data chunks
        /// </summary>
        /// <param name="script">script as byte array</param>
        public Script(IEnumerable<byte> script)
        {
            // save the script bytes
            ScriptBytes = script.ToArray();
            // create the op_code and data storage
            OpCodes = new List<OpCodeType>();
            DataChunks = new List<byte[]>();
            // populate op_codes and retrieve data sections from the script
            Decode();
        }

        /// <summary>
        ///     Decodes the script into op_codes and data sections
        /// </summary>
        private void Decode()
        {
            // Iterate over each byte in the script.
            // When a data chunk (non-op_code section) is encountered,
            //   i will be incremented so as to skip reading op_codes from the data
            for (var i = 0; i < ScriptBytes.Length; i++)
            {
                // OP_PUSH - indicates a data section of ScriptBytes[i] length
                if (ScriptBytes[i] > 1 && ScriptBytes[i] < 76)
                {
                    // save the data chunk and add an OP_DATA code to the script
                    var dataLength = ScriptBytes[i];
                    DataChunks.Add(ScriptBytes.Skip(i + 1).Take(dataLength).ToArray());
                    OpCodes.Add(OpCodeType.OP_DATA);
                    // increment i to skip over the data section during further op_code processing
                    i += dataLength;
                }
                else switch (ScriptBytes[i])
                {
                    // OP_PUSHDATA1 - indicates a data section of ScriptBytes[i+1] length
                    case (byte) OpCodeType.OP_PUSHDATA1:
                    {
                        // save the data chunk and an OP_DATA code to the script
                        var dataLength = ScriptBytes[i + 1];
                        DataChunks.Add(ScriptBytes.Skip(i + 2).Take(dataLength).ToArray());
                        OpCodes.Add(OpCodeType.OP_DATA);
                        // increment i to skip over the data section during further op_code processing
                        i += 1 + dataLength;
                        break;
                    }
                    // OP_PUSHDATA2 - indicates a data section with 2 bytes indicating length
                    case (byte) OpCodeType.OP_PUSHDATA2:
                    {
                        // get 2 byte count and data
                        var dataLength = BitConverter.ToInt16(ScriptBytes, i + 1);
                        DataChunks.Add(ScriptBytes.Skip(i + 3).Take(dataLength).ToArray());
                        OpCodes.Add(OpCodeType.OP_DATA);
                        i += 2 + dataLength;
                        break;
                    }
                    // OP_PUSHDATA2 - indicates a data section with 4 bytes indicating length
                    case (byte) OpCodeType.OP_PUSHDATA4:
                    {
                        // get 4 byte count and data
                        var dataLength = BitConverter.ToInt32(ScriptBytes, i + 1);
                        DataChunks.Add(ScriptBytes.Skip(i + 5).Take(dataLength).ToArray());
                        OpCodes.Add(OpCodeType.OP_DATA);
                        i += 4 + dataLength;
                        break;
                    }
                    // any other OP_CODE (non-data identifier)
                    default:
                    {
                        // check if this is a valid/known op code, and add it to the list
                        if (Enum.IsDefined(typeof(OpCodeType), ScriptBytes[i]) && ScriptBytes[i] != 218) // 218 = DA
                            OpCodes.Add((OpCodeType) ScriptBytes[i]);
                        else
                        {
                            // TODO: handle unknown OP_CODE... this shouldn't happen
                            return;
                        }
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
	///     Script op codes
	///     https://github.com/bitcoin/bitcoin/blob/v0.17.1/src/script/script.h#L46L187
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum OpCodeType : byte
    {
        // OP_CODES above 0xB9 (OP_NOP10) are invalid - we use 0xDA internally to indicate a data chunk
        OP_DATA = 0xda,

        // push value
        OP_0 = 0x00,
        OP_FALSE = OP_0,
        OP_PUSHDATA1 = 0x4c,
        OP_PUSHDATA2 = 0x4d,
        OP_PUSHDATA4 = 0x4e,
        OP_1NEGATE = 0x4f,
        OP_RESERVED = 0x50,
        OP_1 = 0x51,
        OP_TRUE = OP_1,
        OP_2 = 0x52,
        OP_3 = 0x53,
        OP_4 = 0x54,
        OP_5 = 0x55,
        OP_6 = 0x56,
        OP_7 = 0x57,
        OP_8 = 0x58,
        OP_9 = 0x59,
        OP_10 = 0x5a,
        OP_11 = 0x5b,
        OP_12 = 0x5c,
        OP_13 = 0x5d,
        OP_14 = 0x5e,
        OP_15 = 0x5f,
        OP_16 = 0x60,

        // control
        OP_NOP = 0x61,
        OP_VER = 0x62,
        OP_IF = 0x63,
        OP_NOTIF = 0x64,
        OP_VERIF = 0x65,
        OP_VERNOTIF = 0x66,
        OP_ELSE = 0x67,
        OP_ENDIF = 0x68,
        OP_VERIFY = 0x69,
        OP_RETURN = 0x6a,

        // stack ops
        OP_TOALTSTACK = 0x6b,
        OP_FROMALTSTACK = 0x6c,
        OP_2DROP = 0x6d,
        OP_2DUP = 0x6e,
        OP_3DUP = 0x6f,
        OP_2OVER = 0x70,
        OP_2ROT = 0x71,
        OP_2SWAP = 0x72,
        OP_IFDUP = 0x73,
        OP_DEPTH = 0x74,
        OP_DROP = 0x75,
        OP_DUP = 0x76,
        OP_NIP = 0x77,
        OP_OVER = 0x78,
        OP_PICK = 0x79,
        OP_ROLL = 0x7a,
        OP_ROT = 0x7b,
        OP_SWAP = 0x7c,
        OP_TUCK = 0x7d,

        // splice ops
        OP_CAT = 0x7e,
        OP_SUBSTR = 0x7f,
        OP_LEFT = 0x80,
        OP_RIGHT = 0x81,
        OP_SIZE = 0x82,

        // bit logic
        OP_INVERT = 0x83,
        OP_AND = 0x84,
        OP_OR = 0x85,
        OP_XOR = 0x86,
        OP_EQUAL = 0x87,
        OP_EQUALVERIFY = 0x88,
        OP_RESERVED1 = 0x89,
        OP_RESERVED2 = 0x8a,

        // numeric
        OP_1ADD = 0x8b,
        OP_1SUB = 0x8c,
        OP_2MUL = 0x8d,
        OP_2DIV = 0x8e,
        OP_NEGATE = 0x8f,
        OP_ABS = 0x90,
        OP_NOT = 0x91,
        OP_0NOTEQUAL = 0x92,

        OP_ADD = 0x93,
        OP_SUB = 0x94,
        OP_MUL = 0x95,
        OP_DIV = 0x96,
        OP_MOD = 0x97,
        OP_LSHIFT = 0x98,
        OP_RSHIFT = 0x99,

        OP_BOOLAND = 0x9a,
        OP_BOOLOR = 0x9b,
        OP_NUMEQUAL = 0x9c,
        OP_NUMEQUALVERIFY = 0x9d,
        OP_NUMNOTEQUAL = 0x9e,
        OP_LESSTHAN = 0x9f,
        OP_GREATERTHAN = 0xa0,
        OP_LESSTHANOREQUAL = 0xa1,
        OP_GREATERTHANOREQUAL = 0xa2,
        OP_MIN = 0xa3,
        OP_MAX = 0xa4,

        OP_WITHIN = 0xa5,

        // crypto
        OP_RIPEMD160 = 0xa6,
        OP_SHA1 = 0xa7,
        OP_SHA256 = 0xa8,
        OP_HASH160 = 0xa9,
        OP_HASH256 = 0xaa,
        OP_CODESEPARATOR = 0xab,
        OP_CHECKSIG = 0xac,
        OP_CHECKSIGVERIFY = 0xad,
        OP_CHECKMULTISIG = 0xae,
        OP_CHECKMULTISIGVERIFY = 0xaf,

        OP_CHECKLOCKTIMEVERIFY = 0xb1,
        OP_CHECKSEQUENCEVERIFY = 0xb2,

        // expansion
        OP_NOP1 = 0xb0,
        OP_NOP2 = 0xb1,
        OP_NOP3 = 0xb2,
        OP_NOP4 = 0xb3,
        OP_NOP5 = 0xb4,
        OP_NOP6 = 0xb5,
        OP_NOP7 = 0xb6,
        OP_NOP8 = 0xb7,
        OP_NOP9 = 0xb8,
        OP_NOP10 = 0xb9
    };
}