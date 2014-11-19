/*=============================================================================|
| Project : Bauglir Internet Library                                           |
|==============================================================================|
| Content: Generic connection and server                                       |
|==============================================================================|
| Copyright (c)2011-2012, Bronislav Klucka                                     |
| All rights reserved.                                                         |
| Source code is licenced under original 4-clause BSD licence:                 |
| http://licence.bauglir.com/bsd4.php                                          |
|                                                                              |
|                                                                              |
| Project download homepage:                                                   |
|   http://code.google.com/p/bauglir-websocket/                                |
| Project homepage:                                                            |
|   http://www.webnt.eu/index.php                                              |
| WebSocket RFC:                                                               |
|   http://tools.ietf.org/html/rfc6455                                         |
|                                                                              |
|                                                                              |
|=============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Net.Security;
using log4net;
using log4net.Repository.Hierarchy;

namespace Bauglir.Ex
{


    /// <summary>
    /// basic WebSocket connection 
    /// </summary>
    public class WebSocketConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketConnection));

        #region protected properties
        protected TcpClient fClient = null;
        protected int fIndex = WebSocketIndexer.GetIndex();

        protected internal string fCookie = "-";
        protected internal string fExtension = "-";
        protected internal string fOrigin = "-";
        protected internal string fProtocol = "-";
        protected internal string fHost = "";
        protected internal string fPort = "";
        protected internal string fResourceName = "";
        protected internal int fVersion = 0;
        protected internal WebSocketHeaders fHeaders;
        protected internal bool fSsl;
        protected internal SslStream fSslStream;
        protected internal bool fFullDataProcess = false;
        protected internal bool fHandshake = false;
        //fFullDataProcess: boolean;
        //fFullDataStream: TMemoryStream;


        protected bool fRequireMask = false;
        protected bool fMasking = false;

        protected bool fClosedByMe = false;
        protected bool fClosedByPeer = false;

        #endregion protected properties


        #region public properties

        /// <summary>
        /// whether WebSocket handshake has been successfull
        /// </summary>
        public bool Handshake
        {
            get { return fHandshake; }
        }

        /// <summary>
        /// Whether to register for full data processing
        /// 
        /// Full data methods will be called only if this property is true
        /// </summary>
        public bool FullDataProcess
        {
            get { return fFullDataProcess; }
            set { fFullDataProcess = value; }

        }

        /// <summary>
        /// TcpClient representing connection itself
        /// 
        /// propery should not be used for reading and writing data directly (use WebSocketConnection methods),
        /// property is accessible for information usage (e.g. IP address)
        /// </summary>
        public TcpClient Client
        {
            get { return fClient; }
        }


        /// <summary>
        /// whether connection is closed
        /// true, WebSocketConnection object has been closed by object itself, peer, or underlying socket is not connected
        /// </summary>
        public bool Closed
        {
            get { return (fClosedByMe && fClosedByPeer) || (fClient == null) || !fClient.Connected; }
        }

        /// <summary>
        /// whether connection if about to be closed
        /// true, WebSocketConnection object has been closed by object itself, peer but connection is not closed fully (see Closed property)
        /// </summary>
        public bool Closing
        {
            get { return (fClosedByMe || fClosedByPeer) && !Closed; }
        }

        /// <summary>
        /// WebSocket connection cookies
        /// 
        /// Property is regular unparsed Cookie header string
        /// e.g. cookie1=value1;cookie2=value2      
        ///
        /// string "-" represents that no cookies are present
        /// </summary>
        public string Cookie
        {
            get { return fCookie; }
        }


        /// <summary>
        /// WebSocket connection extensions
        /// 
        /// PProperty is regular unparsed Sec-WebSocket-Extensions header string
        /// e.g. foo, bar; baz=2
        /// 
        /// On both client and server connection this value represents the extension(s) selected by server to be used
        /// as a result of extension negotioation
        ///
        /// string "-" represents that no extensions are present (not sent by client or missing inserver response)
        /// </summary>
        public string Extension
        {
            get { return fExtension; }
        }

        /// <summary>
        /// WebSocket connection host
        /// 
        /// Property is regular unparsed Host header string
        /// e.g. server.example.com
        /// </summary>
        public string Host
        {
            get { return fHost; }
        }

        /// <summary>
        /// object unique index
        /// </summary>
        public int Index
        {
            get { return fIndex; }
        }

        /// <summary>
        /// WebSocket connection origin
        /// 
        /// Property is regular unparsed Sec-WebSocket-Origin header string
        /// e.g. http://example.com
        /// </summary>
        public string Origin
        {
            get { return fOrigin; }
        }

        /// <summary>
        /// WebSocket connection port
        /// </summary>
        public string Port
        {
            get { return fPort; }
        }

        /// <summary>
        /// WebSocket connection protocol
        /// 
        /// Property is regular unparsed Sec-WebSocket-Protocol header string
        /// e.g. chat, superchat
        /// 
        /// On both client and server connection this value represents the protocol(s) selected by server to be used
        /// as a result of protocol negotioation
        /// 
        /// string "-" represents that no protocol are present (not sent by client or missing inserver response)
        /// </summary>
        public string Protocol
        {
            get { return fProtocol; }
        }

        /// <summary>
        /// Connection resource
        /// 
        /// e.g. /path1/path2/path3/file.ext
        /// </summary>
        public string ResourceName
        {
            get { return fResourceName; }
        }

        /// <summary>
        /// Whether SSL is used
        /// </summary>
        public bool Ssl
        {
            get { return fSsl; }
        }

        /// <summary>
        /// WebSocket version
        /// </summary>
        public int Version
        {
            get { return fVersion; }
        }

        #region delegates & events


        /// <summary>
        /// delegate to define connection close event
        /// </summary>
        /// <param name="aConnection">connection instance</param>
        /// <param name="aCloseCode">see WebSocketCloseReason</param>
        /// <param name="aCloseReason">close associated string data</param>
        /// <param name="aClosedByPeer">whether connection has been closed by WebSocketConnection insnce or by peer endpoint</param>
        public delegate void ConnectionCloseEvent(WebSocketConnection aConnection, int aCloseCode, string aCloseReason, bool aClosedByPeer);


        /// <summary>
        /// Connection data event (read or write)
        /// 
        /// see read and write methodts for parameters description
        /// </summary>
        /// <param name="aConnection">connection instance</param>
        /// <param name="aFinal"></param>
        /// <param name="aRes1"></param>
        /// <param name="aRes2"></param>
        /// <param name="aRes3"></param>
        /// <param name="aCode"></param>
        /// <param name="aData"></param>
        public delegate void ConnectionDataEvent(WebSocketConnection aConnection, bool aFinal, bool aRes1, bool aRes2, bool aRes3, int aCode, MemoryStream aData);

        /// <summary>
        /// Connection data event - read 
        /// connection has read full data (concanated fragmented together)
        /// 
        /// see read and write methodts for parameters description
        /// </summary>
        /// <param name="aConnection">connection instance</param>
        /// <param name="aCode"></param>
        /// <param name="aData"></param>
        public delegate void ConnectionDataEventFull(WebSocketConnection aConnection, int aCode, MemoryStream aData);


        /// <summary>
        /// Basic connection event
        /// </summary>
        /// <param name="aConnection">connection instance</param>
        public delegate void ConnectionEvent(WebSocketConnection aConnection);



        /// <summary>
        /// connection close event
        /// </summary>
        public event ConnectionCloseEvent ConnectionClose;

        /// <summary>
        /// Connection has been successfully opened
        /// </summary>
        public event ConnectionEvent ConnectionOpen;

        /// <summary>
        /// Connection has read data
        /// </summary>
        public event ConnectionDataEvent ConnectionRead;

        /// <summary>
        /// Connection has read full data
        /// </summary>
        public event ConnectionDataEventFull ConnectionReadFull;

        /// <summary>
        /// Connection has written data
        /// </summary>
        public event ConnectionDataEvent ConnectionWrite;

        #endregion delegates & events

        #endregion public properties


        public WebSocketConnection()
        {

        }

        public WebSocketConnection(TcpClient aClient)
        {
            fClient = aClient;
        }

        /// <summary>
        /// close connection 
        /// </summary>
        /// <param name="aCloseCode">WebSocketCloseCode constant reason</param>
        /// <param name="aCloseReason">textual data (max 123 bytes)</param>
        public virtual void Close(int aCloseCode, string aCloseReason)
        {
            byte[] bytes;
            MemoryStream ms = new MemoryStream();
            string s = aCloseReason;
            if (!Closed)
            {
                fClosedByMe = true;
                if (!fClosedByPeer)
                {
                    bytes = new byte[2];
                    bytes[0] = (byte)((int)aCloseCode / 256);
                    bytes[1] = (byte)((int)aCloseCode % 256);
                    ms.Write(bytes, 0, 2);
                    bytes = Encoding.UTF8.GetBytes(s);
                    while (bytes.Length > 123)
                    {
                        s = s.Substring(0, s.Length - 1);
                        bytes = Encoding.UTF8.GetBytes(s);
                    }
                    ms.Write(bytes, 0, bytes.Length);
                    SendData(true, false, false, false, WebSocketFrame.Close, ms);
                }
                Close();
                ProcessClose(aCloseCode, aCloseReason, false);
                if (ConnectionClose != null) ConnectionClose(this, aCloseCode, aCloseReason, false);
            }
        }
        public virtual void Close(int aCloseCode)
        {
            Close(aCloseCode, String.Empty);
        }


        /// <summary>
        /// send ping
        /// </summary>
        /// <param name="aData">string data</param>
        /// <returns>true if sending was successful</returns>
        public bool Ping(string aData)
        {
            return SendData(true, false, false, false, WebSocketFrame.Ping, aData);
        }

        /// <summary>
        /// send pong
        /// </summary>
        /// <param name="aData">string data</param>
        /// <returns>true if sending was successful</returns>
        public bool Pong(string aData)
        {
            return SendData(true, false, false, false, WebSocketFrame.Pong, aData);
        }

        /// <summary>
        /// Send binary data
        /// </summary>
        /// <param name="aStream">binary data</param>
        /// <param name="aWriteFinal">whether frame is final</param>
        /// <param name="aRes1">extensions 1st bit</param>
        /// <param name="aRes2">extensions 2nd bit</param>
        /// <param name="aRes3">extensions 3nd bit</param>
        /// <returns>true if sending was successful</returns>
        public bool SendBinary(MemoryStream aStream, bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3)
        {
            return SendData(aWriteFinal, aRes1, aRes2, aRes3, WebSocketFrame.Binary, aStream);
        }
        public bool SendBinary(MemoryStream aStream, bool aWriteFinal, bool aRes1, bool aRes2)
        {
            return SendBinary(aStream, aWriteFinal, aRes1, aRes2, false);
        }
        public bool SendBinary(MemoryStream aStream, bool aWriteFinal, bool aRes1)
        {
            return SendBinary(aStream, aWriteFinal, aRes1, false);
        }
        public bool SendBinary(MemoryStream aStream, bool aWriteFinal)
        {
            return SendBinary(aStream, aWriteFinal, false);
        }
        public bool SendBinary(MemoryStream aStream)
        {
            return SendBinary(aStream, true);
        }

        /// <summary>
        /// send binary continuation data
        /// 
        /// see SendBinary for parameter and result response
        /// </summary>
        /// <param name="aStream">binary data</param>
        /// <param name="aWriteFinal">whether frame is final</param>
        /// <param name="aRes1">extensions 1st bit</param>
        /// <param name="aRes2">extensions 2nd bit</param>
        /// <param name="aRes3">extensions 3nd bit</param>
        /// <returns>true if sending was successful</returns>
        public bool SendBinaryContinuation(MemoryStream aStream, bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3)
        {
            return SendData(aWriteFinal, aRes1, aRes2, aRes3, WebSocketFrame.Continuation, aStream);
        }
        public bool SendBinaryContinuation(MemoryStream aStream, bool aWriteFinal, bool aRes1, bool aRes2)
        {
            return SendBinaryContinuation(aStream, aWriteFinal, aRes1, aRes2, false);
        }
        public bool SendBinaryContinuation(MemoryStream aStream, bool aWriteFinal, bool aRes1)
        {
            return SendBinaryContinuation(aStream, aWriteFinal, aRes1, false);
        }
        public bool SendBinaryContinuation(MemoryStream aStream, bool aWriteFinal)
        {
            return SendBinaryContinuation(aStream, aWriteFinal, false);
        }
        public bool SendBinaryContinuation(MemoryStream aStream)
        {
            return SendBinaryContinuation(aStream, true);
        }

        /// <summary>
        /// Send textual data
        /// </summary>
        /// <param name="aString">string data</param>
        /// <param name="aWriteFinal">whether frame is final</param>
        /// <param name="aRes1">extensions 1st bit</param>
        /// <param name="aRes2">extensions 2nd bit</param>
        /// <param name="aRes3">extensions 3nd bit</param>
        /// <returns>true if sending was successful</returns>
        public bool SendText(String aString, bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3)
        {
            return SendData(aWriteFinal, aRes1, aRes2, aRes3, WebSocketFrame.Text, aString);
        }
        public bool SendText(String aString, bool aWriteFinal, bool aRes1, bool aRes2)
        {
            return SendText(aString, aWriteFinal, aRes1, aRes2, false);
        }
        public bool SendText(String aString, bool aWriteFinal, bool aRes1)
        {
            return SendText(aString, aWriteFinal, aRes1, false);
        }
        public bool SendText(String aString, bool aWriteFinal)
        {
            return SendText(aString, aWriteFinal, false);
        }
        public bool SendText(String aString)
        {
            return SendText(aString, true);
        }


        /// <summary>
        /// Send textual continuation data
        /// </summary>
        /// <param name="aString">string data</param>
        /// <param name="aWriteFinal">whether frame is final</param>
        /// <param name="aRes1">extensions 1st bit</param>
        /// <param name="aRes2">extensions 2nd bit</param>
        /// <param name="aRes3">extensions 3nd bit</param>
        /// <returns>true if sending was successful</returns>
        public bool SendTextContinuation(String aString, bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3)
        {
            return SendData(aWriteFinal, aRes1, aRes2, aRes3, WebSocketFrame.Continuation, aString);
        }
        public bool SendTextContinuation(String aString, bool aWriteFinal, bool aRes1, bool aRes2)
        {
            return SendTextContinuation(aString, aWriteFinal, aRes1, aRes2, false);
        }
        public bool SendTextContinuation(String aString, bool aWriteFinal, bool aRes1)
        {
            return SendTextContinuation(aString, aWriteFinal, aRes1, false);
        }
        public bool SendTextContinuation(String aString, bool aWriteFinal)
        {
            return SendTextContinuation(aString, aWriteFinal, false);
        }
        public bool SendTextContinuation(String aString)
        {
            return SendTextContinuation(aString, true);
        }







        protected virtual void Close()
        {
            if (fClient.Connected)
            {

                fClient.Close();

            }
        }


        protected virtual void ProcessClose(int aCloseCode, string aCloseReason, bool aClosedByPeer)
        {

        }

        protected virtual void ProcessData(ref bool aReadFinal, ref bool aRes1, ref bool aRes2, ref bool aRes3, ref int aReadCode, MemoryStream aStream)
        {
        }

        protected virtual void ProcessPing(string aData)
        {
        }

        protected virtual void ProcessPong(string aData)
        {
        }

        protected virtual void ProcessStream(bool aReadFinal, bool aRes1, bool aRes2, bool aRes3, MemoryStream aStream)
        {
        }

        protected virtual void ProcessStreamContinuation(bool aReadFinal, bool aRes1, bool aRes2, bool aRes3, MemoryStream aStream)
        {
        }

        protected virtual void ProcessStreamFull(MemoryStream aStream)
        {
        }

        protected virtual void ProcessText(bool aReadFinal, bool aRes1, bool aRes2, bool aRes3, string aString)
        {
        }

        protected virtual void ProcessTextContinuation(bool aReadFinal, bool aRes1, bool aRes2, bool aRes3, string aString)
        {
        }

        protected virtual void ProcessTextFull(string aString)
        {
        }


        protected static byte[] ReverseBytes(byte[] inArray)
        {
            byte temp;
            int highCtr = inArray.Length - 1;

            for (int ctr = 0; ctr < inArray.Length / 2; ctr++)
            {
                temp = inArray[ctr];
                inArray[ctr] = inArray[highCtr];
                inArray[highCtr] = temp;
                highCtr -= 1;
            }
            return inArray;
        }

        private Object writeLock = new Object();

        public virtual bool SendData(bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3, int aWriteCode, MemoryStream aStream)
        {
            bool result = !Closed && ((aWriteCode == WebSocketFrame.Close) || !fClosedByMe);
            int bt = 0;
            int sendLen = 0;
            int i;
            long len = 0;
            Stream stream;
            byte[] bytes;
            byte[] masks = new byte[4];
            byte[] send = new byte[65536];
            Random rand = new Random();
            if (result)
            {
                lock (writeLock)
                {
                    try
                    {
                        stream = getStream(fClient);

                        //send basics
                        bt = (aWriteFinal ? 1 : 0) * 0x80;
                        bt += (aRes1 ? 1 : 0) * 0x40;
                        bt += (aRes2 ? 1 : 0) * 0x20;
                        bt += (aRes3 ? 1 : 0) * 0x10;
                        bt += aWriteCode;
                        stream.WriteByte((byte)bt);

                        //length & mask
                        len = (fMasking ? 1 : 0) * 0x80;
                        if (aStream.Length < 126) len += aStream.Length;
                        else if (aStream.Length < 65536) len += 126;
                        else len += 127;
                        stream.WriteByte((byte)len);

                        if (aStream.Length >= 126)
                        {
                            if (aStream.Length < 65536)
                            {
                                bytes = System.BitConverter.GetBytes((ushort)aStream.Length);
                            }
                            else
                            {
                                bytes = System.BitConverter.GetBytes((ulong)aStream.Length);
                            }
                            if (BitConverter.IsLittleEndian) bytes = ReverseBytes(bytes);
                            stream.Write(bytes, 0, bytes.Length);
                        }

                        //masking
                        if (fMasking)
                        {
                            masks[0] = (byte)rand.Next(256);
                            masks[1] = (byte)rand.Next(256);
                            masks[2] = (byte)rand.Next(256);
                            masks[3] = (byte)rand.Next(256);
                            stream.Write(masks, 0, masks.Length);
                        }


                        //send data
                        aStream.Position = 0;
                        while ((sendLen = aStream.Read(send, 0, send.Length)) > 0)
                        {
                            if (fMasking)
                            {
                                for (i = 0; i < send.Length; i++)
                                {
                                    send[i] = (byte)(send[i] ^ masks[i % 4]);
                                }
                            }
                            stream.Write(send, 0, sendLen);
                        }
                        aStream.Position = 0;
                        if (ConnectionWrite != null)
                            ConnectionWrite(this, aWriteFinal, aRes1, aRes2, aRes3, aWriteCode, aStream);
                    }
                    catch
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        public virtual bool SendData(bool aWriteFinal, bool aRes1, bool aRes2, bool aRes3, int aWriteCode, String aData)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(aData);
            sw.Flush();
            return SendData(aWriteFinal, aRes1, aRes2, aRes3, aWriteCode, ms);
        }



        protected void Execute()
        {
            bool final, res1, res2, res3;
            int code, closeCode;
            byte[] closeReasonB;
            string closeReason;
            bool readRes;
            int lastCode = -1;
            int lastCode2 = -1;
            int errorCode = -1;
            MemoryStream messageMemoryStream = new MemoryStream();
            MemoryStream fullMessageMemoryStream = new MemoryStream();
            if (ConnectionOpen != null) ConnectionOpen(this);
            //fFullDataProcess
            while (true)
            {
                readRes = ReadData(out final, out res1, out res2, out res3, out code, messageMemoryStream);
                if (readRes)
                {
                    messageMemoryStream.Position = 0;
                    ProcessData(ref final, ref res1, ref res2, ref res3, ref code, messageMemoryStream);
                    messageMemoryStream.Position = 0;
                    errorCode = -1;

                    // reset the full memory stream if its a new frame
                    if ((code == WebSocketFrame.Text) || (code == WebSocketFrame.Binary))
                    {
                        fullMessageMemoryStream.Position = 0;
                        fullMessageMemoryStream.SetLength(0);
                    }
                    if ((code == WebSocketFrame.Continuation) || (code == WebSocketFrame.Text) || (code == WebSocketFrame.Binary))
                    {
                        messageMemoryStream.Position = 0;
                        fullMessageMemoryStream.Write(messageMemoryStream.GetBuffer(), 0, (int)messageMemoryStream.Length);
                        messageMemoryStream.Position = 0;
                    }
                    switch (code)
                    {
                        case WebSocketFrame.Continuation:
                            if (lastCode == WebSocketFrame.Text)
                            {
                                ProcessTextContinuation(final, res1, res2, res3, Encoding.UTF8.GetString(messageMemoryStream.ToArray()));
                            }
                            else if (lastCode == WebSocketFrame.Binary)
                            {
                                ProcessStreamContinuation(final, res1, res2, res3, messageMemoryStream);
                            }
                            else
                            {
                                errorCode = WebSocketCloseCode.ProtocolError;
                            }
                            if (final) lastCode = -1;
                            break;
                        case WebSocketFrame.Text:
                            ProcessText(final, res1, res2, res3, Encoding.UTF8.GetString(messageMemoryStream.ToArray()));
                            if (!final) lastCode = code;
                            else lastCode = -1;
                            lastCode2 = code;
                            break;
                        case WebSocketFrame.Binary:
                            ProcessStream(final, res1, res2, res3, messageMemoryStream);
                            if (!final) lastCode = code;
                            else lastCode = -1;
                            lastCode2 = code;
                            break;
                        case WebSocketFrame.Ping:
                            ProcessPing(Encoding.UTF8.GetString(messageMemoryStream.ToArray()));
                            break;
                        case WebSocketFrame.Pong:
                            ProcessPong(Encoding.UTF8.GetString(messageMemoryStream.ToArray()));
                            break;
                        case WebSocketFrame.Close:
                            closeCode = WebSocketCloseCode.NoStatus;
                            closeReason = String.Empty;
                            if (messageMemoryStream.Length > 1)
                            {
                                closeCode = messageMemoryStream.ReadByte() * 256 + messageMemoryStream.ReadByte();
                                if (messageMemoryStream.Length > 2)
                                {
                                    closeReasonB = new byte[messageMemoryStream.Length - 2];
                                    messageMemoryStream.Read(closeReasonB, 0, closeReasonB.Length);
                                    closeReason = Encoding.UTF8.GetString(closeReasonB);
                                }
                            }
                            fClosedByPeer = true;
                            ProcessClose(closeCode, closeReason, true);
                            if (ConnectionClose != null) ConnectionClose(this, closeCode, closeReason, true);
                            if ((closeCode == WebSocketCloseCode.Normal) && (!fClosedByMe))
                            {
                                Close(WebSocketCloseCode.Normal);
                            }
                            else
                            {
                                Close();
                            }
                            break;
                        default:
                            errorCode = WebSocketCloseCode.DataError;
                            break;
                    }
                    if (errorCode == -1)
                    {
                        messageMemoryStream.Position = 0;
                        if (ConnectionRead != null) ConnectionRead(this, final, res1, res2, res3, code, messageMemoryStream);
                    }
                    else
                    {
                        break;
                    }

                    if (((code == WebSocketFrame.Continuation) || (code == WebSocketFrame.Text) || (code == WebSocketFrame.Binary)) && fFullDataProcess && final)
                    {
                        fullMessageMemoryStream.Position = 0;
                        if (lastCode2 == WebSocketFrame.Text)
                            ProcessTextFull(Encoding.UTF8.GetString(fullMessageMemoryStream.ToArray()));
                        else if (lastCode2 == WebSocketFrame.Binary)
                            ProcessStreamFull(fullMessageMemoryStream);
                        if (ConnectionReadFull != null) ConnectionReadFull(this, lastCode2, fullMessageMemoryStream);
                    }

                }
                else
                {
                    errorCode = WebSocketCloseCode.DataError;
                    break;
                }
            }
            if (errorCode != -1)
            {
                Close(errorCode, String.Empty);
            }
            fullMessageMemoryStream.Dispose();
            messageMemoryStream.Dispose();
        }


        protected bool ReadByte(Stream aStream, out int aByte)
        {
            aByte = aStream.ReadByte();
            return aByte > -1;
        }

        protected bool ReadData(out bool aReadFinal, out bool aRes1, out bool aRes2, out bool aRes3, out int aReadCode, MemoryStream aStream)
        {
            bool result = true;
            bool mask = false;
            int bt, j, k;
            long len, i;
            int[] masks = new int[4];
            byte[] buffer;
            Stream ns;

            aReadFinal = false;
            aRes1 = false;
            aRes2 = false;
            aRes3 = false;
            aReadCode = -1;
            result = !Closed;
            if (result)
            {

                ns = getStream(fClient);





                ns.ReadTimeout = Timeout.Infinite;
                try
                {
                    result = ReadByte(ns, out bt);
                    ns.ReadTimeout = 10 * 1000;
                    if (result)
                    {
                        //basics
                        aReadFinal = (bt & 0x80) == 0x80;
                        aRes1 = (bt & 0x40) == 0x40;
                        aRes2 = (bt & 0x20) == 0x20;
                        aRes3 = (bt & 0x10) == 0x10;
                        aReadCode = (bt & 0x0f);

                        //mask & length
                        result = ReadByte(ns, out bt);
                        if (result)
                        {
                            mask = (bt & 0x80) == 0x80;
                            len = (bt & 0x7F);
                            if (len == 126)
                            {
                                result = ReadByte(ns, out bt);
                                if (result)
                                {
                                    len = bt * 0x100;
                                    result = ReadByte(ns, out bt);
                                    if (result)
                                    {
                                        len = len + bt;
                                    }
                                }
                            }
                            else if (len == 127)
                            {
                                result = ReadByte(ns, out bt);
                                if (result)
                                {
                                    len = bt * 0x100000000000000;
                                    result = ReadByte(ns, out bt);
                                    if (result)
                                    {
                                        len = len + bt * 0x1000000000000;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt * 0x10000000000;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt * 0x100000000;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt * 0x1000000;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt * 0x10000;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt * 0x100;
                                        result = ReadByte(ns, out bt);
                                    }
                                    if (result)
                                    {
                                        len = len + bt;
                                    }
                                }
                            }

                            if ((result) && (fRequireMask) && (!mask))
                            {
                                Close(WebSocketCloseCode.ProtocolError);
                                result = false;
                            }

                            //read mask
                            if (result && mask)
                            {
                                result = ReadByte(ns, out masks[0]);
                                if (result) result = ReadByte(ns, out masks[1]);
                                if (result) result = ReadByte(ns, out masks[2]);
                                if (result) result = ReadByte(ns, out masks[3]);
                            }

                            if (result)
                            {
                                aStream.SetLength(0);
                                aStream.Position = 0;
                                ns.ReadTimeout = 1000 * 60 * 60 * 2;
                                buffer = new byte[len];
                                j = 0;
                                while (len > 0)
                                {
                                    k = ns.Read(buffer, j, (int)Math.Min(len, (long)System.Int32.MaxValue));
                                    j += k;
                                    len -= k;
                                }
                                if (mask)
                                {
                                    for (i = 0; i < buffer.Length; i++)
                                    {
                                        buffer[i] = (byte)(buffer[i] ^ masks[i % 4]);
                                    }
                                }
                                aStream.Write(buffer, 0, buffer.Length);
                                aStream.Position = 0;
                            }
                        }
                    }
                }
                catch
                {
                    result = false;
                }
            }

            return result;
        }

        protected internal void StartRead()
        {
            var t = new Thread(Execute);
            t.Name = "WSReadThread[" + t.ManagedThreadId + "]";
            t.Start();
        }

        protected Stream getStream(TcpClient aClient)
        {
            if (fSsl)
            {
                return fSslStream;
            }
            else
            {
                return aClient.GetStream();
            }
        }

    }


    /// <summary>
    /// basic WebSocket server connection 
    /// 
    /// object is created by WebSocketServer automatically
    /// </summary>
    public class WebSocketServerConnection : WebSocketConnection
    {

        #region protected properties
        WebSocketServer fParent;
        #endregion

        #region public properties
        /// <summary>
        /// hash list of unparsed headers
        /// </summary>
        public WebSocketHeaders Headers
        {
            get { return fHeaders; }
        }
        #endregion public properties


        public WebSocketServerConnection(TcpClient aClient, WebSocketServer aParent)
            : base(aClient)
        {
            fRequireMask = true;
            fParent = aParent;
        }

        protected override void Close()
        {
            if (fParent != null)
            {
                fParent.SafeRemoveConnection(this);
                fParent = null;
                base.Close();
            }
        }



    }

    /// <summary>
    /// basic WebSocket client connection 
    /// </summary>
    public class WebSocketClientConnection : WebSocketConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketClientConnection));
        public WebSocketClientConnection()
        {
            fMasking = true;
        }

        /// <summary>
        /// start client connection
        /// </summary>
        /// <param name="aHost"></param>
        /// <param name="aPort"></param>
        /// <param name="aResourceName"></param>
        /// <param name="aSsl"></param>
        /// <param name="aOrigin"></param>
        /// <param name="aProtocol"></param>
        /// <param name="aExtension"></param>
        /// <param name="aCookie"></param>
        /// <param name="aVersion"></param>
        /// <returns></returns>
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl,
          string aOrigin, string aProtocol, string aExtension,
          string aCookie, int aVersion)
        {

            fHost = aHost;
            fPort = aPort;
            fResourceName = aResourceName;
            fSsl = aSsl;
            fOrigin = aOrigin;
            fProtocol = aProtocol;
            fExtension = aExtension;
            fCookie = aCookie;
            fVersion = aVersion;
            fHeaders = new WebSocketHeaders();


            try
            {
                // try connect directly
                try
                {
                    fClient = new TcpClient();
                    IAsyncResult result = fClient.BeginConnect(aHost, int.Parse(aPort), null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(30000);
                    if (!success || !fClient.Connected)
                    {
                        fClient.Close();
                        throw new SocketException();
                    }
                    else
                    {
                        Logger.Debug("Connected directly to " + aHost + ":" + aPort);
                    }
                }
                catch (SocketException e)
                {
                    Logger.Warn("Failed to connect to host, attempting to find a proxy");
                    var proxy = WebRequest.DefaultWebProxy.GetProxy(new Uri("https://" + aHost + ":" + aPort));
                    if (proxy != null && !aHost.Equals(proxy.Host))
                    {
                        Logger.Debug("Found a web proxy at " + proxy);
                        fClient = new TcpClient(proxy.Host, proxy.Port);
                        StreamReader proxyReader = new StreamReader(fClient.GetStream());
                        StreamWriter proxyWriter = new StreamWriter(fClient.GetStream());
                        proxyWriter.WriteLine("CONNECT " + aHost + ":" + aPort + " HTTP/1.0");
                        proxyWriter.WriteLine("");
                        proxyWriter.Flush();
                        String readerOut = proxyReader.ReadLine();
                        if (!String.IsNullOrEmpty(readerOut) && readerOut.StartsWith("HTTP/1.0 200"))
                        {
                            // Now clear other data from it until we get a blank line
                            while (!String.IsNullOrEmpty(readerOut))
                            {
                                readerOut = proxyReader.ReadLine();
                            }
                            // now we are ready to proceed normally through the proxied tunnel
                        }
                        else
                        {
                            Logger.Error("Failed to connect via proxy, proxy responded with " + readerOut);
                            throw e;
                        }
                    }
                    else
                    {
                        Logger.Error("No system proxy setup, cannot continue");
                        throw e;
                    }
                }

                fClient.ReceiveTimeout = 300000; // if we don't read anything for 5 minutes

                if (fSsl)
                {
                    fSslStream = new SslStream(fClient.GetStream(), false, new RemoteCertificateValidationCallback(validateServerCertificate), null);
                    fSslStream.AuthenticateAsClient(fHost);
                }

                Stream stream = getStream(fClient);
                StreamReader sr = new StreamReader(stream);
                StreamWriter sw = new StreamWriter(stream);
                string key = "";
                Random rand = new Random();
                String get;
                String line;
                Char[] separator = new char[] { ':' };
                Char[] separator2 = new char[] { ' ' };
                string[] parts;
                SHA1 sha = new SHA1CryptoServiceProvider();
                /*
        
                string cookie = "-";
                string extension = "-";
                string origin = "-";
                string protocol = "-";
                */

                sw.Write(String.Format("GET {0} HTTP/1.1\r\n", fResourceName));
                sw.Write(String.Format("Upgrade: websocket\r\n"));
                sw.Write(String.Format("Connection: Upgrade\r\n"));
                sw.Write(String.Format("Host: {0}:{1}\r\n", fHost, fPort));
                while (key.Length < 16) key += (char)(rand.Next(85) + 32);
                key = Convert.ToBase64String(Encoding.ASCII.GetBytes(key));
                sw.Write(String.Format("Sec-WebSocket-Key: {0}\r\n", key));
                sw.Write(String.Format("Sec-WebSocket-Version: {0}\r\n", fVersion));
                if (fProtocol != "-")
                    sw.Write(String.Format("Sec-WebSocket-Protocol: {0}\r\n", fProtocol));
                if (fOrigin != "-")
                {
                    if (fVersion < 13)
                        sw.Write(String.Format("Sec-WebSocket-Origin: {0}\r\n", fOrigin));
                    else
                        sw.Write(String.Format("Origin: {0}\r\n", fOrigin));
                }
                if (fExtension != "-")
                    sw.Write(String.Format("Sec-WebSocket-Extensions: {0}\r\n", fExtension));
                if (fCookie != "-")
                    sw.Write(String.Format("Cookie: {0}\r\n", fCookie));
                sw.Write("\r\n");
                sw.Flush();


                get = sr.ReadLine();
                if (get.ToLower().IndexOf("http/1.1 101") > -1)
                {
                    do
                    {
                        line = sr.ReadLine();
                        if (!String.IsNullOrEmpty(line))
                        {
                            parts = line.Split(separator, 2);
                            fHeaders.Append(parts[0].ToLower(), parts.Length == 2 ? parts[1] : "");
                        }
                    } while (!String.IsNullOrEmpty(line));

                    if (
                        (fHeaders.Contains("upgrade")) &&
                        (fHeaders["upgrade"].Trim().ToLower() == "websocket".ToLower()) &&
                        (fHeaders.Contains("connection")) &&
                        (fHeaders["connection"].Trim().ToLower().IndexOf("upgrade") > -1))
                    {
                        fProtocol = "-";
                        fExtension = "-";
                        if (fHeaders.Contains("sec-websocket-protocol")) fProtocol = fHeaders["sec-websocket-protocol"].Trim();
                        if (fHeaders.Contains("sec-websocket-extensions")) fExtension = fHeaders["sec-websocket-extensions"].Trim();
                        if (fHeaders.Contains("sec-websocket-accept"))
                        {
                            get = fHeaders["sec-websocket-accept"].Trim();
                            key = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                            if (get == key)
                            {
                                fHandshake = true;
                                StartRead();
                                Logger.Debug("Upgrade succceeded, connection complete.");
                                return true;
                            }
                        }
                    }
                    else
                    {
                        Logger.Error("Failed to upgrade connection, server did not return correct headers [" + fHeaders.ToHeaders() + "]");
                        throw new Exception("Failed to upgrade connection");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to server", e);
                throw e;
            }
            try { fClient.Close(); }
            catch (Exception e) { Logger.Warn("Error closing connection : " + e.Message); }
            return false;
        }
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl, string aOrigin, string aProtocol, string aExtension, string aCookie)
        {
            return Start(aHost, aPort, aResourceName, aSsl, aOrigin, aProtocol, aExtension, aCookie, 13);
        }
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl, string aOrigin, string aProtocol, string aExtension)
        {
            return Start(aHost, aPort, aResourceName, aSsl, aOrigin, aProtocol, aExtension, "-", 13);
        }
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl, string aOrigin, string aProtocol)
        {
            return Start(aHost, aPort, aResourceName, aSsl, aOrigin, aProtocol, "-", "-", 13);
        }
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl, string aOrigin)
        {
            return Start(aHost, aPort, aResourceName, aSsl, aOrigin, "-", "-", "-", 13);
        }
        public bool Start(string aHost, string aPort, string aResourceName, bool aSsl)
        {
            return Start(aHost, aPort, aResourceName, aSsl, "-", "-", "-", "-", 13);
        }
        public bool Start(string aHost, string aPort, string aResourceName)
        {
            return Start(aHost, aPort, aResourceName, false, "-", "-", "-", "-", 13);
        }


        protected virtual bool validateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //if (sslPolicyErrors == SslPolicyErrors.None)
            return true;
            /*
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
             * */
        }

    }



    /// <summary>
    /// WebSocket server
    /// </summary>
    public class WebSocketServer
    {
        #region protected properties
        protected IPAddress fAddress;
        protected int fPort;
        protected volatile bool fTerminated;
        protected TcpListener listener;
        protected bool fIsRunning = false;
        protected int fIndex = WebSocketIndexer.GetIndex();
        protected List<WebSocketServerConnection> fConnections = new List<WebSocketServerConnection>();
        protected Object connLock = new Object();
        protected bool fSsl = false;
        protected string fSslCertificate = String.Empty;
        #endregion protected properties



        #region public properties

        /// <summary>
        /// number of connection
        /// </summary>
        public int ConnectionCount
        {
            get { return fConnections.Count; }
        }



        /// <summary>
        /// object index
        /// </summary>
        public int Index
        {
            get
            {
                return fIndex;
            }
        }

        /// <summary>
        /// whether server is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return fIsRunning;
            }
        }

        /// <summary>
        /// whether SSL should be used
        /// </summary>
        public bool Ssl
        {
            get { return fSsl; }
            set { if (!IsRunning) fSsl = value; }
        }

        /// <summary>
        /// SSL certificate file
        /// </summary>
        public string SslCertificate
        {
            get { return fSslCertificate; }
            set { if (!IsRunning) fSslCertificate = value; }
        }


        #region public delegates & events

        /// <summary>
        /// Server accepted new connection
        /// </summary>
        /// <param name="aServer">WebSocketServer instance</param>
        /// <param name="aConnection">new WebSocketServerConnection instance, connection MUST NOT be managed in this event, it's only information event</param>
        /// <returns> true if connection should be accepted, false otherwise</returns>
        public delegate void ConnectionAcceptedEvent(WebSocketServer aServer, WebSocketServerConnection aConnection, ref bool aCanAdd);

        /// <summary>
        /// Common connection event delegate
        /// </summary>
        /// <param name="aServer">WebSocketServer instance</param>
        /// <param name="aConnection">WebSocketServerConnection instance, connection MUST NOT be managed in this event, it's only information event</param>
        public delegate void ConnectionEvent(WebSocketServer aServer, WebSocketServerConnection aConnection);


        /// <summary>
        /// Socket exception event handler definition
        /// </summary>
        /// <param name="aServer">WebSocketServer instance</param>
        /// <param name="aException">Socket exception</param>
        public delegate void SocketErrorEvent(WebSocketServer aServer, SocketException aException);




        /// <summary>
        /// Event called if new connection is accepted (after connection is created with properties populated and connection is added to connection list)
        /// </summary>
        public event ConnectionEvent AfterAddConnection;

        /// <summary>
        /// Event called after connection is removed from connection list 
        /// </summary>
        public event ConnectionEvent AfterRemoveConnection;

        /// <summary>
        /// Event called if new connection is accepted (after connection is created with properties populated, but before connection is added to connection list)
        /// </summary>
        public event ConnectionAcceptedEvent BeforeAddConnection;

        /// <summary>
        /// Event called before connection is removed from connection list
        /// </summary>
        public event ConnectionEvent BeforeRemoveConnection;


        /// <summary>
        /// Event for socket receiving connections exception
        /// </summary>
        public event SocketErrorEvent SocketError;

        #endregion public delegates & events


        //public delegate void LogEvent(string aData);
        //public event LogEvent Log;
        #endregion public properties



        /// <summary>
        /// constructor function
        /// </summary>
        public WebSocketServer()
        {

        }

        /// <summary>
        /// close all server connections
        /// </summary>
        /// <param name="aCloseCode">WebSocketCloseCode constant reason</param>
        /// <param name="aCloseReason">textual data (max 123 bytes)</param>
        public void CloseAllConnection(int aCloseCode, string aCloseReason)
        {
            LockConnections();
            WebSocketServerConnection c;
            for (int i = fConnections.Count - 1; i >= 0; i--)
            {
                c = fConnections[i];
                if (!c.Closing && !c.Closed)
                    c.Close(aCloseCode, aCloseReason);
            }
            UnlockConnections();
        }



        /// <summary>
        /// return connection by its position in list
        /// </summary>
        /// <param name="index">position in list</param>
        /// <returns>WebSocketServerConnection</returns>
        public WebSocketServerConnection GetConnection(int index)
        {
            return fConnections[index];
        }

        /// <summary>
        /// return connection by its position Index property
        /// </summary>
        /// <param name="index">connection's Index ptoperty</param>
        /// <returns>WebSocketServerConnection</returns>
        public WebSocketServerConnection GetConnectionByIndex(int index)
        {
            LockConnections();
            WebSocketServerConnection result = null;
            int i;
            int j = fConnections.Count;
            for (i = 0; i < j; i++)
            {
                if (fConnections[i].Index == index)
                {
                    result = fConnections[i];
                    break;
                }
            }
            UnlockConnections();
            return result;
        }


        /// <summary>
        /// Function to create WebSocketServerConnection instance.
        /// Function should return null if connection should not be accepted or some non 101 HTTP result code (see below) 
        /// 
        /// if application implementation needs to use it's own inplementation of WebSocketServerConnection (based on WebSocketServerConnection class)
        /// application should create new WebSocketServer (based on this one) and override this method to return its own instance of connection based on either
        /// application logic or passed parameters
        /// 
        /// All parameters, except for aClient and aHttpCode refers to properties in WebSocketConnection, check the documentation of WebSocketConnection
        /// Function does not have to set up phis variables to result object properties, it's done automatically by server after object is returned.
        /// 
        /// </summary>
        /// <param name="aClient">TcpClient requesting connection, client MUST NOT be closed by this function</param>
        /// <param name="aHeaders"></param>
        /// <param name="aHost"></param>
        /// <param name="aPort"></param>
        /// <param name="aResourceName"></param>
        /// <param name="aOrigin">If "-" passed, no origin was passed by client</param>
        /// <param name="aCookie">If "-" passed, no cookies was passed by client</param>
        /// <param name="aVersion"></param>
        /// <param name="aProtocol">In value is list of protocol requested by client ("-" if non requested). Out values should be list of protocol supported by server based on client request, if "-" is returned, no protocol will be negotitated</param>
        /// <param name="aExtension">In value is list of extensions requested by client ("-" if non requested). Out values should be list of extensions supported by server based on client request, if "-" is returned, no extensions will be negotitated</param>
        /// <param name="aHttpCode">HTTP result code to return, if other than 101 is returned, connection will be automatically closed. 101 is default value</param>
        /// <returns></returns>
        public virtual WebSocketServerConnection GetConnectionInstance(
          TcpClient aClient,
          WebSocketHeaders aHeaders,
          string aHost, string aPort, string aResourceName, string aOrigin, string aCookie, string aVersion,
          ref string aProtocol, ref string aExtension,
          ref int aHttpCode
        )
        {
            return new WebSocketServerConnection(aClient, this);
        }


        /// <summary>
        /// this function should be used when application needs to travers through connections
        /// 
        /// function locks the connection list for removal from different thread, no connection will be removed until 
        /// UnlockConnections is called
        /// 
        /// UnlockConnections MUST be called after traversing
        /// </summary>
        public void LockConnections()
        {
            Monitor.Enter(connLock);
        }


        /// <summary>
        /// start server
        /// <returns>true server is able to listen and has started, false if error occures or server is already running</returns>
        /// </summary>
        public bool Start(IPAddress aAddress, int aPort)
        {
            if (fIsRunning) return false;
            fTerminated = false;
            fAddress = aAddress;
            fPort = aPort;
            listener = new TcpListener(fAddress, fPort);
            try
            {
                listener.Start();
                var t = new Thread(Execute) { Name = "WebSocketThread" };
                t.Start();
                return true;
            }
            catch (SocketException e)
            {
                if (SocketError != null) SocketError(this, e);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Stop server
        /// </summary>
        public void Stop()
        {
            fTerminated = true;
        }

        /// <summary>
        /// this function should be used when application needs to travers through connections
        /// 
        /// function unlocks the connection list for removal from different thread, function should be called after
        /// LockConnections is called
        /// </summary>
        public void UnlockConnections()
        {
            Monitor.Exit(connLock);
        }













        protected void RemoveConnection(WebSocketServerConnection aConnection)
        {
            if (fConnections.IndexOf(aConnection) > -1)
            {
                if (BeforeRemoveConnection != null) BeforeRemoveConnection(this, aConnection);
                LockConnections();
                fConnections.Remove(aConnection);
                UnlockConnections();
                if (AfterRemoveConnection != null) AfterRemoveConnection(this, aConnection);
            }
        }

        protected internal void SafeRemoveConnection(WebSocketServerConnection aConnection)
        {
            if (fConnections.IndexOf(aConnection) > -1)
            {

                RemoveConnection(aConnection);

            }
        }

        /*
        protected void DoLog(string aData)
        {
          if (Log != null) Log(aData);
        }
         */

        /// <summary>
        /// basic function to add WebSocket connection to list of connections
        /// </summary>
        /// <param name="aClient">incomming connection</param>
        /// <returns>new WebSocket connection class or null</returns>
        protected WebSocketServerConnection AddConnection(TcpClient aClient, Stream aStream)
        {

            WebSocketHeaders headers = new WebSocketHeaders();
            //NetworkStream stream = aClient.GetStream();

            Stream stream = aStream == null ? aClient.GetStream() : aStream; //getStream(aClient);


            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);
            string line = String.Empty;
            string get = String.Empty;
            string[] parts;
            string cookie = "-";
            string extension = "-";
            string origin = "-";
            string protocol = "-";
            string host = "";
            string port = "";
            string resourceName = "";
            string version = "";
            Char[] separator = new char[] { ':' };
            Char[] separator2 = new char[] { ' ' };
            bool doCreate = true;
            WebSocketServerConnection result = null;
            string key = String.Empty;
            string tmpS;
            byte[] tmpB;
            int resultHTTP = 101;
            SHA1 sha = new SHA1CryptoServiceProvider();
            bool canAdd = true;
            int iversion;

            stream.ReadTimeout = 60 * 1000;
            //sr.

            try
            {
                // read resourceName
                get = sr.ReadLine();
                if (get != null)
                {
                    parts = get.Split(separator2);
                    if ((get.ToUpper().IndexOf("GET ") > -1) && (get.ToUpper().IndexOf(" HTTP/1.1") > -1) && (parts.Length >= 3))
                    {
                        parts = get.Split(separator2, 2);
                        resourceName = parts[1].Trim();
                        parts = resourceName.Split(separator2, 2);
                        resourceName = parts[0].Trim();
                    }
                }
                doCreate = resourceName != String.Empty;

                // read all headers
                if (doCreate)
                {
                    do
                    {
                        line = sr.ReadLine();
                        if (!String.IsNullOrEmpty(line))
                        {
                            parts = line.Split(separator, 2);
                            headers.Append(parts[0].ToLower(), parts.Length == 2 ? parts[1] : "");
                            //headers.Add("brona", "klucka");              
                        }

                    } while (!String.IsNullOrEmpty(line));
                    if (line == null)
                    {
                        doCreate = false;

                    }
                }

                //host & port
                if (doCreate)
                {
                    if (headers.Contains("host"))
                    {
                        parts = headers["host"].Split(separator);
                        host = parts[0].Trim();
                        if (parts.Length > 1)
                        {
                            port = parts[1].Trim();
                        }
                    }
                    doCreate = doCreate && (host != String.Empty);
                }

                //websocket key
                if (doCreate)
                {
                    if (headers.Contains("sec-websocket-key"))
                    {
                        tmpS = headers["sec-websocket-key"].Trim();
                        tmpB = Convert.FromBase64String(tmpS);
                        tmpS = Encoding.ASCII.GetString(tmpB);
                        if (tmpS.Length == 16)
                        {
                            key = headers["sec-websocket-key"].Trim();
                        }
                    }
                    doCreate = doCreate && (key != String.Empty);
                }

                //websocket version
                iversion = 0;
                if (doCreate)
                {
                    if (headers.Contains("sec-websocket-version"))
                    {
                        tmpS = headers["sec-websocket-version"].Trim();
                        if ((tmpS == "8") || (tmpS == "7") || (tmpS == "13"))
                        {
                            version = tmpS;
                            iversion = int.Parse(version);
                        }
                    }
                    doCreate = doCreate && (version != String.Empty);
                }

                //upgrade and connection
                if (doCreate)
                {
                    doCreate = doCreate &&
                              (headers.Contains("upgrade")) && (headers["upgrade"].Trim().ToLower() == "websocket".ToLower()) &&
                              (headers.Contains("connection")) && (headers["connection"].Trim().ToLower().IndexOf("upgrade") > -1);
                }

                if (doCreate)
                {
                    if (iversion < 13)
                    {
                        if (headers.Contains("sec-websocket-origin")) origin = headers["sec-websocket-origin"].Trim();
                    }
                    else
                    {
                        if (headers.Contains("origin")) origin = headers["origin"].Trim();
                    }
                    if (headers.Contains("sec-websocket-protocol")) protocol = headers["sec-websocket-protocol"].Trim();
                    if (headers.Contains("sec-websocket-extensions")) extension = headers["sec-websocket-extensions"].Trim();
                    if (headers.Contains("cookie")) cookie = headers["cookie"].Trim();
                }
            }
            catch (SocketException e)
            {
                if (SocketError != null) SocketError(this, e);
                doCreate = false;
            }
            catch (Exception)
            {
                doCreate = false;
            }
            finally
            {

            }


            result = GetConnectionInstance(aClient, headers, host, port, resourceName, origin, cookie, version, ref protocol, ref extension, ref resultHTTP);
            if (result == null)
            {
                if (resultHTTP == 101) resultHTTP = 404;
            }

            try
            {
                doCreate = doCreate && stream.CanWrite;
                if (doCreate)
                {
                    if (resultHTTP != 101)
                    {
                        sw.Write(String.Format("HTTP/1.1 {0} {1}\r\n", resultHTTP, this.httpCode(resultHTTP)));
                        sw.Write(String.Format("{0} {1}\r\n", resultHTTP, this.httpCode(resultHTTP)));
                        sw.Write("\r\n");
                        sw.Flush();
                        doCreate = false;
                    }
                }
                if (!doCreate)
                {
                    //DoLog("close");
                    aClient.Close();
                    return null;
                }
                else
                {
                    //result = new WebSocketServerConnection(aClient);
                    result.fCookie = cookie;
                    result.fExtension = extension;
                    result.fOrigin = origin;
                    result.fProtocol = protocol;
                    result.fHost = host;
                    result.fPort = port;
                    result.fResourceName = resourceName;
                    result.fVersion = int.Parse(version);
                    result.fHeaders = headers;
                    result.fSsl = fSsl;
                    result.fHandshake = true;
                    if (fSsl)
                    {
                        result.fSslStream = (SslStream)aStream;
                    }


                    if (BeforeAddConnection != null)
                    {
                        canAdd = true;
                        BeforeAddConnection(this, result, ref canAdd);
                        doCreate = canAdd;
                    }
                    if (doCreate)
                    {
                        tmpB = System.Text.Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                        key = Convert.ToBase64String(sha.ComputeHash(tmpB));

                        sw.Write("HTTP/1.1 101 Switching Protocols\r\n");
                        sw.Write("Upgrade: websocket\r\n");
                        sw.Write("Connection: Upgrade\r\n");
                        sw.Write(String.Format("Sec-WebSocket-Accept: {0}\r\n", key));
                        if (protocol != "-")
                            sw.Write(String.Format("Sec-WebSocket-Protocol: {0}\r\n", protocol));
                        if (extension != "-")
                            sw.Write(String.Format("Sec-WebSocket-Extensions: {0}\r\n", extension));
                        sw.Write("\r\n");
                        sw.Flush();
                        return result;
                    }
                    else
                    {
                        aClient.Close();
                        return null;
                    }

                }
            }
            catch (SocketException e)
            {
                if (SocketError != null) SocketError(this, e);
                aClient.Close();
                return null;
            }
            catch (Exception)
            {
                aClient.Close();
                return null;
            }
            finally
            {

            }
        }

        protected string httpCode(int aCode)
        {
            string result = "unknown code: " + aCode.ToString();
            switch (aCode)
            {
                case 100: result = "Continue"; break;
                case 101: result = "Switching Protocols"; break;
                case 200: result = "OK"; break;
                case 201: result = "Created"; break;
                case 202: result = "Accepted"; break;
                case 203: result = "Non-Authoritative Information"; break;
                case 204: result = "No Content"; break;
                case 205: result = "Reset Content"; break;
                case 206: result = "Partial Content"; break;
                case 300: result = "Multiple Choices"; break;
                case 301: result = "Moved Permanently"; break;
                case 302: result = "Found"; break;
                case 303: result = "See Other"; break;
                case 304: result = "Not Modified"; break;
                case 305: result = "Use Proxy"; break;
                case 307: result = "Temporary Redirect"; break;
                case 400: result = "Bad Request"; break;
                case 401: result = "Unauthorized"; break;
                case 402: result = "Payment Required"; break;
                case 403: result = "Forbidden"; break;
                case 404: result = "Not Found"; break;
                case 405: result = "Method Not Allowed"; break;
                case 406: result = "Not Acceptable"; break;
                case 407: result = "Proxy Authentication Required"; break;
                case 408: result = "Request Time-out"; break;
                case 409: result = "Conflict"; break;
                case 410: result = "Gone"; break;
                case 411: result = "Length Required"; break;
                case 412: result = "Precondition Failed"; break;
                case 413: result = "Request Entity Too Large"; break;
                case 414: result = "Request-URI Too Large"; break;
                case 415: result = "Unsupported Media Type"; break;
                case 416: result = "Requested range not satisfiable"; break;
                case 417: result = "Expectation Failed"; break;
                case 500: result = "Internal Server Error"; break;
                case 501: result = "Not Implemented"; break;
                case 502: result = "Bad Gateway"; break;
                case 503: result = "Service Unavailable"; break;
                case 504: result = "Gateway Time-out"; break;
            }
            return result;
        }


        /// <summary>
        /// thread function
        /// this function actually waits for incomming connections and spawns new server threads
        /// </summary>
        protected void Execute()
        {
            WebSocketServerConnection c;
            X509Certificate2 serverCertificate = null;
            TcpClient client;
            SslStream sslStream;

            if (fSsl)
            {
                serverCertificate = new X509Certificate2(fSslCertificate);
            }
            fIsRunning = true;
            while (!fTerminated)
            {
                try
                {

                    if (listener.Pending())
                    {

                        client = listener.AcceptTcpClient();
                        sslStream = null;
                        try
                        {

                            if (fSsl)
                            {
                                sslStream = new SslStream(client.GetStream(), false);
                                sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);
                            }
                        }
                        catch
                        {
                            client.Close();
                            client = null;
                        }
                        if (client != null)
                        {
                            c = AddConnection(client, sslStream);
                            if (c != null)
                            {
                                LockConnections();
                                fConnections.Add(c);
                                UnlockConnections();
                                if (AfterAddConnection != null) AfterAddConnection(this, c);
                                c.StartRead();
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(250);
                    }
                }
                catch (SocketException e)
                {
                    if (SocketError != null) SocketError(this, e);
                    break;
                }
            }
            LockConnections();
            for (int i = fConnections.Count - 1; i >= 0; i--)
            {
                c = fConnections[i];
                fConnections.Remove(c);
                c.Close(WebSocketCloseCode.Shutdown);
            }
            UnlockConnections();
            listener.Stop();
            fIsRunning = false;
        }
    }
}