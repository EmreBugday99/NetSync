using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetSync2
{
    public class Packet
    {
        public NetConnection TargetConnection;

        private List<byte> _buffer;
        private byte[] _readBuffer;
        private int _readPosition;

        /// <summary>
        /// Should be used for serializing data.
        /// </summary>
        public Packet()
        {
            _buffer = new List<byte>();
        }

        /// <summary>
        /// Should be used for deserializing received packets.
        /// </summary>
        /// <param name="receivedData">Received data</param>
        public Packet(byte[] receivedData)
        {
            _readBuffer = receivedData;
        }

        /// <summary>
        /// Returns the packet buffer as an array.
        /// </summary>
        /// <returns>Packet buffer</returns>
        public byte[] GetByteArray()
        {
            if (_buffer != null) return _buffer.ToArray();

            return _readBuffer;
        }

        public ushort GetRemainingBytes()
        {
            return (ushort)(_readBuffer.Length - _readPosition);
        }

        #region Write Methods

        /// <summary>
        /// Writes byte to into the packet buffer
        /// </summary>
        /// <param name="data">Byte to write</param>
        public void WriteByte(byte data)
            => _buffer.Add(data);

        /// <summary>
        /// Writes a full byte array into the packet buffer.
        /// </summary>
        /// <param name="data">Byte array to write.</param>
        public void WriteByteArray(byte[] data)
            => _buffer.AddRange(data);

        /// <summary>
        /// Inserts byte into specified index of the buffer.
        /// </summary>
        /// <param name="index">Which index to insert in the buffer</param>
        /// <param name="data">The byte data to insert</param>
        public void InsertByte(int index, byte data)
            => _buffer.Insert(index, data);

        /// <summary>
        /// Writes bool into the packet buffer.
        /// 0 : False
        /// 1 : True
        /// </summary>
        /// <param name="data">Boolean to write</param>
        public void WriteBool(bool data)
        {
            if (data == false)
                WriteByte(0);
            else
                WriteByte(1);
        }

        /// <summary>
        /// Writes 16bit integer into the packet buffer.
        /// </summary>
        /// <param name="data">16bit integer to write.</param>
        public void WriteShort(short data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Inserts short into specified index of the buffer.
        /// </summary>
        /// <param name="index">Which index to insert in the buffer</param>
        /// <param name="data">The short data to insert</param>
        public void InsertShort(int index, short data)
            => _buffer.InsertRange(index, BitConverter.GetBytes(data));

        /// <summary>
        /// Writes unsigned 16bit integer into the packet buffer.
        /// </summary>
        /// <param name="data">16bit unsigned integer to write.</param>
        public void WriteUnsignedShort(ushort data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Inserts unsigned short into specified index of the buffer.
        /// </summary>
        /// <param name="index">Which index to insert in the buffer</param>
        /// <param name="data">The unsigned short data to insert</param>
        public void InsertUnsignedShort(int index, ushort data)
            => _buffer.InsertRange(index, BitConverter.GetBytes(data));

        /// <summary>
        /// Writes 32bit integer into the packet buffer.
        /// </summary>
        /// <param name="data">32bit integer to write.</param>
        public void WriteInteger(int data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Inserts integer into specified index of the buffer.
        /// </summary>
        /// <param name="index">Which index to insert in the buffer</param>
        /// <param name="data">The int data to insert</param>
        public void InsertInteger(int index, int data)
            => _buffer.InsertRange(index, BitConverter.GetBytes(data));

        /// <summary>
        /// Writes 32bit unsigned integer into the packet buffer.
        /// </summary>
        /// <param name="data">32bit unsigned integer to write.</param>
        public void WriteUnsignedInteger(uint data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Writes 64bit integer into the packet buffer.
        /// </summary>
        /// <param name="data">64bit integer to write.</param>
        public void WriteLong(long data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Writes 64bit unsigned integer into the packet buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteUnsignedLong(UInt64 data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Writes float into the packet buffer.
        /// </summary>
        /// <param name="data">Float to write.</param>
        public void WriteFloat(float data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Writes double into the packet buffer.
        /// </summary>
        /// <param name="data">Double to write.</param>
        public void WriteDouble(double data)
            => _buffer.AddRange(BitConverter.GetBytes(data));

        /// <summary>
        /// Writes a dynamic string into the packet buffer.
        /// Length of the string is not known before hand therefore it is written into the packet.
        /// This increases the packet size by 4bytes due to writing the length of the string.
        /// If you know the length of the string beforehand you should use WriteStaticString.
        /// </summary>
        /// <param name="data">Dynamic string to write</param>
        public void WriteString(string data)
        {
            _buffer.AddRange(BitConverter.GetBytes(data.Length));
            _buffer.AddRange(Encoding.ASCII.GetBytes(data));
        }

        /// <summary>
        /// Writes a static string into the packet buffer where the length of the string is known beforehand.
        /// </summary>
        /// <param name="data">Static string to write</param>
        public void WriteStaticString(string data)
            => _buffer.AddRange(Encoding.ASCII.GetBytes(data));

        #endregion Write Methods

        #region Read Methods

        /// <summary>
        /// Reads a byte from the read buffer.
        /// </summary>
        /// <returns>Read byte</returns>
        public byte ReadByte()
        {
            if (_readBuffer.Length >= _readPosition + 1)
            {
                byte returnData = _readBuffer[_readPosition];
                _readPosition += 1;
                return returnData;
            }

            throw new Exception("Error while reading Int16 from buffer!");
        }

        /// <summary>
        /// Reads a boolean from the buffer.
        /// </summary>
        /// <returns>Read boolean</returns>
        public bool ReadBool()
        {
            if (_readBuffer.Length >= _readPosition + 1)
            {
                byte data = ReadByte();
                if (data == 0)
                    return false;

                return true;
            }

            throw new Exception("Error while reading Boolean from buffer!");
        }

        /// <summary>
        /// Reads a short (16 bit) from the buffer.
        /// </summary>
        /// <returns>Read short</returns>
        public short ReadShort()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 2)
            {
                short returnData = BitConverter.ToInt16(_readBuffer, _readPosition);
                _readPosition += 2;
                return returnData;
            }

            throw new Exception("Error while reading Int16 from buffer!");
        }

        /// <summary>
        /// Reads an unsigned short (16 bit) from the buffer.
        /// </summary>
        /// <returns>Read unsigned short</returns>
        public ushort ReadUnsignedShort()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 2)
            {
                ushort returnData = BitConverter.ToUInt16(_readBuffer, _readPosition);
                _readPosition += 2;
                return returnData;
            }

            throw new Exception("Error while reading UInt16 from buffer!");
        }

        /// <summary>
        /// Reads an integer (32 bit) from the buffer.
        /// </summary>
        /// <returns>Read integer</returns>
        public int ReadInteger()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 4)
            {
                int returnData = BitConverter.ToInt32(_readBuffer, _readPosition);
                _readPosition += 4;
                return returnData;
            }

            throw new Exception("Error while reading Int32 from buffer!");
        }

        /// <summary>
        /// Reads an unsigned integer (32 bit) from buffer
        /// </summary>
        /// <returns>Read unsigned integer</returns>
        public uint ReadUnsignedInteger()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 4)
            {
                uint returnData = BitConverter.ToUInt32(_readBuffer, _readPosition);
                _readPosition += 4;
                return returnData;
            }

            throw new Exception("Error while reading UInt32 from buffer!");
        }

        /// <summary>
        /// Reads a long (64 bit) from buffer
        /// </summary>
        /// <returns>Read long</returns>
        public long ReadLong()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 8)
            {
                long returnData = BitConverter.ToInt64(_readBuffer, _readPosition);
                _readPosition += 8;
                return returnData;
            }

            throw new Exception("Error while reading Int64 from buffer!");
        }

        /// <summary>
        /// Reads an unsigned long (64 bit) from buffer.
        /// </summary>
        /// <returns>Read unsigned long</returns>
        public ulong ReadUnsignedLong()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 8)
            {
                ulong returnData = BitConverter.ToUInt64(_readBuffer, _readPosition);
                _readPosition += 8;
                return returnData;
            }

            throw new Exception("Error while reading UInt64 from buffer!");
        }

        /// <summary>
        /// Reads a float from buffer
        /// </summary>
        /// <returns>Read float</returns>
        public float ReadFloat()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 4)
            {
                float returnData = BitConverter.ToSingle(_readBuffer, _readPosition);
                _readPosition += 4;
                return returnData;
            }

            throw new Exception("Error while reading Float from buffer!");
        }

        /// <summary>
        /// Reads a double from buffer
        /// </summary>
        /// <returns>Read double</returns>
        public double ReadDouble()
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + 8)
            {
                double returnData = BitConverter.ToDouble(_readBuffer, _readPosition);
                _readPosition += 8;
                return returnData;
            }

            throw new Exception("Error while reading Double from buffer!");
        }

        /// <summary>
        /// Reads a dynamic string from buffer.
        /// Length of the string is not known from beforehand therefore
        /// the length is written into the buffer as an int.
        /// This increases the packet size therefore if the length is known beforehand
        /// Read/Write Static String should be used instead to reduce overhead.
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            //If there are any bytes left to read in the buffer
            int dataLength = ReadInteger();
            if (_readBuffer.Length >= _readPosition + dataLength)
            {
                string returnData = Encoding.ASCII.GetString(_readBuffer, _readPosition, dataLength);
                _readPosition += dataLength;
                return returnData;
            }

            throw new Exception("Error while reading String from buffer!");
        }

        /// <summary>
        /// Reads a static string from buffer where the
        /// length of the string is known beforehand.
        /// </summary>
        /// <param name="stringLength">Length of th string</param>
        /// <returns>Read string</returns>
        public string ReadStaticString(int stringLength)
        {
            //If there are any bytes left to read in the buffer
            if (_readBuffer.Length >= _readPosition + stringLength)
            {
                string returnData = Encoding.ASCII.GetString(_readBuffer, _readPosition, stringLength);
                _readPosition += stringLength;
                return returnData;
            }

            throw new Exception("Error while reading Static String from buffer!");
        }

        #endregion Read Methods
    }
}