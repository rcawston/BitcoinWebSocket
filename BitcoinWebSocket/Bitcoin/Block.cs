using System.Collections.Generic;
using System.Security.Cryptography;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a bitcoin block
    ///     https://en.bitcoin.it/wiki/Block
    ///     - The block passed by ZMQ starts at the header
    ///     - Unlike to on disk, there is no magic number or blocksize passed at the start of a ZMQ block
    ///     https://en.bitcoin.it/wiki/Block_hashing_algorithm
    /// </summary>
    public class Block : Serializer
    {
        public BlockHeader Header { get; private set; }
        public Transaction[] Transactions { get; private set; }

        public uint BlockSize { get; private set; }
        public byte[] BlockHash => Header.BlockHash;

        public bool LengthMatch { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - decodes the block data
        /// </summary>
        /// <param name="blockBytes">raw block as byte array</param>
        public Block(IEnumerable<byte> blockBytes) : base(blockBytes)
        {
            Decode();
        }

        /// <summary>
        ///     Decode - Decodes a raw bitcoin block
        /// </summary>
        private void Decode()
        {
            BlockSize = (uint) ByteData.Length;

            // header is 80bytes
            Header = new BlockHeader(ReadSlice(80));

            // get the number of transactions
            Transactions = new Transaction[ReadVarLenInt()];

            // read all the transactions
            for (var i = 0; i < Transactions.Length; ++i)
            {
                // decode the transaction
                Transactions[i] = DecodeTX();
            }

            // strict validation - we should be at the end of the block
            LengthMatch = Offset == ByteData.Length;
        }
        
        /// <summary>
        ///     Decode - Decodes a raw bitcoin transaction
        /// </summary>
        private Transaction DecodeTX()
        {
            // tx version - uint32
            var txVersion = ReadUInt();
            var hasWitness = false;

            // if the transaction has witness, next bytes will be 0001
            if (ByteData[Offset] == 0 && ByteData[Offset + 1] == 1)
            {
                Offset += 2;
                hasWitness = true;
            }

            // get the number of inputs - vin length
            var inputs = new TXInput[ReadVarLenInt()];

            // read all the inputs
            for (var i = 0; i < inputs.Length; ++i)
                inputs[i] = new TXInput
                {
                    Hash = ReadSlice(32),
                    Index = ReadUInt(),
                    Script = ReadSlice(
                        (int)ReadVarLenInt()), // script length maximum is 520 bytes, so casting to int should be fine
                    Sequence = ReadUInt()
                };


            // get the number of inputs - vout length
            var outputs = new TXOutput[ReadVarLenInt()];

            // read all the outputs
            for (var i = 0; i < outputs.Length; ++i)
                outputs[i] = new TXOutput
                {
                    Value = ReadUInt64(),
                    Script = ReadSlice(
                        (int)ReadVarLenInt()) // script length maximum is 520 bytes, so casting to int should be fine
                };

            // if this is a segwit transaction, read in the witnesses
            if (hasWitness)
            {
                foreach (var input in inputs)
                    input.Witness = ReadVector();
            }

            var lockTime = ReadUInt();

            // strict validation - we should be at the end of the transaction
            LengthMatch = Offset == ByteData.Length;

            return new Transaction(BlockHash, txVersion, hasWitness, inputs, outputs, lockTime);
        }
    }
}