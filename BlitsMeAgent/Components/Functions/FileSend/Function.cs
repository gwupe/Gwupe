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
        private readonly Dictionary<String, FileSendInfo> _pendingFileReceives = new Dictionary<string, FileSendInfo>();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Function));

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        // Request to send a file to user
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
                    _engagement.Chat.LogSystemMessage(string.Format("Sending {0} the file {1}, waiting for acceptance.", _engagement.SecondParty.Firstname, filename));
                    _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestRq, FileSendRequestRs>(request,
                                                                          (req, res, ex) =>
                                                                          ProcessFileSendRequestRs(req, res, ex,
                                                                                                         fileInfo));
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during request for File Send : " + ex.Message, ex);
                    _engagement.Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Firstname +
                                                      " a request to send them a file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up request for file send : " + ex.Message, ex);
                _engagement.Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Firstname + " a request to send them a file.");
            }
        }

        // Called when the offering of the file is cancelled by this user
        private void FileOfferCancelled(FileSendInfo fileInfo)
        {
            _engagement.Chat.LogSystemMessage("You stopped offering " + fileInfo.Filename + " to " + _engagement.SecondParty.Firstname);
            Logger.Debug("User cancelled the file send for " + fileInfo.Filename);
            RemovePendingFileSend(fileInfo.FileSendId);
        }

        private void RemovePendingFileSend(String fileSendId)
        {
            if (_pendingFileSends.ContainsKey(fileSendId))
                _pendingFileSends.Remove(fileSendId);
            TrySetInactive();
        }

        private void TrySetInactive()
        {
            if (_pendingFileSends.Count == 0 && _pendingFileReceives.Count == 0)
                IsActive = false;
        }

        // Async callback on result of requesting to send a file
        private void ProcessFileSendRequestRs(FileSendRequestRq request, FileSendRequestRs res, Exception e, FileSendInfo fileInfo)
        {
            if (e != null)
            {
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Username + " a request to send them a file.");
            }
            else
            {
                Logger.Info("Requested to send " + fileInfo.Filename + " to " + _engagement.SecondParty.Username);
                fileInfo.State = FileSendState.PendingSend;
                _pendingFileSends.Add(request.fileSendId, fileInfo);
                IsActive = true;
                var notification = new CancellableNotification()
                {
                    AssociatedUsername = _engagement.SecondParty.Username,
                    Message = "Offering " + _engagement.SecondParty.Firstname + " " + fileInfo.Filename,
                    CancelTooltip = "Cancel File Send",
                    Id = fileInfo.FileSendId
                };
                notification.Cancelled += (sender, args) => FileOfferCancelled(fileInfo);
                _appContext.NotificationManager.AddNotification(notification);
            }
        }

        // Called when someone is requesting to send a file to us
        internal void ProcessIncomingFileSendRequest(string filename, string fileSendId, long fileSize)
        {
            Logger.Info(_engagement.SecondParty.Username + " requests to send the file " + filename);

            var fileSendInfo = new FileSendInfo()
                {
                    Filename = filename,
                    FileSendId = fileSendId,
                    FileSize = fileSize,
                    Direction = FileSendDirection.Receive,
                    State = FileSendState.PendingReceive
                };
            IsActive = true;
            _pendingFileReceives.Add(fileSendId, fileSendInfo);
            var notification = new FileSendRequestNotification()
            {
                AssociatedUsername = _engagement.SecondParty.Username,
                Message = _engagement.SecondParty.Firstname + " would like to send you " + filename,
                FileInfo = fileSendInfo,
            };
            notification.AnsweredTrue += (sender, args) => ProcessAcceptFile(notification.FileInfo);
            notification.AnsweredFalse += (sender, args) => ProcessDenyFile(notification.FileInfo);
            _appContext.NotificationManager.AddNotification(notification);
            _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Firstname + " offered you the file " + filename + ".");
        }

        // We call this when we deny them access to receive the file
        private void ProcessDenyFile(FileSendInfo fileInfo)
        {
            fileInfo.State = FileSendState.ReceiveCancelled;
            RemovePendingFileReceive(fileInfo.FileSendId);
            Logger.Info("Denied request from " + _engagement.SecondParty.Name + " to send the file " + fileInfo.Filename);
            _engagement.Chat.LogSystemMessage("You refused " + fileInfo.Filename + " from " + _engagement.SecondParty.Firstname + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ShortCode,
                    username = _engagement.SecondParty.Username,
                    fileSendId = fileInfo.FileSendId,
                    accepted = false
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq, FileSendRequestResponseRs>(request, delegate(FileSendRequestResponseRq rq, FileSendRequestResponseRs rs, Exception e)
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

        private void RemovePendingFileReceive(String fileSendId)
        {
            if (_pendingFileReceives.ContainsKey(fileSendId))
                _pendingFileReceives.Remove(fileSendId);
            TrySetInactive();
        }

        // Async callback from a response message to a file send request (either we will allow it or not)
        private void FileSendRequestResponseHandler(FileSendRequestResponseRq request, FileSendRequestResponseRs response, Exception e, FileSendListener fileReceiver, FileSendProgressNotification notification)
        {
            if (e != null)
            {
                RemovePendingFileReceive(request.fileSendId);
                Logger.Error("Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " + e.Message, e);
                fileReceiver.Close();
                _appContext.NotificationManager.DeleteNotification(notification);
                _engagement.Chat.LogSystemMessage("An error occured receiving the file");
            }
        }

        // Called when a user accepts a file
        private void ProcessAcceptFile(FileSendInfo fileInfo)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Username + " to send the file " + fileInfo.Filename);
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
                fileInfo.State = FileSendState.Receiving;
                FileSendListener fileReceiver = new FileSendListener(_engagement.TransportManager, fileInfo);
                var notification = ShowFileProgressNotification(fileInfo);
                notification.ProcessCancelFile += delegate { FileReceiveCancelled(notification, fileReceiver); };
                fileReceiver.ConnectionClosed +=
                    (o, eventArgs) => FileReceiverOnConnectionClosed(notification, fileReceiver);
                fileReceiver.DataRead += delegate { notification.Progress = (int)((fileReceiver.DataWriteSize * 100) / fileInfo.FileSize); };
                fileReceiver.ListenOnce();
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq, FileSendRequestResponseRs>(request,
                    (rq, rs, e) => FileSendRequestResponseHandler(rq, rs, e, fileReceiver, notification));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Username);
            }
        }


        private void FileReceiverOnConnectionClosed(FileSendProgressNotification notification, FileSendListener fileReceiver)
        {
            RemovePendingFileReceive(notification.FileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(notification);
            if (fileReceiver.FileReceiveResult)
            {
                var fileReceivedNotification = new FileReceivedNotification
                    {
                        AssociatedUsername = _engagement.SecondParty.Username,
                        FileInfo = notification.FileInfo
                    };
                _appContext.NotificationManager.AddNotification(fileReceivedNotification);
                _engagement.Chat.LogSystemMessage("Successfully received the file " +
                                                  notification.FileInfo.Filename);
            }
            else
            {
                if (notification.FileInfo.State != FileSendState.ReceiveCancelled)
                    _engagement.Chat.LogErrorMessage("There was a problem receiving the file " +
                                                  notification.FileInfo.Filename);
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

        // this user cancelled receiving of the file
        private void FileReceiveCancelled(FileSendProgressNotification notification, FileSendListener fileReceiver)
        {
            _engagement.Chat.LogSystemMessage("You cancelled receiving the file " + notification.FileInfo.Filename);
            notification.FileInfo.State = FileSendState.ReceiveCancelled;
            // cancel transfer here
            RemovePendingFileReceive(notification.FileInfo.FileSendId);
            fileReceiver.Close();
            _appContext.NotificationManager.DeleteNotification(notification);
        }

        // Called when a response comes back to me from a file request I sent (either I can send or not)
        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            // first remove the cancel file send notification
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
                if (accepted)
                {
                    fileInfo.State = FileSendState.Sending;
                    _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Firstname + " accepted " +
                                                      fileInfo.Filename);
                    Logger.Info("File send of file " + fileInfo.Filename + " accepted by " + _engagement.SecondParty.Name);
                    FileSendClient client = new FileSendClient(_engagement.TransportManager);
                    // this is hacky, but lets get it to work before we make it pretty
                    var notification = ShowFileProgressNotification(fileInfo);
                    notification.ProcessCancelFile += delegate { FileSendCancelled(notification, client); };

                    client.DataWritten += delegate { notification.Progress = (int)((client.DataWriteSize * 100) / fileInfo.FileSize); };
                    client.SendFileComplete += delegate { FileSendComplete(notification); };
                    Thread fileSendThread = new Thread(() => client.SendFile(fileInfo)) { IsBackground = true, Name = "fileSend[" + fileInfo.FileSendId + "]" };
                    fileSendThread.Start();
                }
                else
                {
                    _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Firstname + " refused " +
                                  fileInfo.Filename);
                    RemovePendingFileSend(fileInfo.FileSendId);
                    Logger.Info("File send of file " + fileInfo.Filename + " rejected by " + _engagement.SecondParty.Name);
                }
            }
            else
            {
                throw new Exception("Got a file send request response with an invalid id [" + fileSendId + "]");
            }
        }

        private void FileSendComplete(FileSendProgressNotification notification)
        {
            RemovePendingFileSend(notification.FileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(notification);
        }

        private void FileSendCancelled(FileSendProgressNotification notification, FileSendClient client)
        {
            RemovePendingFileReceive(notification.FileInfo.FileSendId);
            client.Close();
            _appContext.NotificationManager.DeleteNotification(notification);
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                if (_isActive != value)
                {
                    Logger.Debug("FileSender is now " + (value ? "Active" : "Inactive"));
                    _isActive = value;
                    if (value)
                        OnActivate(EventArgs.Empty);
                    else
                        OnDeactivate(EventArgs.Empty);
                }
            }
        }

        public event EventHandler Activate;

        public void OnActivate(EventArgs e)
        {
            EventHandler handler = Activate;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Deactivate;

        public void OnDeactivate(EventArgs e)
        {
            EventHandler handler = Deactivate;
            if (handler != null) handler(this, e);
        }
    }
}
