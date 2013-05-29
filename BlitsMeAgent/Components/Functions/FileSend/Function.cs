using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.FileSend.Notification;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
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
                    _engagement.Chat.LogSystemMessage(string.Format("You sent {0} a request to send the file {1}", _engagement.SecondParty.Name, filename));
                    _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestRq,FileSendRequestRs>(request,
                                                                          (req, res, ex) =>
                                                                          ProcessRequestFileSendResponse(req, res, ex,
                                                                                                         fileInfo));
                    var notification = new CancellableNotification()
                    {
                        AssociatedUsername = _engagement.SecondParty.Username,
                        Message = "You offered " + _engagement.SecondParty.Name + " " + filename,
                        CancelTooltip = "Cancel File Send",
                        Id = fileInfo.FileSendId
                    };
                    notification.Cancelled += (sender, args) => FileOfferCancelled(fileInfo);
                    _appContext.NotificationManager.AddNotification(notification);
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

        private void FileOfferCancelled(FileSendInfo fileInfo)
        {
            Logger.Debug("User cancelled the file send for " + fileInfo.Filename);
            if(_pendingFileSends.ContainsKey(fileInfo.FileSendId))
                _pendingFileSends.Remove(fileInfo.FileSendId);
        }

        private void ProcessRequestFileSendResponse(FileSendRequestRq request, FileSendRequestRs res, Exception e, FileSendInfo fileInfo)
        {
            if (e != null)
            {
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to send them a file.");
            }
            else
            {
                Logger.Info("Requested to send " + fileInfo.Filename + " to " + _engagement.SecondParty.Name);
                _pendingFileSends.Add(request.fileSendId, fileInfo);
            }
        }

        public void ProcessIncomingFileSendRequest(string filename, string fileSendId, long fileSize)
        {
            Logger.Info(_engagement.SecondParty.Name + " requests to send the file " + filename);
            var notification = new FileSendRequestNotification()
            {
                AssociatedUsername = _engagement.SecondParty.Username,
                Message = _engagement.SecondParty.Name + " would like to send you the file " + filename,
                FileInfo = new FileSendInfo()
                {
                    Filename = filename,
                    FileSendId = fileSendId,
                    FileSize = fileSize,
                    Direction = FileSendDirection.Receive
                },
            };
            notification.AnsweredTrue += (sender, args) => ProcessAcceptFile(notification.FileInfo);
            notification.AnsweredFalse += (sender, args) => ProcessDenyFile(notification.FileInfo);
            _appContext.NotificationManager.AddNotification(notification);
            _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " offered to send you the file " + filename + ".");
        }

        private void ProcessDenyFile(FileSendInfo fileInfo)
        {
            Logger.Info("Denied request from " + _engagement.SecondParty.Name + " to send the file " + fileInfo.Filename);
            _engagement.Chat.LogSystemMessage("You refused to let " + _engagement.SecondParty.Name + " send you the file " + fileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ShortCode,
                    username = _engagement.SecondParty.Username,
                    fileSendId = fileInfo.FileSendId,
                    accepted = false
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq,FileSendRequestResponseRs>(request, delegate(FileSendRequestResponseRq rq, FileSendRequestResponseRs rs, Exception e)
                    {
                        if (e != null)
                        {
                            Logger.Error("Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " + e.Message, e);
                        }
                    });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }

        private void FileSendRequestResponseHandler(FileSendRequestResponseRq request, FileSendRequestResponseRs response, Exception e, FileSendListener fileReceiver, FileSendProgressNotification notification)
        {
            if (e != null)
            {
                Logger.Error("Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " + e.Message,e);
                fileReceiver.Close();
                _appContext.NotificationManager.DeleteNotification(notification);
                _engagement.Chat.LogSystemMessage("An error occured receiving the file");
            }
        }

        private void ProcessAcceptFile(FileSendInfo fileInfo)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Name + " to send the file " + fileInfo.Filename);
            _engagement.Chat.LogSystemMessage("Accepted " + fileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ShortCode,
                    username = _engagement.SecondParty.Username,
                    fileSendId = fileInfo.FileSendId,
                    accepted = true
                };
            try
            {
                FileSendListener fileReceiver = new FileSendListener(_engagement.TransportManager, fileInfo);
                var notification = ShowFileProgressNotification(fileInfo);
                notification.ProcessCancelFile += delegate { FileReceiveCancelled(notification, fileReceiver); };
                fileReceiver.ConnectionClosed +=
                    (o, eventArgs) => FileReceiverOnConnectionClosed(notification, fileReceiver);
                fileReceiver.DataRead += delegate { notification.Progress = (int)((fileReceiver.DataWriteSize * 100) / fileInfo.FileSize); };
                fileReceiver.ListenOnce();
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq,FileSendRequestResponseRs>(request, 
                    ( rq,  rs,  e) => FileSendRequestResponseHandler(rq, rs, e, fileReceiver, notification));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }


        private void FileReceiverOnConnectionClosed(FileSendProgressNotification notification, FileSendListener fileReceiver)
        {
            _appContext.NotificationManager.DeleteNotification(notification);
            if (fileReceiver.FileReceiveResult)
            {
                var fileReceivedNotification = new FileReceivedNotification
                    {
                        AssociatedUsername = _engagement.SecondParty.Username,
                        FileInfo = notification.FileInfo
                    };
                _appContext.NotificationManager.AddNotification(fileReceivedNotification);
            }
        }

        private FileSendProgressNotification ShowFileProgressNotification(FileSendInfo fileInfo)
        {
            var notification = new FileSendProgressNotification()
            {
                AssociatedUsername = _engagement.SecondParty.Username,
                FileInfo = fileInfo
            };
            _appContext.NotificationManager.AddNotification(notification);
            return notification;
        }

        private void FileReceiveCancelled(FileSendProgressNotification notification, FileSendListener fileReceiver)
        {
            // cancel transfer here
            fileReceiver.Close();
            _appContext.NotificationManager.DeleteNotification(notification);
        }

        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            foreach (var notification in _appContext.NotificationManager.Notifications)
            {
                if (notification is CancellableNotification && _engagement.SecondPartyUsername.Equals(notification.AssociatedUsername) && notification.Id.Equals(fileSendId))
                {
                    _appContext.NotificationManager.DeleteNotification(notification);
                    break;
                }
            }
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
                    notification.ProcessCancelFile += delegate { FileSendCancelled(notification, client); };

                    client.DataWritten += delegate { notification.Progress = (int)((client.DataWriteSize * 100) / fileInfo.FileSize); };
                    client.SendFileComplete +=
                        (sender, args) => _appContext.NotificationManager.DeleteNotification(notification);
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

        private void FileSendCancelled(FileSendProgressNotification notification, FileSendClient client)
        {
            client.Close();
            _appContext.NotificationManager.DeleteNotification(notification);
        }
    }
}
