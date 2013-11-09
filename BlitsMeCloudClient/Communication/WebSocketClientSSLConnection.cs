using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Bauglir.Ex;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.API;
using log4net;

namespace BlitsMe.Cloud.Communication
{
    internal class WebSocketClientSSLConnection : WebSocketClientConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketClientSSLConnection));
        private readonly X509Certificate2 _cacert;
        private readonly IWebSocketMessageHandler _wsMessageHandler;

        public WebSocketClientSSLConnection(X509Certificate2 cacert, IWebSocketMessageHandler wsMessageHandler)
            : base()
        {
            _cacert = cacert;
            _wsMessageHandler = wsMessageHandler;
            this.FullDataProcess = true;
        }

        protected override void ProcessTextFull(string message)
        {
            _wsMessageHandler.ProcessMessage(message);
        }

        protected override bool validateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isValid = false;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                X509Chain chain0 = new X509Chain();
                chain0.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                // add all your extra certificate chain
                chain0.ChainPolicy.ExtraStore.Add(new X509Certificate2(_cacert));
                chain0.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                isValid = chain0.Build((X509Certificate2)certificate);
            }
            Logger.Debug("Checking cert valid, " + isValid);
            return isValid;
        }
    }
}