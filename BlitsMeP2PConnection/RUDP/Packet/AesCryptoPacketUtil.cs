using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gwupe.Common.Security;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Communication.P2P.RUDP.Packet
{
    public class AesCryptoPacketUtil
    {
        private const int MaxBufferSize = 4112;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AesCryptoPacketUtil));
        private readonly byte[] _key;
        private readonly byte[] _overflowBuffer;
        private int _pendingBytes = 0;

        public AesCryptoPacketUtil(byte[] key, int overflowBufferSize = 16384)
        {
            _key = key;
            _overflowBuffer = new byte[overflowBufferSize];
        }

        public byte[] EncryptData(byte[] data, int length)
        {
            // if the length is greater than 4080 (after encryption, pads to 4096), we will have to cut it up 
            // so that our frame counter (1 byte) works
            int consumed = 0;
            MemoryStream stream = new MemoryStream();
            int count = 0;
            do
            {
                int consume = length - consumed > 4080 ? 4080 : length - consumed;
                // Encrypt the data and add the IV
                var encryptedData = Util.getSingleton().AesEncryptBytes(data, consumed, consume, _key);
                consumed += consume;
                // Now we need to prepend the length (always divisible by 16, so we can cheat and use a byte which 
                // still gives us a max buffer size of 255*16)
                // encLength (number of hextets - 16 (IV) - 16 (first frame))
                var encLength = (byte)((encryptedData.Length - 32) / 16);
                // Now construct the final array which ultimately includes 1 byte length, 16 byte IV and unknown length 
                // encrypted message (but guaranteed >= 16)
                // note, for this encLength to be useful, you will need to add 2 then times by 16
                stream.WriteByte(encLength);
                stream.Write(encryptedData, 0, encryptedData.Length);
                count++;
            } while (length > consumed);
#if DEBUG
            //Logger.Debug("Encrypted " + length + " bytes into " + count + " frames");
#endif
            return stream.ToArray();
        }

        public byte[] DecryptData(byte[] data, int length)
        {
#if DEBUG
            //Logger.Debug("Decrypting " + length + " bytes");
#endif
            if (_pendingBytes > 0)
            {
                // We have stuff left in our buffer
                // What is left here will start with our count
                var newData = new byte[_pendingBytes + length];
                Array.Copy(_overflowBuffer, newData, _pendingBytes);
                Array.Copy(data, 0, newData, _pendingBytes, length);
                data = newData;
                length = length + _pendingBytes;
#if DEBUG
                //Logger.Debug("Added " + _pendingBytes + " bytes, now Decrypting " + length + " bytes");
#endif
                _pendingBytes = 0;
            }
            int encLength = data[0] * 16 + 32;
#if DEBUG
            //Logger.Debug("Embedded packet is of length " + encLength);
#endif
            int offset = 1;
            int consumed = 0;
            // try avoiding using memory stream
            MemoryStream fullBytes = null;
            byte[] packetBytes = null;
            // while we can pull a full packet from the data
            while (encLength <= length - offset)// && (consumed + encLength <= MaxBufferSize))
            {
#if DEBUG
                //Logger.Debug("Full packet is available");
#endif
                // if we have been through once before, store that data in memory stream
                if (packetBytes != null)
                {
                    if (fullBytes == null)
                    {
                        fullBytes = new MemoryStream();
                    }
                    fullBytes.Write(packetBytes, 0, packetBytes.Length);
                }
                // Decrypt the full packet
                packetBytes = Util.getSingleton().AesDecryptBytes(data, offset, encLength, _key);
                consumed = offset + encLength;
                // if the amount we read was less 
                if (encLength < length - offset)
                {
                    offset = consumed + 1;
                    encLength = data[consumed] * 16 + 32;
#if DEBUG
                   // Logger.Debug("Embedded packet is of length " + encLength);
#endif
                }
                else
                {
                    break;
                }
            }
            if (consumed < length)
            {
#if DEBUG
                //Logger.Debug("Full packet is not available, need " + encLength + ", only have " + (length - offset) + " bytes, saving " + (length - consumed) + " bytes (incl length byte)");
#endif
                _pendingBytes = length - consumed;
                Array.Copy(data, consumed, _overflowBuffer, 0, length - consumed);
            }
            if (fullBytes != null)
            {
                fullBytes.Write(packetBytes, 0, packetBytes.Length);
                packetBytes = fullBytes.ToArray();
            }
#if DEBUG
            /*
            if (packetBytes != null)
            {
                Logger.Debug("Successfully decrypted " + packetBytes.Length + " bytes");
            }
            else
            {
                Logger.Debug("Didn't decrypt any packet this round, not enough data");
            }
             */
#endif
            return packetBytes;

        }
    }
}
