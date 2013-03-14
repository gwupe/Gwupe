using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.FileSend.Notification;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    internal delegate void FileSendRequestEvent(object sender, FileSendRequestArgs args);

    class Function : IFunction
    {
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;
        private readonly Dictionary<String, String> _pendingFileSends = new Dictionary<string, string>();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Function));

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        internal void RequestFileSend(String filepath)
        {
            String filename = Path.GetFileName(filepath);
            try
            {
                FileInfo file = new FileInfo(filepath);
                FileSendRequestRq request = new FileSendRequestRq()
                    {
                        shortCode = _engagement.SecondParty.ShortCode,
                        username = _engagement.SecondParty.Username,
                        filename = filename,
                        fileSize = file.Length,
                        fileSendId = Util.getSingleton().generateString(8)
                    };
                try
                {
                    ChatElement chatElement =
                        _engagement.Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Name +
                                                          " a request to send the file " + filename);
                    _appContext.ConnectionManager.Connection.RequestAsync(request,
                                                                          (req, res) =>
                                                                          ProcessRequestFileSendResponse(req, res,
                                                                                                         filepath));
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during request for File Send : " + ex.Message, ex);
                    _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name +
                                                      " a request to send them a file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up request for file send : " + ex.Message, ex);
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
        }

        private void ProcessRequestFileSendResponse(Request req, Response res, String filepath)
        {
            if (!res.isValid())
            {
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
            else
            {
                FileSendRequestRq request = (FileSendRequestRq)req;
                Logger.Info("Requested to send " + filepath + " to " + _engagement.SecondParty.Name);
                _pendingFileSends.Add(request.fileSendId, filepath);
            }
        }

        public void ProcessIncomingFileSendRequest(string filename, string fileSendId, long fileSize)
        {
            Logger.Info(_engagement.SecondParty.Name + " requests to send the file " + filename);
            var notification = new FileSendRequestNotification()
            {
                From = _engagement.SecondParty.Username,
                Message = _engagement.SecondParty.Name + " would like to send you the file " + filename,
                FileInfo = new FileSendInfo()
                {
                    Filename = filename,
                    FileSendId = fileSendId,
                    FileSize = fileSize
                }
            };
            notification.ProcessAcceptFile += ProcessAcceptFile;
            notification.ProcessDenyFile += ProcessDenyFile;
            _appContext.NotificationManager.AddNotification(notification);
            _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " offered to send you the file " + filename + ".");
        }

        private void ProcessDenyFile(object sender, FileSendEventArgs args)
        {
            Logger.Info("Denied request from " + _engagement.SecondParty.Name + " to send the file " + args.FileInfo.Filename);
            _engagement.Chat.LogSystemMessage("You refused to let " + _engagement.SecondParty.Name + " send you the file " + args.FileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ShortCode, 
                    username = _engagement.SecondParty.Username, 
                    fileSendId = args.FileInfo.FileSendId, 
                    accepted = false
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync(request, FileSendRequestResponseHandler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + args.FileInfo.Filename + "[" + args.FileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }

        private void FileSendRequestResponseHandler(Request request, Response response)
        {
            if(!response.isValid())
            {
                Logger.Error("Failed to send the FileSendRequestResponse for file " + ((FileSendRequestResponseRq) request).fileSendId + " : " + response.errorMessage);
            }
        }

        private void ProcessAcceptFile(object sender, FileSendEventArgs args)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Name + " to send the file " + args.FileInfo.Filename);
            _engagement.Chat.LogSystemMessage("You allowed " + _engagement.SecondParty.Name + " to send you the file " + args.FileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ShortCode, 
                    username = _engagement.SecondParty.Username, 
                    fileSendId = args.FileInfo.FileSendId,
                    accepted = true
                };
            try
            {
                FileSendListener fileReceiver = new FileSendListener(_engagement.TransportManager, args.FileInfo);
                ShowFileProgressNotification(args.FileInfo, fileReceiver);
                fileReceiver.ListenOnce();
                _appContext.ConnectionManager.Connection.RequestAsync(request, FileSendRequestResponseHandler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + args.FileInfo.Filename + "[" + args.FileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }

        private void ShowFileProgressNotification(FileSendInfo fileInfo, FileSendListener fileReceiver)
        {
            var notification = new FileSendProgressNotification()
            {
                From = _engagement.SecondParty.Username,
                ProgressText = fileInfo.Filename,
                FileInfo = fileInfo
            };
            notification.ProcessCancelFile += (sender, args) => NotificationOnProcessCancelFile(notification, sender, args);
            _appContext.NotificationManager.AddNotification(notification);
            fileReceiver.ConnectionClosed += delegate { _appContext.NotificationManager.DeleteNotification(notification); };
            fileReceiver.DataRead += delegate
                { notification.Progress = (int) ((fileReceiver.DataWriteSize*100)/fileInfo.FileSize); };
        }

        private void NotificationOnProcessCancelFile(FileSendProgressNotification notification, object sender, EventArgs eventArgs)
        {
            // cancel transfer here
            _appContext.NotificationManager.DeleteNotification(notification);
        }

        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            if (fileSendId != null && _pendingFileSends.ContainsKey(fileSendId))
            {
                String filename = _pendingFileSends[fileSendId];
                _pendingFileSends.Remove(fileSendId);
                if (accepted)
                {
                    Logger.Info("File send of file " + filename + " accepted by " + _engagement.SecondParty.Name);
                    FileSendClient client = new FileSendClient(_engagement.TransportManager);
                    // this is hacky, but lets get it to work before we make it pretty
                    client.SendFileComplete += ClientOnSendFileComplete;
                    Thread fileSendThread = new Thread(() => client.SendFile(filename, fileSendId)) { IsBackground = true };
                    fileSendThread.Start();
                }
                else
                {
                    Logger.Info("File send of file " + filename + " rejected by " + _engagement.SecondParty.Name);
                }
            }
            else
            {
                throw new Exception("Got a file send request response with an invalid id [" + fileSendId + "]");
            }
        }

        private void ClientOnSendFileComplete(object sender, EventArgs eventArgs)
        {
            
        }
    }
}
