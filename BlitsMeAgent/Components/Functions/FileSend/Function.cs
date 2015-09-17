using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Gwupe.Agent.Components.Functions.API;
using Gwupe.Agent.Components.Functions.Chat;
using Gwupe.Agent.Components.Functions.FileSend.ChatElement;
using Gwupe.Agent.Components.Functions.FileSend.Notification;
using Gwupe.Agent.Components.Notification;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using Gwupe.Common;
using Gwupe.Common.Security;
using log4net;

namespace Gwupe.Agent.Components.Functions.FileSend
{
    //internal delegate void FileSendRequestEvent(object sender, FileSendRequest args);

    class Function : FunctionImpl
    {
        private readonly GwupeClientAppContext _appContext;
        private readonly Engagement _engagement;
        private Chat.Function Chat { get { return _engagement.Functions["Chat"] as Chat.Function; } }
        private readonly Dictionary<String, FileSendInfo> _pendingFileSends = new Dictionary<string, FileSendInfo>();
        private readonly Dictionary<String, FileSendInfo> _pendingFileReceives = new Dictionary<string, FileSendInfo>();
        public override String Name { get { return "FileSend"; } }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Function));

        internal Function(GwupeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        #region FileSendMessaging

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
                        username = _engagement.SecondParty.Party.Username,
                        filename = fileInfo.Filename,
                        fileSize = fileInfo.FileSize,
                        fileSendId = fileInfo.FileSendId,
                        interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id
                    };
                try
                {
                    Chat.LogSystemMessage(string.Format("Sending {0} the file {1}, waiting for acceptance.", _engagement.SecondParty.Party.Firstname, filename));
                    _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestRq, FileSendRequestRs>(request,
                                                                          (req, res, ex) =>
                                                                          ProcessFileSendRequestRs(req, res, ex,
                                                                                                         fileInfo));
                    Logger.Info("Requested to send " + fileInfo.Filename + " to " + _engagement.SecondParty.Party.Username);
                    fileInfo.State = FileSendState.PendingSend;
                    _pendingFileSends.Add(request.fileSendId, fileInfo);
                    IsActive = true;
                    var notification = new CancellableNotification()
                    {
                        AssociatedUsername = _engagement.SecondParty.Party.Username,
                        Message = "Offering " + _engagement.SecondParty.Party.Firstname + " " + fileInfo.Filename,
                        CancelTooltip = "Cancel File Send",
                        Id = fileInfo.FileSendId
                    };
                    notification.Cancelled += (sender, args) => CancelFileOffer(fileInfo);
                    fileInfo.Notification = notification;
                    _appContext.NotificationManager.AddNotification(notification);
                    OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during request for File Send : " + ex.Message, ex);
                    Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Party.Firstname +
                                                      " a request to send them a file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up request for file send : " + ex.Message, ex);
                Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Party.Firstname + " a request to send them a file.");
            }
        }

        // Called when a response comes back to me from a file request I sent (either I am allowed to send or not)
        public void ProcessFileSendRequestResponse(bool accepted, string fileSendId)
        {
            if (fileSendId != null && _pendingFileSends.ContainsKey(fileSendId))
            {
                FileSendInfo fileInfo = _pendingFileSends[fileSendId];
                _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { To = "_SELF", From = _engagement.SecondParty.Party.Username, FileInfo = fileInfo, Answer = accepted });
                if (accepted)
                {

                    fileInfo.State = FileSendState.Sending;
                    Logger.Info("File send of file " + fileInfo.Filename + " accepted by " + _engagement.SecondParty.Party.Name);
                    Chat.LogSystemMessage(_engagement.SecondParty.Party.Firstname + " accepted your request to send " +
                                          fileInfo.Filename);
                    fileInfo.FileSender = new FileSendClient(_engagement.SecondParty, fileInfo);
                    var notification = ShowFileProgressNotification(fileInfo);
                    fileInfo.Notification = notification;
                    notification.Cancelled += delegate { CancelFileSend(fileInfo); };
                    fileInfo.FileSender.DataWritten += delegate { notification.Progress = (int)((fileInfo.FileSender.DataWriteSize * 100) / fileInfo.FileSize); };
                    fileInfo.FileSender.SendFileComplete += (sender, args) => FileSendComplete(fileInfo, args);
                    Thread fileSendThread = new Thread(() => fileInfo.FileSender.SendFile())
                    {
                        IsBackground = true,
                        Name = "fileSend[" + fileInfo.FileSendId + "]"
                    };
                    fileSendThread.Start();
                    OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_START)
                    {
                        From = "_SELF",
                        To = _engagement.SecondParty.Party.Username,
                        FileInfo = fileInfo
                    });
                    /*}
                    else
                    {
                        try
                        {
                            BlitsMeClientAppContext.CurrentAppContext.RepeaterManager.InitRepeatedConnection(
                                _engagement.SecondParty.Person.Username, _engagement.SecondParty.ActiveShortCode,
                                _engagement.Interactions.CurrentOrNewInteraction.Id, fileSendId);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to get a repeated connection to " + _engagement.SecondParty.Person.Username);
                        }
                    }*/
                }
                else
                {
                    Chat.LogSystemMessage(_engagement.SecondParty.Party.Firstname + " refused " + fileInfo.Filename);
                    RemovePendingFileSend(fileInfo.FileSendId);
                    Logger.Info("File send of file " + fileInfo.Filename + " rejected by " + _engagement.SecondParty.Party.Name);
                }
            }
            else
            {
                throw new Exception("Got a file send request response with an invalid id [" + fileSendId + "]");
            }
        }

        // Async callback on result of requesting to send a file
        private void ProcessFileSendRequestRs(FileSendRequestRq request, FileSendRequestRs res, Exception e, FileSendInfo fileInfo)
        {
            if (e != null)
            {
                Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Party.Username + " a request to send them " + fileInfo.Filename);
                Logger.Error("Failed to send file send request for " + fileInfo.Filename);
                RemovePendingFileSend(fileInfo.FileSendId);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_REQUEST_FAILED) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
            }
            else
            {
            }
        }

        // Called when the offering of the file is cancelled by this user
        private void CancelFileOffer(FileSendInfo fileInfo)
        {
            Chat.LogSystemMessage("You stopped offering " + fileInfo.Filename + " to " + _engagement.SecondParty.Party.Firstname);
            Logger.Debug("User cancelled the file send for " + fileInfo.Filename);
            RemovePendingFileSend(fileInfo.FileSendId);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
        }

        #endregion

        #region FileReceiveMessaging

        // Called when someone is requesting to send a file to us
        internal void ProcessIncomingFileSendRequest(string filename, string fileSendId, long fileSize)
        {
            Logger.Info(_engagement.SecondParty.Party.Username + " requests to send the file " + filename);

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
            FileSendRequestChatElement chatElement = LogFileSendRequest(_engagement.SecondParty.Party.Firstname + " offered you the file " + filename + ".", _engagement.SecondParty.Party.Username);
            chatElement.AnsweredTrue += (sender, args) => ProcessAcceptFile(fileSendInfo);
            chatElement.AnsweredFalse += (sender, args) => ProcessDenyFile(fileSendInfo);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_REQUEST) { To = "_SELF", From = _engagement.SecondParty.Party.Username, FileInfo = fileSendInfo });
        }

        // We call this when we denies secondparty the request to send the file to us
        private void ProcessDenyFile(FileSendInfo fileInfo)
        {
            fileInfo.State = FileSendState.ReceiveCancelled;
            RemovePendingFileReceive(fileInfo.FileSendId);
            Logger.Info("Denied request from " + _engagement.SecondParty.Party.Name + " to send the file " + fileInfo.Filename);
            Chat.LogSystemMessage("You refused " + fileInfo.Filename + " from " + _engagement.SecondParty.Party.Firstname + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
            {
                shortCode = _engagement.SecondParty.ActiveShortCode,
                username = _engagement.SecondParty.Party.Username,
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
                        OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo, Answer = false });
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Party.Username);
            }
        }

        // Called when this user accepts a file
        private void ProcessAcceptFile(FileSendInfo fileInfo)
        {
            Logger.Info("Accepted request from " + _engagement.SecondParty.Party.Username + " to send the file " + fileInfo.Filename);
            //Chat.LogSystemMessage("Accepted " + fileInfo.Filename + ".");
            FileSendRequestResponseRq request = new FileSendRequestResponseRq()
            {
                shortCode = _engagement.SecondParty.ActiveShortCode,
                username = _engagement.SecondParty.Party.Username,
                interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                fileSendId = fileInfo.FileSendId,
                accepted = true
            };
            try
            {
                fileInfo.State = FileSendState.Receiving;
                fileInfo.FilePath = OsUtils.IsWinVistaOrHigher ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileInfo.Filename)
                                      : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileInfo.Filename);
                fileInfo.FileReceiver = new FileSendListener(fileInfo);
                var notification = ShowFileProgressNotification(fileInfo);
                fileInfo.Notification = notification;
                notification.Cancelled += delegate { CancelFileReceive(fileInfo); };
                fileInfo.FileReceiver.ServerConnectionClosed += (o, eventArgs) => FileReceiverOnConnectionClosed(fileInfo);
                fileInfo.FileReceiver.DataRead += delegate { notification.Progress = (int)((fileInfo.FileReceiver.DataReadSize * 100) / fileInfo.FileSize); };
                fileInfo.FileReceiver.Listen();
                // Now we have started our listener, we can send our filesendrequest response to the requester, to tell him to send.
                _appContext.ConnectionManager.Connection.RequestAsync<FileSendRequestResponseRq, FileSendRequestResponseRs>(request, (rq, rs, ex) => FileSendRequestResponseResponseHandler(fileInfo, rq, rs, ex));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a file acceptance request for file " + fileInfo.Filename + "[" + fileInfo.FileSendId + "] to " + _engagement.SecondParty.Party.Username);
            }
        }

        // Async callback from our file send request response
        private void FileSendRequestResponseResponseHandler(FileSendInfo fileInfo, FileSendRequestResponseRq request, FileSendRequestResponseRs response, Exception e)
        {
            {
                if (e != null)
                {
                    RemovePendingFileReceive(request.fileSendId);
                    Logger.Error("Failed to send the FileSendRequestResponse for file " + request.fileSendId + " : " + e.Message, e);
                    fileInfo.FileReceiver.Close();
                    _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
                    Chat.LogSystemMessage("An error occured receiving the file");
                }
                else
                {
                    OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_RESPONSE) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo, Answer = true });
                }
            }
        }

        #endregion

        #region FileReceiveEventHandler

        private void FileReceiverOnConnectionClosed(FileSendInfo fileInfo)
        {
            RemovePendingFileReceive(fileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            if (fileInfo.FileReceiver.FileReceiveResult)
            {
                var fileReceivedNotification = new FileReceivedNotification
                    {
                        AssociatedUsername = _engagement.SecondParty.Party.Username,
                        FileInfo = fileInfo
                    };
                _appContext.NotificationManager.AddNotification(fileReceivedNotification);
                Chat.LogSystemMessage("Successfully received the file " + fileInfo.Filename);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_COMPLETE) { To = "_SELF", From = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
            }
            else
            {
                if (fileInfo.State != FileSendState.ReceiveCancelled)
                    Chat.LogErrorMessage("There was a problem receiving the file " +
                                                  fileInfo.Filename);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_FAILED) { To = "_SELF", From = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
            }
        }

        private FileSendProgressNotification ShowFileProgressNotification(FileSendInfo fileInfo)
        {
            var notification = new FileSendProgressNotification()
            {
                AssociatedUsername = _engagement.SecondParty.Party.Username,
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
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
        }

#endregion

        #region FileSendEventHandlers

        // called if we are sending the file and it is complete
        private void FileSendComplete(FileSendInfo fileInfo, FileSendCompleteEventArgs args)
        {
            RemovePendingFileSend(fileInfo.FileSendId);
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            if (args.Success)
            {
                fileInfo.State = FileSendState.SendComplete;
                Chat.LogSystemMessage("You successfully sent the file " + fileInfo.Filename);
                OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_COMPLETE)
                {
                    From = "_SELF",
                    To = _engagement.SecondParty.Party.Username,
                    FileInfo = fileInfo
                });
            }
            else
            {
                if (fileInfo.State != FileSendState.SendCancelled)
                {
                    Chat.LogSystemMessage("Sending of " + fileInfo.Filename + "  failed.");
                    OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_FAILED)
                    {
                        From = "_SELF",
                        To = _engagement.SecondParty.Party.Username,
                        FileInfo = fileInfo
                    });
                }
            }
        }

        // called if we cancel the file send
        private void CancelFileSend(FileSendInfo fileInfo)
        {
            Chat.LogSystemMessage("You cancelled sending the file " + fileInfo.Filename);
            fileInfo.State = FileSendState.SendCancelled;
            Logger.Warn("Cancelling file send of " + fileInfo.Filename);
            RemovePendingFileReceive(fileInfo.FileSendId);
            fileInfo.FileSender.Close();
            _appContext.NotificationManager.DeleteNotification(fileInfo.Notification);
            OnNewActivity(new FileSendActivity(_engagement, FileSendActivity.FILE_SEND_CANCEL) { From = "_SELF", To = _engagement.SecondParty.Party.Username, FileInfo = fileInfo });
        }

        #endregion

        internal FileSendRequestChatElement LogFileSendRequest(String message, string userName)
        {
            var chatElement = new FileSendRequestChatElement()
            {
                Message = message,
                SpeakTime = DateTime.Now,
                UserName = userName
            };
            Chat.Conversation.AddMessage(chatElement);
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_FILE_SEND_REQUEST)
            {
                From = _engagement.SecondParty.Party.Username,
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
            return chatElement;
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
        internal const String FILE_SEND_REQUEST_FAILED = "FILE_SEND_REQUEST_FAILED";
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
