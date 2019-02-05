using System.Collections.Generic;
using System.Security.Cryptography;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Bitcoin
{    
    /// <summary>
    ///     Represents a bitcoin block header
    ///     https://en.bitcoin.it/wiki/Block_hashing_algorithm
    /// </summary>
    public class BlockHeader : Serializer
    {
        public uint BlockVersion { get; private set; }
        public byte[] PrevBlockHash { get; private set; }
        public byte[] MerkleRootHash { get; private set; }
        public uint TimeStamp { get; private set; }
        public uint DiffTarget { get; private set; }
        public uint Nonce { get; private set; }
        public byte[] BlockHash { get; private set; }

        public bool LengthMatch { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - decodes the block header data
        /// </summary>
        /// <param name="headerBytes">raw block header as byte array</param>
        public BlockHeader(IEnumerable<byte> headerBytes) : base(headerBytes)
        {
            Decode();
        }
        
        /// <summary>
        ///     Decode - Decodes a raw bitcoin block header
        /// </summary>
        private void Decode()
        {
            // block version number
            BlockVersion = ReadUInt();

            // previous block hash
            PrevBlockHash = ReadSlice(32);

            // merkle root hash
            MerkleRootHash = ReadSlice(32);

            // block timestamp (seconds since 1970-01-01T00:00 UTC)
            TimeStamp = ReadUInt();

            // difficulty target in compact format
            DiffTarget = ReadUInt();

            // nonce
            Nonce = ReadUInt();

            // strict validation - we should be at the end of the header
            LengthMatch = Offset == ByteData.Length;

            // blockHash = sha256(sha256(header_data))
            SHA256 sha256 = new SHA256Managed();
            BlockHash = sha256.ComputeHash(sha256.ComputeHash(ByteData));
        }
    }
}