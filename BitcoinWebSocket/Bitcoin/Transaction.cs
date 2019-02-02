using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinWebSocket.Bitcoin
{
    public class Transaction
    {
        private readonly byte[] _txBytes;
        private int _offset;

        public TXInput[] Inputs { get; private set; }
        public TXOutput[] Outputs { get; private set; }

        public bool HasWitness { get; private set; }
        public int TXVersion { get; private set; }
        public int LockTime { get; private set; }
        public bool LengthMatch { get; private set; }

        public Transaction(IEnumerable<byte> txBytes)
        {
            _txBytes = txBytes.ToArray();
            Decode();
        }

        /// <summary>
        ///     ReadUInt - returns a 16bit integer from the transaction bytes at the current offset
        /// </summary>
        /// <returns>int</returns>
        private int ReadUInt16()
        {
            var ret = BitConverter.ToInt16(_txBytes, _offset);
            _offset += 2;
            return ret;
        }

        /// <summary>
        ///     ReadUInt - returns a 32bit integer from the transaction bytes at the current offset
        /// </summary>
        /// <returns>int</returns>
        private int ReadUInt()
        {
            var ret = BitConverter.ToInt32(_txBytes, _offset);
            _offset += 4;
            return ret;
        }

        /// <summary>
        ///     ReadUInt - returns a 32bit integer from the transaction bytes at the current offset
        /// </summary>
        /// <returns>int</returns>
        private long ReadUInt64()
        {
            var ret = BitConverter.ToInt64(_txBytes, _offset);
            _offset += 8;
            return ret;
        }

        /// <summary>
        ///     ReadVector - returns a vector slice from the transaction bytes at the current offset
        /// </summary>
        /// <returns>bytes</returns>
        private byte[] ReadVector()
        {
            var count = ReadVarLenInt();
            var ret = new byte[0];
            for (var i = 0; i < count; i++)
            {
                var slice = ReadSlice((int) ReadVarLenInt());
                var buff = new byte[ret.Length + slice.Length];
                Buffer.BlockCopy(ret, 0, buff, 0, ret.Length);
                Buffer.BlockCopy(slice, 0, buff, ret.Length, slice.Length);
                ret = buff;
            }

            return ret;
        }

        /// <summary>
        ///     ReadSlice - returns a slice of the transaction bytes at the current offset
        /// </summary>
        /// <param name="size">size of slice to return</param>
        /// <returns>bytes</returns>
        private byte[] ReadSlice(int size)
        {
            _offset += size;
            return _txBytes.Skip(_offset - size).Take(size).ToArray();
        }

        /// <summary>
        ///     ReadVarLenInt - returns a variable length bitcoin integer from the transaction bytes at the current offset
        ///     - if first byte is below 0xFD, length is single byte (uint8)
        ///     - if first byte is 0xFD, length is the following 2 bytes (uint16)
        ///     - if first byte is 0xFE, length is the following 4 bytes (uint32)
        ///     - if first byte is 0xFF, length is the following 8 bytes (uint64)
        /// </summary>
        /// <returns>int</returns>
        private long ReadVarLenInt()
        {
            if (_txBytes[_offset] < 253)
                return _txBytes[_offset++];

            _offset++;
            if (_txBytes[_offset-1] == 253)
            {
                return ReadUInt16();
            }

            return _txBytes[_offset-1] == 254 ? ReadUInt() : ReadUInt64();
        }

        /// <summary>
        ///     Decode - Decodes a raw bitcoin transaction
        /// </summary>
        private void Decode()
        {
            // tx version - int32
            TXVersion = ReadUInt();

            // if the transaction has witness, next bytes will be 0001
            if (_txBytes[_offset] == 0 && _txBytes[_offset + 1] == 1)
            {
                _offset += 2;
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
            LengthMatch = _offset == _txBytes.Length;
        }
    }
}