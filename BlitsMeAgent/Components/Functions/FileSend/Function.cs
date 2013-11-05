using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.FileSend.Notification;
using BlitsMe.Agent.Components.Functions.RemoteDesktop;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    //internal delegate void FileSendRequestEvent(object sender, FileSendRequest args);

    class Function : FunctionImpl
    {
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;
        private Chat.Function Chat { get { return _engagement.Functions["Chat"] as Chat.Function; } }
        private readonly Dictionary<String, FileSendInfo> _pendingFileSends = new Dictionary<string, FileSendInfo>();
        private readonly Dictionary<String, FileSendInfo> _pendingFileReceives = new Dictionary<string, FileSendInfo>();
        public override String Name { get { return "FileSend"; } }

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
                        shortCode = _engagement.SecondParty.ActiveShortCode,
                        username = _engagement.SecondParty.Person.Username,
                        filename = fileInfo.Filename,
                        fileSize = fileInfo.FileSize,
                        fileSendId = fileInfo.FileSendId,
                        interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id
                    };
                try
                {
                    Chat.LogSystemMessage(string.Format("Sending {0} the file {1}, waiting for acceptance.", _engagement.SecondParty.Person.Firstname, filename));
                    _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestRq, FileSendRequestRs>(request,
                                                                          (req, res, ex) =>
                                                                          ProcessFileSendRequestRs(req, res, ex,
                                                                                                         fileInfo));
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during request for File Send : " + ex.Message, ex);
                    Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Person.Firstname +
                                                      " a request to send them a file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up request for file send : " + ex.Message, ex);
                Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Person.Firstname + " a request to send them a file.");
            }
        }

        // Async callback on result of requesting to send a file
        private void ProcessFileSendRequestRs(FileSendRequestRq request, FileSendRequestRs res, Exception e, FileSendInfo fileInfo)
        {
            if (e != null)
            {
                Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Person.Username + " a request to send them a file.");
            }
            else
            {
                Logger.Info("Requested to send " + fileInfo.Filename + " to " + _engagement.SecondParty.Person.Username);
                fileInfo.State = FileSendState.PendingSend;
                _pendingFileSends.Add(request.fileSendId, fileInfo);
                IsActive = true;
                var notification = new CancellableNotification()
                {
                    AssociatedUsername = _engagement.SecondParty.Person.Username,
                    Message = "Offering " + _engagement.SecondParty.Person.Firstname + " " + fileInfo.Filename,
                    Flag = "",
                    CancelTooltip = "Cancel File Send",
                    Id = fileInfo.FileSendId
                };
                notification.Cancelled += (sender, args) => CancelFileOffer(fileInfo);
                fileInfo.Notification = notification;
                _appContext.NotificationManager.AddNotification(notification);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
            }
        }


        // Called when the offering of the file is cancelled by this user
        private void CancelFileOffer(FileSendInfo fileInfo)
        {
            Chat.LogSystemMessage("You stopped offering " + fileInfo.Filename + " to " + _engagement.SecondParty.Person.Firstname);
            Logger.Debug("User cancelled the file send for " + fileInfo.Filename);
            RemovePendingFileSend(fileInfo.FileSendId);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
        }

        // Called when someone is requesting to send a file to us
        internal void ProcessIncomingFileSendRequest(string filename, string fileSendId, long fileSize)
        {
            Logger.Info(_engagement.SecondParty.Person.Username + " requests to send the file " + filename);

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
                AssociatedUsername = _engagement.SecondParty.Person.Username,
                //Message = _engagement.SecondParty.Person.Firstname + " would like to send you " + filename,
                Message = "Incoming file transfer request \n" + filename,
                Flag = "ReceiveFileRequest",
                FileInfo = fileSendInfo,
            };
            notification.AnsweredTrue += (sender, args) => ProcessAcceptFile(fileSendInfo);
            notification.AnsweredFalse += (sender, args) => ProcessDenyFile(fileSendInfo);
            fileSendInfo.Notification = notification;
            _appContext.NotificationManager.AddNotification(notification);
            //Chat.LogSystemMessage(_engagement.SecondParty.Person.Firstname + " offered you the file " + filename + ".");
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_REQUEST) { To = "_SELF", From = _engagement.SecondParty.Person.Username, FileInfo = fileSendInfo });
        }

        // We call this when we deny them access to receive the file
        private void ProcessDenyFile(FileSendInfo fileInfo)
        {
            fileInfo.State = FileSendState.ReceiveCancelled;
            RemovePendingFileReceive(fileInfo.FileSendId);
            Logger.Info("Denied request from " + _engagement.SecondParty.Person.Name + " to send the file " + fileInfo.Filename);
            Chat.LogSystemMessage("You refused " + fileInfo.Filename + " from " + _engagement.SecondParty.Person.Firstname + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ActiveShortCode,
                    username = _engagement.SecondParty.Person.Username,
                    fileSendId = fileInfo.FileSendId,
                    accepted = false,
                    interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq, FileSendRequestResponseRs>(request, delegate(FileSendRequestResponseRq rq, FileSendRequestResponseRs rs, Exception e)
                    {
                        if (e != null)
                        {
                            Logger.Error(
                                "Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " +
                                e.Message, e);
                        }
                        else
                        {
                            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo, Answer = false });
                        }
                    });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Person.Username);
            }
        }


        // Called when this user accepts a file
        private void ProcessAcceptFile(FileSendInfo fileInfo)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Person.Username + " to send the file " + fileInfo.Filename);
            Chat.LogSystemMessage("Accepted " + fileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
                {
                    shortCode = _engagement.SecondParty.ActiveShortCode,
                    username = _engagement.SecondParty.Person.Username,
                    interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                    fileSendId = fileInfo.FileSendId,
                    accepted = true
                };
            try
            {
                fileInfo.State = FileSendState.Receiving;
                fileInfo.FileReceiver = new FileSendListener(_engagement.TransportManager, fileInfo);
                var notification = ShowFileProgressNotification(fileInfo);
                fileInfo.Notification = notification;
                notification.ProcessCancelFile += delegate { CancelFileReceive(fileInfo); };
                fileInfo.FileReceiver.ConnectionClosed +=
                    (o, eventArgs) => FileReceiverOnConnectionClosed(fileInfo);
                fileInfo.FileReceiver.DataRead += delegate { notification.Progress = (int)((fileInfo.FileReceiver.DataWriteSize * 100) / fileInfo.FileSize); };
                fileInfo.FileReceiver.ListenOnce();
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq, FileSendRequestResponseRs>(request,
                    (rq, rs, e) =>
                    {
                        if (e != null)
                        {
                            RemovePendingFileReceive(request.fileSendId);
                            Logger.Error(
                                "Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " + e.Message, e);
                            fileInfo.FileReceiver.Close();
                            _appContext.NotificationManager.DeleteNotification(notification);
                            Chat.LogSystemMessage("An error occured receiving the file");
                        }
                        else
                        {
                            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo, Answer = true });
                        }
                    });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Person.Username);
            }
        }

        private void FileReceiverOnConnectionClosed(FileSendInfo fileInfo)
        {
            RemovePendingFileReceive(fileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            if (fileInfo.FileReceiver.FileReceiveResult)
            {
                var fileReceivedNotification = new FileReceivedNotification
                    {
                        AssociatedUsername = _engagement.SecondParty.Person.Username,
                        FileInfo = fileInfo
                    };
                _appContext.NotificationManager.AddNotification(fileReceivedNotification);
                Chat.LogSystemMessage("Successfully received the file " +
                                                  fileInfo.Filename);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_COMPLETE) { To = "_SELF", From = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
            }
            else
            {
                if (fileInfo.State != FileSendState.ReceiveCancelled)
                    Chat.LogErrorMessage("There was a problem receiving the file " +
                                                  fileInfo.Filename);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_FAILED) { To = "_SELF", From = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
            }
        }

        private FileSendProgressNotification ShowFileProgressNotification(FileSendInfo fileInfo)
        {
            var notification = new FileSendProgressNotification()
            {
                AssociatedUsername = _engagement.SecondParty.Person.Username,
                FileInfo = fileInfo
            };
            _appContext.NotificationManager.AddNotification(notification);
            return notification;
        }

        // this user cancelled receiving of the file
        private void CancelFileReceive(FileSendInfo fileInfo)
        {
            Chat.LogSystemMessage("You cancelled receiving the file " + fileInfo.Filename);
            fileInfo.State = FileSendState.ReceiveCancelled;
            // cancel transfer here
            RemovePendingFileReceive(fileInfo.FileSendId);
            fileInfo.FileReceiver.Close();
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
        }

        // Called when a response comes back to me from a file request I sent (either I can send or not)
        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            // first remove the cancel file send notification
            /*
            foreach (var notification in _appContext.NotificationManager.Notifications)
            {
                if (notification is CancellableNotification && _engagement.SecondPartyUsername.Equals(notification.AssociatedUsername) && notification.Id.Equals(fileSendId))
                {
                    _appContext.NotificationManager.DeleteNotification(notification);
                    break;
                }
            }*/
            if (fileSendId != null && _pendingFileSends.ContainsKey(fileSendId))
            {
                FileSendInfo fileInfo = _pendingFileSends[fileSendId];
                _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { To = "_SELF", From = _engagement.SecondParty.Person.Username, FileInfo = fileInfo, Answer = accepted });
                if (accepted)
                {
                    fileInfo.State = FileSendState.Sending;
                    Chat.LogSystemMessage(_engagement.SecondParty.Person.Firstname + " accepted " +
                                                      fileInfo.Filename);
                    Logger.Info("File send of file " + fileInfo.Filename + " accepted by " + _engagement.SecondParty.Person.Name);
                    fileInfo.FileSender = new FileSendClient(_engagement.TransportManager);
                    // this is hacky, but lets get it to work before we make it pretty
                    var notification = ShowFileProgressNotification(fileInfo);
                    fileInfo.Notification = notification;
                    notification.ProcessCancelFile += delegate { CancelFileSend(fileInfo); };

                    fileInfo.FileSender.DataWritten += delegate { notification.Progress = (int)((fileInfo.FileSender.DataWriteSize * 100) / fileInfo.FileSize); };
                    fileInfo.FileSender.SendFileComplete += delegate { FileSendComplete(fileInfo); };
                    Thread fileSendThread = new Thread(() => fileInfo.FileSender.SendFile(fileInfo)) { IsBackground = true, Name = "fileSend[" + fileInfo.FileSendId + "]" };
                    fileSendThread.Start();
                    OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_START) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
                }
                else
                {
                    Chat.LogSystemMessage(_engagement.SecondParty.Person.Firstname + " refused " +
                                  fileInfo.Filename);
                    RemovePendingFileSend(fileInfo.FileSendId);
                    Logger.Info("File send of file " + fileInfo.Filename + " rejected by " + _engagement.SecondParty.Person.Name);
                }
            }
            else
            {
                throw new Exception("Got a file send request response with an invalid id [" + fileSendId + "]");
            }
        }

        // called if we are sending the file and it is complete
        private void FileSendComplete(FileSendInfo fileInfo)
        {
            RemovePendingFileSend(fileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_COMPLETE) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
        }

        // called if we cancel the file send
        private void CancelFileSend(FileSendInfo fileInfo)
        {
            RemovePendingFileReceive(fileInfo.FileSendId);
            fileInfo.FileSender.Close();
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL) { From = "_SELF", To = _engagement.SecondParty.Person.Username, FileInfo = fileInfo });
        }

        private void RemovePendingFileReceive(String fileSendId)
        {
            if (_pendingFileReceives.ContainsKey(fileSendId))
                _pendingFileReceives.Remove(fileSendId);
            TrySetInactive();
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

        public override void Close()
        {
            // Close all send/receives
            _pendingFileSends.Values.ToList().ForEach(CloseFileSend);
            _pendingFileReceives.Values.ToList().ForEach(CloseFileSend);
        }

        private void CloseFileSend(FileSendInfo fileSend)
        {
            switch (fileSend.State)
            {
                case (FileSendState.PendingSend):
                    CancelFileOffer(fileSend);
                    break;
                case (FileSendState.Receiving):
                    CancelFileReceive(fileSend);
                    break;
                case (FileSendState.Sending):
                    CancelFileSend(fileSend);
                    break;
                default:
                    if (fileSend.Notification != null)
                    {
                        _appContext.NotificationManager.DeleteNotification(fileSend.Notification);
                    }
                    break;
            }
        }
    }

    internal class FileSendActivity : EngagementActivity
    {
        internal const String FILE_SEND_REQUEST = "FILE_SEND_REQUEST";
        internal const String FILE_SEND_RESPONSE = "FILE_SEND_RESPONSE";
        internal const String FILE_SEND_START = "FILE_SEND_START";
        internal const String FILE_SEND_COMPLETE = "FILE_SEND_COMPLETE";
        internal const String FILE_SEND_FAILED = "FILE_SEND_FAILED";
        internal const String FILE_SEND_CANCEL_REQUEST = "FILE_SEND_CANCEL_REQUEST";
        internal const String FILE_SEND_CANCEL = "FILE_SEND_CANCEL";
        internal FileSendInfo FileInfo;

        internal FileSendActivity(Engagement engagement, String activity)
            : base(engagement, "FILE_SEND", activity)
        {
        }

        public bool Answer;
    }
}
