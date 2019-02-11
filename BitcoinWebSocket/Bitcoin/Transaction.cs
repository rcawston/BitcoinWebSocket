using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using BitcoinWebSocket.Schema;
using BitcoinWebSocket.Util;
using LiteDB;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a bitcoin transaction
    /// </summary>
    public class Transaction : Serializer, IDatabaseData
    {
        public TXInput[] Inputs { get; private set; }
        public TXOutput[] Outputs { get; private set; }

        public string TXIDHex { get; set; }
        public bool HasWitness { get; private set; }
        public uint TXVersion { get; private set; }
        public uint LockTime { get; private set; }
        public bool LengthMatch { get; private set; }
        public string IncludedInBlockHex { get; set; }

        // database fields:
        public ObjectId Id { get; set; }
        public long LastUpdated { get; set; }
        public long FirstSeen { get; set; }
        public int IncludedAtBlockHeight { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - creates an empty transaction object
        ///     - used for LiteDB queries
        /// </summary>
        public Transaction() : base(new List<byte>())
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - decodes the transaction data
        /// </summary>
        /// <param name="txBytes">raw transaction as byte array</param>
        public Transaction(IEnumerable<byte> txBytes) : base(txBytes)
        {
            var sha256 = new SHA256Managed();
            // double sha256 hash, reverse bytes, then convert to hex
            TXIDHex = ByteToHex.ByteArrayToHex(sha256.ComputeHash(sha256.ComputeHash(ByteData)).Reverse().ToArray());
            Decode();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - creates a transaction object given transaction properties
        ///     - used for transactions found in blocks
        /// </summary>
        public Transaction(IEnumerable<byte> txBytes, string inclusionBlockHex, uint txVersion, bool hasWitness, TXInput[] inputs, TXOutput[] outputs, uint lockTime) : base(txBytes)
        {
            var sha256 = new SHA256Managed();
            TXIDHex = ByteToHex.ByteArrayToHex(sha256.ComputeHash(sha256.ComputeHash(ByteData)).Reverse().ToArray());
            IncludedInBlockHex = inclusionBlockHex;
            TXVersion = txVersion;
            HasWitness = hasWitness;
            Inputs = inputs;
            Outputs = outputs;
            LockTime = lockTime;
            LengthMatch = true;
        }

        /// <summary>
        ///     Decode - Decodes a raw bitcoin transaction
        /// </summary>
        private void Decode()
        {
            // tx version - uint32
            TXVersion = ReadUInt();

            // if the transaction has witness, next bytes will be 0001
            if (ByteData[Offset] == 0 && ByteData[Offset + 1] == 1)
            {
                Offset += 2;
                HasWitness = true;
            }

            // get the number of inputs - vin length
            Inputs = new TXInput[ReadVarLenInt()];

            // read all the inputs
            for (var i = 0; i < Inputs.Length; ++i)
                Inputs[i] = new TXInput
                {
                    Hash = ReadSlice(32),
                    Index = ReadUInt(),
                    Script = ReadSlice(
                        (int) ReadVarLenInt()), // script length maximum is 520 bytes, so casting to int should be fine
                    Sequence = ReadUInt()
                };


            // get the number of inputs - vout length
            Outputs = new TXOutput[ReadVarLenInt()];

            // read all the outputs
            for (var i = 0; i < Outputs.Length; ++i)
                Outputs[i] = new TXOutput
                {
                    Value = ReadUInt64(),
                    Script = ReadSlice(
                        (int) ReadVarLenInt()) // script length maximum is 520 bytes, so casting to int should be fine
                };

            // if this is a segwit transaction, read in the witnesses
            if (HasWitness)
            {
                foreach (var input in Inputs)
                    input.Witness = ReadVector();
            }

            LockTime = ReadUInt();

            // strict validation - we should be at the end of the transaction
            LengthMatch = Offset == ByteData.Length;
        }
    }
}