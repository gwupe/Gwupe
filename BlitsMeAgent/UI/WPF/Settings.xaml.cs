using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Agent.UI.WPF.Utils;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : IDashboardContentControl, IGwupeUserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Settings));
        private InputValidator _inputValidator;

        public Settings()
        {
            this.InitializeComponent();
            _inputValidator = new InputValidator(StatusText, ErrorText, Dispatcher, true);
            DataContext = new SettingsData(_inputValidator);
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }

    }

    public class SettingsData : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingsData));
        private readonly InputValidator _validator;

        public SettingsData(InputValidator validator)
        {
            _validator = validator;
        }

        public bool? PreRelease
        {
            get
            {
                try
                {
                    return GwupeClientAppContext.CurrentAppContext.Reg.PreRelease;
                }
                catch (Exception e)
                {
                    _validator.SetError("Failed to get experimental setting");
                }
                return null;
            }
            set
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.GwupeService.SetPreRelease(value == true);
                        OnPropertyChanged("PreRelease");

                        _validator.SetStatus(value == true ? "You will now be upgraded to beta releases when available."
                                : "You will no longer be upgraded to beta releases.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to set registry variable for pre release", ex);
                    }
                    _validator.SetError("There was an error saving your setting.");
                });
            }
        }

        public bool? NoUpdateNotifications
        {
            get
            {
                try
                {
                    return !GwupeClientAppContext.CurrentAppContext.Reg.NotifyUpdate;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get NoUpdateNotifications Setting : ", e);
                }
                return null;
            }
            set
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.Reg.NotifyUpdate = value == false;
                        OnPropertyChanged("NoUpdateNotifications");
                        _validator.SetStatus(value == true ? "You will no longer be notified when an update occurs."
        : "You will be notified when and update occurs.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to set registry variable for NoUpdateNotifications", ex);
                    }
                    _validator.SetError("There was an error saving your setting.");

                });

            }
        }
        public bool? NoAutoUpgrade
        {
            get
            {
                try
                {
                    return !GwupeClientAppContext.CurrentAppContext.Reg.AutoUpgrade;
                }
                catch (Exception e)
                {
                    _validator.SetError("Failed to get experimental setting");
                }
                return null;
            }
            set
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.GwupeService.DisableAutoUpgrade(value == true);
                        OnPropertyChanged("NoAutoUpgrade");

                        _validator.SetStatus(value == true ? "You will not be upgraded automatically, this is not recommended and may lead to instability."
                                : "You will now be automatically upgraded as new versions are released.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to set registry variable for auto upgrade", ex);
                    }
                    _validator.SetError("There was an error saving your setting.");
                });
            }
        }

        public bool? Experimental
        {
            get
            {
                try
                {
                    return GwupeClientAppContext.CurrentAppContext.Reg.Experimental;
                }
                catch (Exception e)
                {
                    _validator.SetError("Failed to get experimental setting");
                }
                return null;
            }
            set
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.Reg.Experimental = value == true;
                        OnPropertyChanged("Experimental");

                        _validator.SetStatus(value == true ? "You now have access to experimental features, restart Gwupe to activate."
                                : "You no longer have access to experimental features, restart Gwupe to activate.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to set registry variable for experimental", ex);
                    }
                    _validator.SetError("There was an error saving your setting.");
                });

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}