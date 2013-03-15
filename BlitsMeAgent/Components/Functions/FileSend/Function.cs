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
        private readonly Dictionary<String, FileSendInfo> _pendingFileSends = new Dictionary<string, FileSendInfo>();

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
                FileSendInfo fileInfo = new FileSendInfo()
                    {
                        Filename = filename,
                        FileSize = new FileInfo(filepath).Length,
                        FileSendId = Util.getSingleton().generateString(8),
                        FilePath = filepath,
                        Direction = FileSendDirection.Send
                    };
                FileSendRequestRq request = new FileSendRequestRq()
                    {
                        shortCode = _engagement.SecondParty.ShortCode,
                        username = _engagement.SecondParty.Username,
                        filename = fileInfo.Filename,
                        fileSize = fileInfo.FileSize,
                        fileSendId = fileInfo.FileSendId
                    };
                try
                {
                    ChatElement chatElement =
                        _engagement.Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Name +
                                                          " a request to send the file " + filename);
                    _appContext.ConnectionManager.Connection.RequestAsync(request,
                                                                          (req, res) =>
                                                                          ProcessRequestFileSendResponse(req, res,
                                                                                                         fileInfo));
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

        private void ProcessRequestFileSendResponse(Request req, Response res, FileSendInfo fileInfo)
        {
            if (!res.isValid())
            {
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
            else
            {
                FileSendRequestRq request = (FileSendRequestRq)req;
                Logger.Info("Requested to send " + fileInfo.Filename + " to " + _engagement.SecondParty.Name);
                _pendingFileSends.Add(request.fileSendId, fileInfo);
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
                    FileSize = fileSize,
                    Direction = FileSendDirection.Receive
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
            if (!response.isValid())
            {
                Logger.Error("Failed to send the FileSendRequestResponse for file " + ((FileSendRequestResponseRq)request).fileSendId + " : " + response.errorMessage);
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
                var notification = ShowFileProgressNotification(args.FileInfo);
                notification.ProcessCancelFile += delegate { NotificationOnProcessCancelFile(notification, fileReceiver); };
                fileReceiver.ConnectionClosed += delegate { _appContext.NotificationManager.DeleteNotification(notification); };
                fileReceiver.DataRead += delegate
                { notification.Progress = (int)((fileReceiver.DataWriteSize * 100) / args.FileInfo.FileSize); };
                fileReceiver.ListenOnce();
                _appContext.ConnectionManager.Connection.RequestAsync(request, FileSendRequestResponseHandler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + args.FileInfo.Filename + "[" + args.FileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }

        private FileSendProgressNotification ShowFileProgressNotification(FileSendInfo fileInfo)
        {
            var notification = new FileSendProgressNotification()
            {
                From = _engagement.SecondParty.Username,
                ProgressText = fileInfo.Filename,
                FileInfo = fileInfo
            };
            _appContext.NotificationManager.AddNotification(notification);
            return notification;
        }

        private void NotificationOnProcessCancelFile(FileSendProgressNotification notification, FileSendListener fileReceiver)
        {
            // cancel transfer here
            fileReceiver.Close();
        }

        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            if (fileSendId != null && _pendingFileSends.ContainsKey(fileSendId))
            {
                FileSendInfo fileInfo = _pendingFileSends[fileSendId];
                _pendingFileSends.Remove(fileSendId);
                if (accepted)
                {
                    Logger.Info("File send of file " + fileInfo.Filename + " accepted by " + _engagement.SecondParty.Name);
                    FileSendClient client = new FileSendClient(_engagement.TransportManager);
                    // this is hacky, but lets get it to work before we make it pretty
                    var notification = ShowFileProgressNotification(fileInfo);
                    notification.ProcessCancelFile += delegate { NotificationOnProcessCancelFile(notification, client); };

                    client.DataWritten += delegate { notification.Progress = (int)((client.DataWriteSize * 100) / fileInfo.FileSize); };
                    client.SendFileComplete += delegate { _appContext.NotificationManager.DeleteNotification(notification); };
                    Thread fileSendThread = new Thread(() => client.SendFile(fileInfo)) { IsBackground = true };
                    fileSendThread.Start();
                }
                else
                {
                    Logger.Info("File send of file " + fileInfo.Filename + " rejected by " + _engagement.SecondParty.Name);
                }
            }
            else
            {
                throw new Exception("Got a file send request response with an invalid id [" + fileSendId + "]");
            }
        }

        private void NotificationOnProcessCancelFile(FileSendProgressNotification notification, FileSendClient client)
        {
            _appContext.NotificationManager.DeleteNotification(notification);
            client.Close();
        }
    }
}
