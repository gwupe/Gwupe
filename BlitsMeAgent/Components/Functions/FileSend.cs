using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components.Functions
{
    class FileSend : IFunction
    {
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;
        private readonly Dictionary<String, String> _pendingFileSends = new Dictionary<string, string>();

        private static readonly ILog Logger = LogManager.GetLogger(typeof (FileSend));

        internal FileSend(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        internal void RequestFileSend(String filepath)
        {
            String filename = Path.GetFileName(filepath);
            FileSendRequestRq request = new FileSendRequestRq() { shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username, filename = filename, fileSendId = Util.getSingleton().generateString(8) };
            try
            {
                ChatElement chatElement = _engagement.Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Name + " a request to send the file " + filename);
                _appContext.ConnectionManager.Connection.RequestAsync(request, (req, res) => ProcessRequestFileSendResponse(req, res, chatElement));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for File Send : " + ex.Message, ex);
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
        }

        private void ProcessRequestFileSendResponse(Request req, Response res, ChatElement chatElement)
        {
            if (!res.isValid())
            {
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
            else
            {
                FileSendRequestRq request = (FileSendRequestRq)req;
                Logger.Debug("Successfully sent a file send request for " + request.filename + " [" + request.fileSendId + "]");
                _pendingFileSends.Add(request.fileSendId, request.filename);
            }
        }

        public void ProcessIncomingFileSendRequest(string filename, string fileSendId)
        {
            var notification = new FileSendRequestNotification()
            {
                From = _engagement.SecondParty.Username,
                Message = _engagement.SecondParty.Name + " would like to send you the file " + filename
            };
            notification.ProcessAcceptFile += ProcessAcceptFile;
            notification.ProcessDenyFile += ProcessDenyFile;
            _appContext.NotificationManager.AddNotification(notification);
            _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " offered to send you the file " + filename + ".");
        }

        private void ProcessDenyFile(object sender, FileSendEventArgs args)
        {
            Logger.Info("Denied request from " + _engagement.SecondParty.Name + " to send the file " + args.Filename);
            _engagement.Chat.LogSystemMessage("You refused to let " + _engagement.SecondParty.Name + " send you the file " + args.Filename + ".");
        }

        private void ProcessAcceptFile(object sender, FileSendEventArgs args)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Name + " to send the file " + args.Filename);
            _engagement.Chat.LogSystemMessage("You allowed " + _engagement.SecondParty.Name + " to send you the file " + args.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq() { shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username, fileSendId = args.FileSendId, accepted = true };
            try
            {
                _appContext.ConnectionManager.Connection.Request(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + args.Filename + "[" + args.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }
    }
}
