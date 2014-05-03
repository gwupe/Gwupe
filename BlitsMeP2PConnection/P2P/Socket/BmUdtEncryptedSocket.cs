using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet;
using log4net;

namespace BlitsMe.Communication.P2P.P2P.Socket
{
    public class BmUdtEncryptedSocket : BmUdtSocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BmUdtSocket));
        public EncryptData EncryptData { get; set; }
        public DecryptData DecryptData { get; set; }
        private readonly byte[] _pendingData = new byte[16384];
        private int _pendingByteCount = 0;

        public BmUdtEncryptedSocket() : base() {}

        public BmUdtEncryptedSocket(String encryptionKey) : base()
        {
            // assume aes encryption
            var aes = new AesCryptoPacketUtil(Encoding.UTF8.GetBytes(encryptionKey));
            EncryptData = aes.EncryptData;
            DecryptData = aes.DecryptData;
        }

        public new void Send(byte[] data, int length)
        {
            try
            {
                data = EncryptData(data, length);
#if DEBUG
                //Logger.Debug("Encrypted " + length + " bytes to " + data.Length + " bytes.");
#endif
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to encrypt data before streaming", ex);
                throw;
            }
            length = data.Length;
            // Send using the base class
            base.Send(data, length);
        }

        public new int Read(byte[] data, int maxRead)
        {
            int read = 0;
            // return pending bytes instead of actually reading
            if (_pendingByteCount > 0)
            {
                Array.Copy(_pendingData, data, _pendingByteCount);
                read = _pendingByteCount;
                _pendingByteCount = 0;
#if DEBUG
                //                    Logger.Debug("Returned pending " + read + " bytes");
#endif
            }
            else
            {
                // no pending bytes, we can actually read
                byte[] decryptedData = null;
                while (decryptedData == null)
                {
                    read = UdtConnection.Receive(data, 0, maxRead);
#if DEBUG
                    //Logger.Debug("Read " + read + " bytes of a possible " + maxRead + " bytes from the udt stream");
#endif

                    if (read == 0)
                    {
                        return 0;
                    }
                    try
                    {
                        decryptedData = DecryptData(data, read);
                        if (decryptedData == null)
                        {
#if DEBUG
                            //Logger.Debug("Decryption of data incomplete, need to read more data.");
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to decrypt data from stream", ex);
                        throw;
                    }
                }
#if DEBUG
                //Logger.Debug("Decrypted " + read + " bytes to " + decryptedData.Length + " bytes from the udt stream");
#endif
                // Sometimes after a decrypt, the data can be larger than we can handle (> maxRead), so we buffer it and return it on the next read
                if (decryptedData.Length > maxRead)
                {
                    Array.Copy(decryptedData, data, maxRead);
                    _pendingByteCount = decryptedData.Length - maxRead;
                    Array.Copy(decryptedData, maxRead, _pendingData, 0, _pendingByteCount);
#if DEBUG
                    //Logger.Debug("Too much data after decrypt, keeping " + pendingByteCount + " bytes back for next read");
#endif
                    read = maxRead;
                }
                else
                {
                    Array.Copy(decryptedData, data, decryptedData.Length);
                    read = decryptedData.Length;
                }
            }
            return read;
        }
    }
}
