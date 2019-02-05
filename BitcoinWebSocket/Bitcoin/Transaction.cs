using System.Collections.Generic;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a bitcoin transaction
    /// </summary>
    public class Transaction : Serializer
    {
        public TXInput[] Inputs { get; private set; }
        public TXOutput[] Outputs { get; private set; }

        public bool HasWitness { get; private set; }
        public uint TXVersion { get; private set; }
        public uint LockTime { get; private set; }
        public bool LengthMatch { get; private set; }
        public byte[] IncludedInBlock { get; }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - decodes the transaction data
        /// </summary>
        /// <param name="txBytes">raw transaction as byte array</param>
        public Transaction(IEnumerable<byte> txBytes) : base(txBytes)
        {
            Decode();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - creates a transaction object given transaction properties
        /// </summary>
        public Transaction(byte[] inclusionBlock, uint txVersion, bool hasWitness, TXInput[] inputs, TXOutput[] outputs, uint lockTime) : base(new byte[0])
        {
            IncludedInBlock = inclusionBlock;
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